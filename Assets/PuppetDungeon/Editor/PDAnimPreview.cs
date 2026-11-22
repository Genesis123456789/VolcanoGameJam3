using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PuppetDungeon.EditorTools
{
    /// <summary>
    /// Aperçus des animations cuites sur le vrai pantin du jeu (PD_Player), rendus hors écran en PNG
    /// dans Logs/PD_Preview : 6 images par clip, vue de face (haut) et de profil (bas).
    /// Sert à vérifier le résultat sans lancer le jeu.
    /// </summary>
    [InitializeOnLoad]
    public static class PDAnimPreview
    {
        private const string Request = "Logs/pd_preview_request";
        private const string OutDir = "Logs/PD_Preview";
        private const int Cell = 300, Frames = 6;

        static PDAnimPreview()
        {
            // Demande déposée dans Logs/pd_preview_request : traitée dès que l'éditeur est libre (hors Play).
            EditorApplication.update += Poll;
        }

        private static double nextPoll;

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 2.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!File.Exists(Request)) return;
            try { File.Delete(Request); } catch { return; }
            LogModelPlacement();
            PDAnimBaker.DebugLegs = true;
            try
            {
                var lib = PDAnimBaker.EnsureLibrary();
                PDAnimBaker.BakeAll(lib);
                var baseCtrl = PDAnimSets.CreateBaseController();
                PDAnimSets.BuildSets(lib, baseCtrl, out _);
                AssetDatabase.SaveAssets();
            }
            finally { PDAnimBaker.DebugLegs = false; }
            RenderAll();
        }

        /// <summary>Diagnostic : position des maillages des modèles de pièces (espace d'import).</summary>
        private static void LogModelPlacement()
        {
            var sb = new System.Text.StringBuilder("[PD-MODELS]");
            foreach (var path in new[] { PDPaths.PartBody, PDPaths.PartLeg, PDPaths.PartArm, PDPaths.PartShield, PDPaths.PartSniper, PDPaths.PartSword, "Assets/Projet/Graphismes/rig_ff002.fbx" })
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) { sb.Append("\n").Append(path).Append(" : introuvable"); continue; }
                var inst = Object.Instantiate(go);
                inst.hideFlags = HideFlags.HideAndDontSave;
                inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                sb.Append("\n== ").Append(path).Append(" racine ").Append(go.transform.localPosition.ToString("F3")).Append(" ").Append(go.transform.localScale.ToString("F3"));
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    var b = r.bounds;
                    sb.Append($"\n  {r.name} : y {b.min.y:F3}..{b.max.y:F3}  x {b.min.x:F3}..{b.max.x:F3}  z {b.min.z:F3}..{b.max.z:F3}");
                }
                var anim = inst.GetComponentInChildren<Animator>();
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                    if (t.name.EndsWith("UpLeg") || t.name.EndsWith("Leg") || t.name.EndsWith("Foot") || t.name.EndsWith("Hips"))
                        sb.Append($"\n  os {t.name} : {t.position:F3}");
                Object.DestroyImmediate(inst);
            }
            Debug.Log(sb.ToString());
        }

        private static void LogPuppetLegs(Animator animator, string label, AnimationClip clip)
        {
            PlayableGraph graph = default;
            if (clip != null)
            {
                graph = PlayableGraph.Create("PD_Legs");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var output = AnimationPlayableOutput.Create(graph, "pose", animator);
                var cp = AnimationClipPlayable.Create(graph, clip);
                    cp.SetApplyFootIK(false); // pas de cibles IK de pieds dans les clips cuits (le jeu ne les utilise pas)
                output.SetSourcePlayable(cp);
                cp.SetTime(0);
                graph.Evaluate(0f);
            }
            var h = animator.GetBoneTransform(HumanBodyBones.Hips);
            string P(HumanBodyBones b) { var t = animator.GetBoneTransform(b); return t != null ? h.InverseTransformPoint(t.position).ToString("F3") + "(" + t.name + ")" : "-"; }
            var handler = new HumanPoseHandler(animator.avatar, animator.transform);
            var pose = new HumanPose();
            handler.GetHumanPose(ref pose);
            handler.Dispose();
            Debug.Log($"[PD-LEGS] {label} : cuisseG {P(HumanBodyBones.LeftUpperLeg)} genouG {P(HumanBodyBones.LeftLowerLeg)} piedG {P(HumanBodyBones.LeftFoot)} | cuisseD {P(HumanBodyBones.RightUpperLeg)} genouD {P(HumanBodyBones.RightLowerLeg)} piedD {P(HumanBodyBones.RightFoot)}\n  muscles : {PDAnimBaker.LegMuscles(pose)}");
            if (clip != null) graph.Destroy();
        }

        /// <summary>Planche des pièces : chaque visuel de pièce porté par le pantin en idle (face / profil).</summary>
        private static int RenderParts(GameObject inst, Animator animator, Camera cam, GameObject camGo, RenderTexture rt, Texture2D tmp, AnimationLibrary lib)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PDPaths.PartVisuals });
            var prefabs = guids.Select(g => AssetDatabase.LoadAssetAtPath<PartVisual>(AssetDatabase.GUIDToAssetPath(g))).Where(p => p != null).OrderBy(p => p.name).ToList();
            if (prefabs.Count == 0) return 0;
            // Lignes : idle de face, idle de profil, garde de face, attaque (milieu) de face, tir de face.
            var rows = new (string clip, float at, Vector3 dir)[]
            {
                ("idle", 0f, Vector3.forward), ("idle", 0f, Vector3.right), ("garde_pose", 0.5f, Vector3.forward),
                ("sworld_att", 0.45f, Vector3.forward), ("shoot", 0.5f, Vector3.forward),
            };
            var sheet = new Texture2D(Cell * prefabs.Count, Cell * rows.Length, TextureFormat.RGB24, false);
            for (int i = 0; i < prefabs.Count; i++)
            {
                var v = Object.Instantiate(prefabs[i], inst.transform);
                v.gameObject.hideFlags = HideFlags.HideAndDontSave;
                v.AttachTo(animator);
                var roots = v.groups.Where(g => g != null && g.root != null).Select(g => g.root).ToList();
                // Journal : position de chaque maillage de la pièce par rapport aux segments de base du même os.
                var pr = inst.GetComponentInChildren<PuppetRig>(true);
                foreach (var g in v.groups)
                {
                    if (g == null || g.root == null || g.root.parent == null) continue;
                    var boneT = g.root.parent;
                    string Range(IEnumerable<Renderer> rs)
                    {
                        var list = rs.Where(x => x != null).ToList();
                        if (list.Count == 0) return "-";
                        Vector3 mn = Vector3.positiveInfinity, mx = Vector3.negativeInfinity;
                        foreach (var r in list)
                        {
                            var b = r.bounds;
                            for (int k = 0; k < 8; k++)
                            {
                                var c = new Vector3((k & 1) == 0 ? b.min.x : b.max.x, (k & 2) == 0 ? b.min.y : b.max.y, (k & 4) == 0 ? b.min.z : b.max.z);
                                var l = boneT.InverseTransformPoint(c);
                                mn = Vector3.Min(mn, l); mx = Vector3.Max(mx, l);
                            }
                        }
                        return mn.ToString("F2") + ".." + mx.ToString("F2");
                    }
                    var baseRs = pr != null ? pr.baseSegments.Where(x => x != null && x.bone == g.bone && x.renderer != null).Select(x => x.renderer) : Enumerable.Empty<Renderer>();
                    Debug.Log($"[PD-PARTS] {prefabs[i].name} / {g.bone} : pièce {Range(g.root.GetComponentsInChildren<Renderer>(true))}  segments de base {Range(baseRs)}");
                }
                // Comme en jeu : les segments du pantin couverts par la pièce sont cachés.
                var hidden = new List<Renderer>();
                var puppet = inst.GetComponentInChildren<PuppetRig>(true);
                if (puppet != null)
                    foreach (var seg in puppet.baseSegments)
                        if (seg != null && seg.renderer != null && seg.renderer.enabled && v.coveredBones.Contains(seg.bone)) { seg.renderer.enabled = false; hidden.Add(seg.renderer); }
                for (int row = 0; row < rows.Length; row++)
                {
                    var clip = lib.Get(rows[row].clip);
                    PlayableGraph graph = default;
                    if (clip != null)
                    {
                        graph = PlayableGraph.Create("PD_PartsPreview");
                        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                        var output = AnimationPlayableOutput.Create(graph, "pose", animator);
                        var cp = AnimationClipPlayable.Create(graph, clip);
                        cp.SetApplyFootIK(false);
                        output.SetSourcePlayable(cp);
                        cp.SetTime(clip.length * rows[row].at);
                        graph.Evaluate(0f);
                    }
                    Vector3 c = inst.transform.position + Vector3.up * (PDRigTools.PlayerHeight * 0.5f);
                    camGo.transform.position = c + rows[row].dir * 6.5f;
                    camGo.transform.LookAt(c);
                    cam.Render();
                    RenderTexture.active = rt;
                    tmp.ReadPixels(new Rect(0, 0, Cell, Cell), 0, 0);
                    tmp.Apply();
                    sheet.SetPixels(i * Cell, (rows.Length - 1 - row) * Cell, Cell, Cell, tmp.GetPixels());
                    if (clip != null) graph.Destroy();
                }
                foreach (var r in hidden) r.enabled = true;
                foreach (var r in roots) Object.DestroyImmediate(r.gameObject);
                Object.DestroyImmediate(v.gameObject);
            }
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, "PD_Parts.png"), sheet.EncodeToPNG());
            File.WriteAllText(Path.Combine(OutDir, "PD_Parts.txt"), string.Join("\n", prefabs.Select((p, k) => k + " : " + p.name)));
            Object.DestroyImmediate(sheet);
            return 1;
        }

        [MenuItem("Tools/Puppet Dungeon/Animations/Aperçus PNG (dossier Logs/PD_Preview)", priority = 43)]
        public static void RenderAll()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PDPaths.Prefabs + "/PD_Player.prefab");
            var lib = AssetDatabase.LoadAssetAtPath<AnimationLibrary>(PDPaths.ContentAnimation + "/PD_AnimationLibrary.asset");
            if (prefab == null || lib == null) { Debug.LogWarning("[PD-PREVIEW] Lancez d'abord le setup complet."); return; }
            Directory.CreateDirectory(OutDir);

            var root = new GameObject("PD_PreviewRig") { hideFlags = HideFlags.HideAndDontSave };
            root.transform.position = new Vector3(0f, -2000f, 0f);
            var inst = Object.Instantiate(prefab, root.transform);
            inst.hideFlags = HideFlags.HideAndDontSave;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            foreach (var mb in inst.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
            var animator = inst.GetComponentInChildren<Animator>();
            animator.enabled = true;
            LogPuppetLegs(animator, "repos du pantin", null);
            var idleClip = lib.Get("idle");
            if (idleClip != null) LogPuppetLegs(animator, "pantin avec idle image 0", idleClip);

            var camGo = new GameObject("PD_PreviewCam") { hideFlags = HideFlags.HideAndDontSave };
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.32f, 0.34f, 0.38f);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;
            var rt = new RenderTexture(Cell, Cell, 24);
            cam.targetTexture = rt;
            var lightGo = new GameObject("PD_PreviewLight") { hideFlags = HideFlags.HideAndDontSave };
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            var tmp = new Texture2D(Cell, Cell, TextureFormat.RGB24, false);
            int done = 0;
            try
            {
                foreach (var e in lib.entries.Where(x => x != null && x.result != null))
                {
                    var clip = e.result;
                    var sheet = new Texture2D(Cell * Frames, Cell * 2, TextureFormat.RGB24, false);
                    var graph = PlayableGraph.Create("PD_Preview");
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var output = AnimationPlayableOutput.Create(graph, "pose", animator);
                    var cp = AnimationClipPlayable.Create(graph, clip);
                    cp.SetApplyFootIK(false); // pas de cibles IK de pieds dans les clips cuits (le jeu ne les utilise pas)
                    output.SetSourcePlayable(cp);
                    for (int f = 0; f < Frames; f++)
                    {
                        cp.SetTime(clip.length * f / (Frames - 1));
                        graph.Evaluate(0f);
                        Vector3 c = inst.transform.position + Vector3.up * (PDRigTools.PlayerHeight * 0.5f);
                        for (int v = 0; v < 2; v++)
                        {
                            Vector3 dir = v == 0 ? Vector3.forward : Vector3.right;
                            camGo.transform.position = c + dir * 6.5f;
                            camGo.transform.LookAt(c);
                            cam.Render();
                            RenderTexture.active = rt;
                            tmp.ReadPixels(new Rect(0, 0, Cell, Cell), 0, 0);
                            tmp.Apply();
                            sheet.SetPixels(f * Cell, (1 - v) * Cell, Cell, Cell, tmp.GetPixels());
                        }
                    }
                    graph.Destroy();
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(OutDir, clip.name + ".png"), sheet.EncodeToPNG());
                    Object.DestroyImmediate(sheet);
                    done++;
                }
                done += RenderParts(inst, animator, cam, camGo, rt, tmp, lib);
                Debug.Log($"[PD-PREVIEW] {done} aperçu(s) écrit(s) dans {OutDir}");
            }
            finally
            {
                RenderTexture.active = null;
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tmp);
                Object.DestroyImmediate(root);
            }
        }
    }
}
