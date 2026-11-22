using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PuppetDungeon.EditorTools
{
    /// <summary>
    /// Cuisson des animations en clips Humanoid.
    ///
    /// Pourquoi : les FBX d'animation fournis ont été exportés sans « Bake Animation » — seuls les
    /// contrôleurs du rig Maya (nurbsCircle…) sont animés, les articulations restent figées
    /// (les contraintes et l'IK de Maya ne sont pas exportées en FBX). Unity n'a donc rien à lire.
    ///
    /// Ce que fait l'outil (sans modifier vos FBX) :
    ///  1. importe le FBX en « Generic » pour lire les courbes des contrôleurs ;
    ///  2. rejoue les contraintes décrites dans le profil de rig : orientation (bras, tête, colonne),
    ///     parent (hanches) et IK à deux os avec pole vector (jambes). Les décalages sont mesurés
    ///     dans la pose exportée, où contrôleurs et articulations sont cohérents ;
    ///  3. convertit chaque image en pose humanoïde (HumanPoseHandler) et écrit un clip .anim Humanoid
    ///     dans Generated/Animation/Baked, réciblable sur n'importe quel pantin humanoïde.
    /// Une articulation qui possède déjà ses propres courbes (FBX exporté avec « Bake Animation »)
    /// est reprise telle quelle : aucun profil n'est alors nécessaire.
    /// </summary>
    public static class PDAnimBaker
    {
        private const string Menu = "Tools/Puppet Dungeon/Animations/";

        [MenuItem(Menu + "Cuire toutes les animations", priority = 40)]
        public static void BakeAllMenu()
        {
            var lib = EnsureLibrary();
            int n = BakeAll(lib);
            AssetDatabase.SaveAssets();
            Selection.activeObject = lib;
            EditorUtility.DisplayDialog("Puppet Dungeon", $"{n} animation(s) cuite(s) dans {PDPaths.BakedAnimation}.\n\nRelancez « Setup complet » pour régénérer l'Animator et les prefabs.", "OK");
        }

        [MenuItem(Menu + "Ouvrir la bibliothèque d'animations", priority = 41)]
        public static void SelectLibrary() => Selection.activeObject = EnsureLibrary();

        // ------------------------------------------------------------------ Profil et bibliothèque

        private const int ProfileVersion = 2;

        public static AnimationRigProfile EnsureDefaultProfile()
        {
            string path = PDPaths.ContentAnimation + "/PD_RigProfile_Maya.asset";
            var p = PDPaths.LoadOrCreate<AnimationRigProfile>(path);
            if (p.dataVersion < ProfileVersion)
            {
                // Rig « rig_0010 » des FBX fournis. Décalages mesurés dans les fichiers FBX (identiques dans les
                // 12 fichiers : contraintes « maintain offset » ; articulations posées sur l'origine des contrôleurs).
                p.drivers = new List<AnimationRigProfile.ControlDriver>
                {
                    D(HumanBodyBones.Hips, "OFFSET2_Hips/nurbsCircle24", Q(0.70543f, 0.70878f, 0f, 0f), true),
                    D(HumanBodyBones.Spine, "OFFSET2_Spine/nurbsCircle25", Q(0.70543f, 0.70878f, 0f, 0f)),
                    D(HumanBodyBones.Head, "OFFSET2_Head/nurbsCircle32", Q(0.70340f, 0.71080f, 0f, 0f)),
                    D(HumanBodyBones.LeftUpperArm, "OFFSET2_LeftArm/nurbsCircle28", Q(0.70161f, -0.08797f, -0.08797f, 0.70161f)),
                    D(HumanBodyBones.LeftLowerArm, "OFFSET2_LeftForeArm/nurbsCircle30", Q(0.70186f, -0.08597f, -0.08597f, 0.70186f)),
                    D(HumanBodyBones.LeftHand, "OFFSET2_LeftHand/nurbsCircle31", Q(0.70186f, -0.08597f, -0.08597f, 0.70186f)),
                    D(HumanBodyBones.RightUpperArm, "OFFSET2_RightArm/nurbsCircle26", Q(0.06658f, -0.69861f, 0.70397f, -0.10927f)),
                    D(HumanBodyBones.RightLowerArm, "offset_arm_R/nurbsCircle37", Q(0.06458f, -0.69892f, 0.70415f, -0.10728f)),
                    D(HumanBodyBones.RightHand, "offset_arm_R1/nurbsCircle37", Q(0.06458f, -0.69892f, 0.70415f, -0.10728f)),
                };
                // Jambes en IK : pied = contrôleur de pied, genou = pivot du contrôleur de genou.
                // Attention : « genoux_R » pilote la jambe GAUCHE et « genoux_L » la droite dans ce rig.
                p.ikLimbs = new List<AnimationRigProfile.IkLimb>
                {
                    new AnimationRigProfile.IkLimb { upper = HumanBodyBones.LeftUpperLeg, lower = HumanBodyBones.LeftLowerLeg, end = HumanBodyBones.LeftFoot,
                        goal = "OFFSET2_LeftFoot/nurbsCircle23", pole = "OFFSET_genoux_R/nurbsCircle35",
                        useStoredOffset = true, storedGoal = Vector3.zero, polePivot = new Vector3(1.968798f, 8.726612f, 3.113337f) },
                    new AnimationRigProfile.IkLimb { upper = HumanBodyBones.RightUpperLeg, lower = HumanBodyBones.RightLowerLeg, end = HumanBodyBones.RightFoot,
                        goal = "OFFSET2_RightFoot/nurbsCircle20", pole = "OFFSET_genoux_L/nurbsCircle33",
                        useStoredOffset = true, storedGoal = Vector3.zero, polePivot = Vector3.zero },
                };
                p.unitReferenceBone = HumanBodyBones.Spine;
                p.unitReferenceLength = 7.298336f;
                p.dataVersion = ProfileVersion;
            }
            if (p.restPose == null) p.restPose = PDPaths.Find<GameObject>(PDPaths.AnimRestPose);
            EditorUtility.SetDirty(p);
            return p;
        }

        private static Quaternion Q(float x, float y, float z, float w) => new Quaternion(x, y, z, w);

        /// <summary>Axes Maya (main droite) → Unity (main gauche) : l'import FBX inverse l'axe X.</summary>
        private static Quaternion MayaToUnity(Quaternion q)
        {
            var u = new Quaternion(q.x, -q.y, -q.z, q.w);
            float m = Mathf.Sqrt(u.x * u.x + u.y * u.y + u.z * u.z + u.w * u.w);
            return m > 1e-6f ? new Quaternion(u.x / m, u.y / m, u.z / m, u.w / m) : Quaternion.identity;
        }

        private static Vector3 MayaToUnity(Vector3 v, float unit) => new Vector3(-v.x, v.y, v.z) * unit;

        private static AnimationRigProfile.ControlDriver D(HumanBodyBones b, string c, Quaternion offset, bool pos = false)
            => new AnimationRigProfile.ControlDriver { bone = b, control = c, followPosition = pos, useStoredOffset = true, storedRotation = offset, storedPosition = Vector3.zero };

        /// <summary>Bibliothèque (Content/Animation) complétée avec les nouveaux FBX du dossier d'animations.</summary>
        public static AnimationLibrary EnsureLibrary()
        {
            var lib = PDPaths.LoadOrCreate<AnimationLibrary>(PDPaths.ContentAnimation + "/PD_AnimationLibrary.asset");
            // Toujours appelé : met aussi à jour un profil créé par une version précédente de l'outil.
            var defaultProfile = EnsureDefaultProfile();
            if (lib.defaultProfile == null) lib.defaultProfile = defaultProfile;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { PDPaths.AnimationsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null || lib.entries.Any(e => e != null && e.source == go)) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                string low = name.ToLowerInvariant();
                lib.entries.Add(new AnimationLibrary.Entry
                {
                    source = go,
                    clipName = name,
                    loop = low.Contains("cicle") || low.Contains("cycle") || low.Contains("walk") || low.Contains("run") || low.Contains("idle") || low.Contains("loop"),
                    // ALL_anime = toutes les animations bout à bout (sert de pose de repos) ;
                    // anime_marche_rig = ancienne version du rig (autres noms de contrôleurs).
                    bake = low != "all_anime" && low != "anime_marche_rig",
                });
            }
            EditorUtility.SetDirty(lib);
            return lib;
        }

        // ------------------------------------------------------------------ Cuisson

        private static readonly Dictionary<string, Avatar> avatarCache = new Dictionary<string, Avatar>();
        /// <summary>Journal détaillé (positions des jambes, muscles) pour le clip « idle ».</summary>
        public static bool DebugLegs;
        private static readonly Dictionary<string, Vector3> poleCalibration = new Dictionary<string, Vector3>();

        public static int BakeAll(AnimationLibrary lib)
        {
            avatarCache.Clear();
            poleCalibration.Clear();
            int n = 0;
            try
            {
                CalibratePoles(lib);
                for (int i = 0; i < lib.entries.Count; i++)
                {
                    var e = lib.entries[i];
                    if (e == null || !e.bake || e.source == null) continue;
                    EditorUtility.DisplayProgressBar("Puppet Dungeon — animations", "Cuisson : " + e.clipName, i / (float)Mathf.Max(1, lib.entries.Count));
                    var clip = Bake(e, e.profile != null ? e.profile : lib.defaultProfile);
                    if (clip != null) { e.result = clip; n++; }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                avatarCache.Clear();
                poleCalibration.Clear();
            }
            EditorUtility.SetDirty(lib);
            return n;
        }

        private static string PoleKey(AnimationRigProfile p, int limb) => (p != null ? p.name : "-") + "#" + limb;

        /// <summary>
        /// Le pole vector Maya vise le PIVOT de rotation du contrôleur de genou, qui peut être décalé de son
        /// origine (c'est le cas de « genoux_R »). Plutôt que de dépendre de la façon dont l'import convertit
        /// les pivots, on retrouve ce point (dans le repère du contrôleur) par moindres carrés : dans chaque
        /// fichier, le genou exporté est dans le plan (hanche, pied, pole). Un fichier = une équation.
        /// </summary>
        private static void CalibratePoles(AnimationLibrary lib)
        {
            var byProfile = lib.entries.Where(e => e != null && e.bake && e.source != null)
                .GroupBy(e => e.profile != null ? e.profile : lib.defaultProfile);
            foreach (var group in byProfile)
            {
                var profile = group.Key;
                if (profile == null) continue;
                for (int li = 0; li < profile.ikLimbs.Count; li++)
                {
                    var limb = profile.ikLimbs[li];
                    if (limb == null || limb.useStoredOffset) continue;
                    var rows = new List<(Vector3 a, float c, float scale)>();
                    foreach (var e in group)
                    {
                        var inst = Object.Instantiate(e.source);
                        inst.hideFlags = HideFlags.HideAndDontSave;
                        try
                        {
                            var bones = PDAvatarTools.ResolveBones(inst.transform, profile);
                            if (!bones.TryGetValue(limb.upper, out var up) || !bones.TryGetValue(limb.lower, out var lo) || !bones.TryGetValue(limb.end, out var en)) continue;
                            var pole = FindControl(inst.transform, limb.pole);
                            if (pole == null) continue;
                            Vector3 a = up.position, b = lo.position, c = en.position;
                            Vector3 n = Vector3.Cross(b - a, c - b);
                            if (n.sqrMagnitude < 1e-12f) continue;
                            n.Normalize();
                            // n · (pole.position + L·v - a) = 0  →  (Lᵀn) · v = n · (a - pole.position)
                            Vector3 lt = Vector3.Scale(pole.lossyScale, Quaternion.Inverse(pole.rotation) * n);
                            rows.Add((lt, Vector3.Dot(n, a - pole.position), Vector3.Distance(a, b)));
                        }
                        finally { Object.DestroyImmediate(inst); }
                    }
                    if (rows.Count < 3) continue;
                    var v = SolveLeastSquares(rows.Select(r => r.a).ToList(), rows.Select(r => r.c).ToList(), out bool ok);
                    if (!ok) continue;
                    float worst = rows.Max(r => Mathf.Abs(Vector3.Dot(r.a, v) - r.c) / Mathf.Max(1e-5f, r.scale));
                    if (worst > 0.05f)
                    {
                        Debug.LogWarning($"[PuppetDungeon] IK {limb.upper} : pole vector non cohérent entre les fichiers (écart {worst:P0}) — on garde l'origine du contrôleur « {limb.pole} ».");
                        continue;
                    }
                    poleCalibration[PoleKey(profile, li)] = v;
                    Debug.Log($"[PuppetDungeon] IK {limb.upper} : point visé par le pole vector calibré sur {rows.Count} fichiers (décalage local {v.ToString("F3")}, écart max {worst:P1}).");
                }
            }
        }

        private static Vector3 SolveLeastSquares(List<Vector3> a, List<float> c, out bool ok)
        {
            // Équations normales (AᵀA) v = Aᵀc, résolues par élimination de Gauss avec pivot partiel.
            var m = new double[3, 4];
            for (int i = 0; i < a.Count; i++)
                for (int r = 0; r < 3; r++)
                {
                    for (int k = 0; k < 3; k++) m[r, k] += a[i][r] * a[i][k];
                    m[r, 3] += a[i][r] * c[i];
                }
            double trace = m[0, 0] + m[1, 1] + m[2, 2];
            for (int r = 0; r < 3; r++) m[r, r] += 1e-9 * trace;
            for (int col = 0; col < 3; col++)
            {
                int piv = col;
                for (int r = col + 1; r < 3; r++) if (Math.Abs(m[r, col]) > Math.Abs(m[piv, col])) piv = r;
                if (Math.Abs(m[piv, col]) < 1e-12 * Math.Max(1e-30, trace)) { ok = false; return Vector3.zero; }
                for (int k = 0; k < 4; k++) { var t = m[col, k]; m[col, k] = m[piv, k]; m[piv, k] = t; }
                for (int r = 0; r < 3; r++)
                {
                    if (r == col) continue;
                    double f = m[r, col] / m[col, col];
                    for (int k = 0; k < 4; k++) m[r, k] -= f * m[col, k];
                }
            }
            ok = true;
            return new Vector3((float)(m[0, 3] / m[0, 0]), (float)(m[1, 3] / m[1, 1]), (float)(m[2, 3] / m[2, 2]));
        }

        private class TCurves
        {
            public Transform t;
            public readonly AnimationCurve[] pos = new AnimationCurve[3], rotQ = new AnimationCurve[4], rotE = new AnimationCurve[3], scl = new AnimationCurve[3];
            public bool HasRotation => rotQ.Any(c => c != null) || rotE.Any(c => c != null);

            public void Apply(float time)
            {
                if (pos.Any(c => c != null))
                {
                    var v = t.localPosition;
                    for (int i = 0; i < 3; i++) if (pos[i] != null) v[i] = pos[i].Evaluate(time);
                    t.localPosition = v;
                }
                if (rotQ.Any(c => c != null))
                {
                    var q = t.localRotation;
                    float x = rotQ[0] != null ? rotQ[0].Evaluate(time) : q.x;
                    float y = rotQ[1] != null ? rotQ[1].Evaluate(time) : q.y;
                    float z = rotQ[2] != null ? rotQ[2].Evaluate(time) : q.z;
                    float w = rotQ[3] != null ? rotQ[3].Evaluate(time) : q.w;
                    float m = Mathf.Sqrt(x * x + y * y + z * z + w * w);
                    if (m > 1e-6f) t.localRotation = new Quaternion(x / m, y / m, z / m, w / m);
                }
                else if (rotE.Any(c => c != null))
                {
                    var e = t.localEulerAngles;
                    for (int i = 0; i < 3; i++) if (rotE[i] != null) e[i] = rotE[i].Evaluate(time);
                    t.localEulerAngles = e;
                }
                if (scl.Any(c => c != null))
                {
                    var s = t.localScale;
                    for (int i = 0; i < 3; i++) if (scl[i] != null) s[i] = scl[i].Evaluate(time);
                    t.localScale = s;
                }
            }

            public bool RotationVaries()
            {
                foreach (var c in rotQ.Concat(rotE))
                {
                    if (c == null || c.length < 2) continue;
                    float mn = float.MaxValue, mx = float.MinValue;
                    foreach (var k in c.keys) { mn = Mathf.Min(mn, k.value); mx = Mathf.Max(mx, k.value); }
                    if (mx - mn > 1e-3f) return true;
                }
                return false;
            }
        }

        private static List<TCurves> ReadCurves(AnimationClip clip, Transform root)
        {
            var map = new Dictionary<Transform, TCurves>();
            foreach (var b in AnimationUtility.GetCurveBindings(clip))
            {
                if (b.type != typeof(Transform)) continue;
                var t = string.IsNullOrEmpty(b.path) ? root : root.Find(b.path);
                if (t == null) continue;
                int dot = b.propertyName.LastIndexOf('.');
                if (dot < 0) continue;
                string prop = b.propertyName.Substring(0, dot);
                int comp = "xyzw".IndexOf(b.propertyName[dot + 1]);
                if (comp < 0) continue;
                if (!map.TryGetValue(t, out var tc)) map[t] = tc = new TCurves { t = t };
                var curve = AnimationUtility.GetEditorCurve(clip, b);
                if (curve == null) continue;
                switch (prop)
                {
                    case "m_LocalPosition": if (comp < 3) tc.pos[comp] = curve; break;
                    case "m_LocalRotation": tc.rotQ[comp] = curve; break;
                    case "localEulerAnglesRaw":
                    case "localEulerAngles":
                    case "localEulerAnglesBaked":
                    case "m_LocalEulerAngles": if (comp < 3) tc.rotE[comp] = curve; break;
                    case "m_LocalScale": if (comp < 3) tc.scl[comp] = curve; break;
                }
            }
            return map.Values.ToList();
        }

        private class FkSolve
        {
            public Transform joint, control;
            public Quaternion offRot;
            public Vector3 offPos;
            public bool position;
            public int depth;

            public void Apply()
            {
                joint.rotation = control.rotation * offRot;
                if (position) joint.position = control.TransformPoint(offPos);
            }
        }

        private class IkSolve
        {
            public Transform upper, lower, end, goal, pole;
            public Vector3 goalLocal, poleLocal, upDirL, upHingeL, loDirL, loHingeL, lastPd;
            public float l1, l2;

            public void Apply()
            {
                Vector3 a = upper.position;
                Vector3 g = goal.TransformPoint(goalLocal);
                Vector3 p = pole.TransformPoint(poleLocal);
                Vector3 dv = g - a;
                float dist = dv.magnitude;
                if (dist < 1e-6f) return;
                Vector3 axis = dv / dist;
                float d = Mathf.Clamp(dist, Mathf.Abs(l1 - l2) + 1e-4f, l1 + l2 - 1e-4f);
                Vector3 pd = (p - a) - Vector3.Dot(p - a, axis) * axis;
                if (pd.sqrMagnitude < 1e-10f) pd = lastPd; else pd.Normalize();
                lastPd = pd;
                float along = (l1 * l1 - l2 * l2 + d * d) / (2f * d);
                float h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - along * along));
                Vector3 knee = a + axis * along + pd * h;
                Vector3 foot = a + axis * d;
                // Plan de la jambe = plan du pole vector ; charnière = normale de ce plan.
                Vector3 n = Vector3.Cross(pd, axis).normalized;
                upper.rotation = Quaternion.LookRotation(knee - a, n) * Quaternion.Inverse(Quaternion.LookRotation(upDirL, upHingeL));
                lower.rotation = Quaternion.LookRotation(foot - knee, n) * Quaternion.Inverse(Quaternion.LookRotation(loDirL, loHingeL));
            }
        }

        public static Transform FindControl(Transform root, string spec)
        {
            if (string.IsNullOrEmpty(spec)) return null;
            var cur = root;
            foreach (var token in spec.Split('/'))
            {
                string tk = token.Trim();
                if (tk.Length == 0) continue;
                Transform found = null;
                var queue = new Queue<Transform>();
                foreach (Transform c in cur) queue.Enqueue(c);
                while (queue.Count > 0 && found == null)
                {
                    var t = queue.Dequeue();
                    if (string.Equals(PDAvatarTools.Strip(t.name), tk, StringComparison.OrdinalIgnoreCase)) { found = t; break; }
                    foreach (Transform c in t) queue.Enqueue(c);
                }
                if (found == null) return null;
                cur = found;
            }
            return cur;
        }

        private static int Depth(Transform t) { int d = 0; for (; t != null; t = t.parent) d++; return d; }

        /// <summary>Squelette propre (articulations seulement) : sert à l'avatar et au HumanPoseHandler.</summary>
        private static GameObject BuildProxy(Transform hips, Dictionary<Transform, Transform> pairs)
        {
            var root = new GameObject("PD_ProxyRoot") { hideFlags = HideFlags.HideAndDontSave };
            void Copy(Transform src, Transform parent)
            {
                if (src.GetComponent<Renderer>() != null) return;
                var dst = new GameObject(src.name) { hideFlags = HideFlags.HideAndDontSave }.transform;
                dst.SetParent(parent, false);
                dst.localPosition = src.localPosition;
                dst.localRotation = src.localRotation;
                dst.localScale = src.localScale;
                pairs[src] = dst;
                foreach (Transform c in src) Copy(c, dst);
            }
            Copy(hips, root.transform);
            return root;
        }

        public static AnimationClip Bake(AnimationLibrary.Entry e, AnimationRigProfile profile)
        {
            string path = AssetDatabase.GetAssetPath(e.source);
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { Debug.LogWarning("[PuppetDungeon] Pas un modèle : " + path); return null; }
            EnsureGenericImport(mi);
            var srcClip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (srcClip == null || model == null) { Debug.LogWarning("[PuppetDungeon] Aucune animation dans " + path); return null; }

            var inst = Object.Instantiate(model);
            inst.hideFlags = HideFlags.HideAndDontSave;
            inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            GameObject proxy = null, rest = null, check = null;
            HumanPoseHandler handler = null, checkHandler = null;
            try
            {
                var bones = PDAvatarTools.ResolveBones(inst.transform, profile);
                if (!PDAvatarTools.HasRequiredBones(bones, out var missing))
                {
                    Debug.LogError($"[PuppetDungeon] {path} : articulations humanoïdes introuvables ({missing}). Renseignez « Bone Names » dans le profil de rig.");
                    return null;
                }
                var curves = ReadCurves(srcClip, inst.transform);
                float unit = 1f;
                if (profile != null && profile.unitReferenceLength > 0f && bones.TryGetValue(profile.unitReferenceBone, out var refBone))
                    unit = refBone.localPosition.magnitude / profile.unitReferenceLength;
                var animatedJoints = new HashSet<Transform>(curves.Where(c => c.HasRotation && c.RotationVaries() && bones.ContainsValue(c.t)).Select(c => c.t));

                // --- Contraintes (mesurées dans la pose exportée, avant toute lecture des courbes)
                var fk = new List<FkSolve>();
                var ik = new List<IkSolve>();
                var warnings = new List<string>();
                if (profile != null)
                {
                    foreach (var d in profile.drivers)
                    {
                        if (d == null || !bones.TryGetValue(d.bone, out var j) || animatedJoints.Contains(j)) continue;
                        var c = FindControl(inst.transform, d.control);
                        if (c == null) { warnings.Add("contrôleur introuvable « " + d.control + " »"); continue; }
                        var solve = new FkSolve { joint = j, control = c, position = d.followPosition, depth = Depth(j) };
                        if (d.useStoredOffset)
                        {
                            solve.offRot = MayaToUnity(d.storedRotation);
                            solve.offPos = MayaToUnity(d.storedPosition, unit);
                        }
                        else
                        {
                            solve.offRot = Quaternion.Inverse(c.rotation) * j.rotation;
                            solve.offPos = c.InverseTransformPoint(j.position);
                        }
                        fk.Add(solve);
                    }
                    for (int li = 0; li < profile.ikLimbs.Count; li++)
                    {
                        var l = profile.ikLimbs[li];
                        if (l == null || !bones.TryGetValue(l.upper, out var up) || !bones.TryGetValue(l.lower, out var lo) || !bones.TryGetValue(l.end, out var en)) continue;
                        if (animatedJoints.Contains(up) || animatedJoints.Contains(lo)) continue;
                        var goal = FindControl(inst.transform, l.goal);
                        var pole = FindControl(inst.transform, l.pole);
                        if (goal == null || pole == null) { warnings.Add($"IK {l.upper} : contrôleur introuvable ({l.goal} / {l.pole})"); continue; }
                        IkSolve ikS;
                        string ikWarn;
                        if (l.useStoredOffset)
                        {
                            // Le pivot du contrôleur de genou peut être (ou non) devenu son origine à l'import :
                            // on garde le candidat situé à hauteur du genou.
                            Vector3 pivotLocal = MayaToUnity(l.polePivot, unit);
                            float dOrigin = Mathf.Abs(pole.position.y - lo.position.y);
                            float dPivot = Mathf.Abs(pole.TransformPoint(pivotLocal).y - lo.position.y);
                            Vector3 poleLocal = dPivot < dOrigin ? pivotLocal : Vector3.zero;
                            ikS = MakeIk(up, lo, en, goal, pole, poleLocal, MayaToUnity(l.storedGoal, unit), out ikWarn);
                        }
                        else
                        {
                            poleCalibration.TryGetValue(PoleKey(profile, li), out var poleLocal);
                            ikS = MakeIk(up, lo, en, goal, pole, poleLocal, null, out ikWarn);
                        }
                        if (ikWarn != null) warnings.Add(ikWarn);
                        if (ikS != null) ik.Add(ikS);
                    }
                }
                fk.Sort((a, b) => a.depth.CompareTo(b.depth));

                // --- Report sur le squelette du pantin.
                // Chaque os du pantin reçoit la rotation de l'os correspondant du rig d'animation, mesurée par
                // rapport à leur pose de repos respective (les deux sont debout, bras légèrement baissés). La pose
                // est ensuite convertie en Humanoid avec l'avatar DU PANTIN : le clip est relu par l'avatar qui l'a
                // écrit. (Passer par un second avatar faisait déraper les jambes : l'IK Maya vrille les cuisses
                // d'environ 30°, ce que l'avatar du pantin, aux jambes parfaitement droites, réinterprétait mal.)
                var tgtAvatar = PDAvatarTools.PuppetAvatar;
                if (tgtAvatar == null) tgtAvatar = PDAvatarTools.BuildPuppetAvatar();
                var tgtModel = PDRigTools.RigModel;
                if (tgtAvatar == null || tgtModel == null) { Debug.LogError("[PuppetDungeon] Avatar ou rig du pantin introuvable."); return null; }
                proxy = Object.Instantiate(tgtModel);
                proxy.hideFlags = HideFlags.HideAndDontSave;
                proxy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                proxy.transform.localScale = Vector3.one;
                check = Object.Instantiate(tgtModel);
                check.hideFlags = HideFlags.HideAndDontSave;
                check.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                check.transform.localScale = Vector3.one;
                var tgtBones = PDAvatarTools.ResolveBones(proxy.transform);
                var chkBones = PDAvatarTools.ResolveBones(check.transform);
                var tgtRest = tgtBones.ToDictionary(kv => kv.Key, kv => kv.Value.rotation);
                Vector3 tgtRestHips = tgtBones[HumanBodyBones.Hips].position;

                var srcRest = new Dictionary<HumanBodyBones, Quaternion>();
                var srcRestPos = new Dictionary<HumanBodyBones, Vector3>();
                Vector3 srcRestHips = bones[HumanBodyBones.Hips].position;
                float srcLeg = 0f;
                if (profile != null && profile.restPose != null)
                {
                    rest = Object.Instantiate(profile.restPose);
                    rest.hideFlags = HideFlags.HideAndDontSave;
                    rest.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    var byName = new Dictionary<string, Transform>();
                    foreach (var t in rest.GetComponentsInChildren<Transform>(true)) if (!byName.ContainsKey(t.name)) byName[t.name] = t;
                    foreach (var kv in bones)
                        if (byName.TryGetValue(kv.Value.name, out var r)) { srcRest[kv.Key] = r.rotation; srcRestPos[kv.Key] = r.position; }
                    if (byName.TryGetValue(bones[HumanBodyBones.Hips].name, out var rh)) srcRestHips = rh.position;
                    if (byName.TryGetValue(bones[HumanBodyBones.LeftFoot].name, out var rf)) srcLeg = rh != null ? (rh.position.y - rf.position.y) : 0f;
                    if (srcRest.Count < bones.Count / 2) warnings.Add("pose de repos incomplète (" + profile.restPose.name + ")");
                }
                else warnings.Add("pas de pose de repos : la pose du fichier sert de repos");
                foreach (var kv in bones) if (!srcRest.ContainsKey(kv.Key)) { srcRest[kv.Key] = kv.Value.rotation; srcRestPos[kv.Key] = kv.Value.position; }
                if (srcLeg <= 0f) srcLeg = bones[HumanBodyBones.Hips].position.y - bones[HumanBodyBones.LeftFoot].position.y;
                float tgtLeg = tgtRestHips.y - tgtBones[HumanBodyBones.LeftFoot].position.y;
                float legScale = srcLeg > 1e-5f ? tgtLeg / srcLeg : 1f;
                var order = tgtBones.Keys.Where(k => bones.ContainsKey(k)).OrderBy(k => Depth(tgtBones[k])).ToList();
                var tgtRestPos = tgtBones.ToDictionary(kv => kv.Key, kv => kv.Value.position);
                // Repère commun : bassin (haut = colonne, côté = cuisse droite - cuisse gauche) au repos.
                Quaternion srcBodyRest = BodyFrame(srcRestPos), tgtBodyRest = BodyFrame(tgtRestPos);
                Quaternion toTgt = tgtBodyRest * Quaternion.Inverse(srcBodyRest);
                // Parent humanoïde de chaque os (pour les os « bouts » : pieds, mains, tête).
                var srcIds = bones.ToDictionary(kv => kv.Value, kv => kv.Key);
                var humanParent = new Dictionary<HumanBodyBones, HumanBodyBones>();
                foreach (var kv in bones)
                    for (var pt = kv.Value.parent; pt != null; pt = pt.parent)
                        if (srcIds.TryGetValue(pt, out var pid)) { humanParent[kv.Key] = pid; break; }
                var srcNowPos = new Dictionary<HumanBodyBones, Vector3>();
                                var srcDelta = new Dictionary<HumanBodyBones, Quaternion>();
                float worstPose = 0f;

                handler = new HumanPoseHandler(tgtAvatar, proxy.transform);
                checkHandler = new HumanPoseHandler(tgtAvatar, check.transform);
                bool dbg = DebugLegs && e.clipName.ToLowerInvariant() == "idle";
                float worstFoot = 0f;

                // --- Échantillonnage
                int rate = profile != null ? profile.sampleRate : 30;
                float length = Mathf.Max(srcClip.length, 1f / rate);
                int frames = Mathf.Max(2, Mathf.RoundToInt(length * rate) + 1);
                int mc = HumanTrait.MuscleCount;
                var muscleNames = HumanTrait.MuscleName;
                var keep = Enumerable.Range(0, mc).Where(i => !IsFinger(muscleNames[i])).ToArray();
                var times = new float[frames];
                var muscles = new float[mc][];
                for (int i = 0; i < mc; i++) muscles[i] = new float[frames];
                var rootT = new Vector3[frames];
                var rootQ = new Quaternion[frames];
                var pose = new HumanPose();
                for (int f = 0; f < frames; f++)
                {
                    float t = length * f / (frames - 1);
                    times[f] = t;
                    foreach (var c in curves) c.Apply(t);
                    foreach (var s in fk) s.Apply();
                    foreach (var s in ik) s.Apply();
                    // Report par DIRECTIONS : chaque segment du pantin (cuisse, tibia, bras, avant-bras, colonne) est
                    // orienté exactement comme le segment correspondant du rig d'animation, mesuré sur les positions
                    // des articulations. Aucune comparaison d'axes d'os entre les deux rigs : la vrille que l'IK Maya
                    // donne aux cuisses n'est plus recopiée (c'est elle qui repliait les jambes du pantin).
                    srcNowPos.Clear(); srcDelta.Clear();
                    foreach (var kv in bones) { srcNowPos[kv.Key] = kv.Value.position; srcDelta[kv.Key] = kv.Value.rotation * Quaternion.Inverse(srcRest[kv.Key]); }
                    Quaternion srcBodyNow = BodyFrame(srcNowPos);
                    foreach (var b in order)
                    {
                        var tb = tgtBones[b];
                        if (b == HumanBodyBones.Hips)
                        {
                            // Le bassin du pantin prend l'orientation du bassin source dans le repère du MONDE source
                            // (personnage tourné vers +Z) : le clip Humanoid regarde donc vers l'avant du jeu, même si le
                            // modèle du pantin est importé dos tourné.
                            tb.rotation = srcBodyNow * Quaternion.Inverse(tgtBodyRest) * tgtRest[b];
                            continue;
                        }
                        if (SegmentEnd(b, bones, tgtBones, out var endB))
                        {
                            // Bras : la rotation propre (pronation, poignet tourné) vient de l'articulation source
                            // elle-même (contrôleurs FK de Maya) ; elle oriente correctement l'arme / le bouclier tenu.
                            // Jambes et colonne : axe du bassin / des épaules (la vrille de l'IK n'a pas de sens ici).
                            Vector3 sec;
                            if (b == HumanBodyBones.LeftUpperArm || b == HumanBodyBones.RightUpperArm)
                                sec = UpperArmSecondary(b, srcNowPos);          // plan du coude (charnière Humanoid)
                            else if (b == HumanBodyBones.LeftLowerArm || b == HumanBodyBones.RightLowerArm)
                                sec = srcDelta[b] * SegmentSecondary(b, srcRestPos); // pronation réelle de l'avant-bras
                            else
                                sec = SegmentSecondary(b, srcNowPos);
                            Vector3 secRest = SegmentSecondary(b, tgtRestPos);
                            Quaternion now = Frame(srcNowPos[endB] - srcNowPos[b], sec);
                            Quaternion restF = Frame(tgtRestPos[endB] - tgtRestPos[b], secRest);
                            tb.rotation = now * Quaternion.Inverse(restF) * tgtRest[b];
                            continue;
                        }
                        // Os « bout » (pied, main, tête) : on reporte son mouvement PAR RAPPORT à son parent.
                        Quaternion rel = humanParent.TryGetValue(b, out var pb) && tgtBones.ContainsKey(pb)
                            ? Quaternion.Inverse(srcDelta[pb]) * srcDelta[b] : srcDelta[b];
                        Quaternion parentDelta = humanParent.TryGetValue(b, out pb) && tgtBones.ContainsKey(pb)
                            ? tgtBones[pb].rotation * Quaternion.Inverse(tgtRest[pb]) : Quaternion.identity;
                        tb.rotation = parentDelta * (toTgt * rel * Quaternion.Inverse(toTgt)) * tgtRest[b];
                    }
                    tgtBones[HumanBodyBones.Hips].position = tgtRestHips + (bones[HumanBodyBones.Hips].position - srcRestHips) * legScale;
                    handler.GetHumanPose(ref pose);
                    // Contrôle : la pose relue par l'avatar doit remettre les pieds au même endroit.
                    checkHandler.SetHumanPose(ref pose);
                    // Contrôle : chaque segment (cuisse, tibia, bras, avant-bras, colonne) relu par l'avatar doit
                    // garder la direction voulue (détecte les poses que les limites musculaires Humanoid écrasent).
                    foreach (var seg in SegmentEnds.Keys)
                    {
                        if (!tgtBones.ContainsKey(seg) || !chkBones.ContainsKey(seg) || !SegmentEnd(seg, tgtBones, chkBones, out var segEnd)) continue;
                        Vector3 wantDir = tgtBones[segEnd].position - tgtBones[seg].position, gotDir = chkBones[segEnd].position - chkBones[seg].position;
                        if (wantDir.sqrMagnitude > 1e-12f && gotDir.sqrMagnitude > 1e-12f) worstPose = Mathf.Max(worstPose, Vector3.Angle(wantDir, gotDir));
                    }
                    foreach (var fb in new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
                    {
                        Vector3 want = tgtBones[fb].position - tgtBones[HumanBodyBones.Hips].position;
                        Vector3 got = chkBones[fb].position - chkBones[HumanBodyBones.Hips].position;
                        worstFoot = Mathf.Max(worstFoot, (want - got).magnitude / Mathf.Max(1e-5f, tgtLeg));
                    }
                    if (dbg && f == 0)
                    {
                        var h = bones[HumanBodyBones.Hips];
                        string P(HumanBodyBones b) => bones.TryGetValue(b, out var t) ? h.InverseTransformPoint(t.position).ToString("F3") + "/w" + t.position.ToString("F3") : "-";
                        Debug.Log("[PD-LEGS] idle image 0 (source, repère hanches) : cuisseG " + P(HumanBodyBones.LeftUpperLeg) + " genouG " + P(HumanBodyBones.LeftLowerLeg) + " piedG " + P(HumanBodyBones.LeftFoot)
                                  + " | cuisseD " + P(HumanBodyBones.RightUpperLeg) + " genouD " + P(HumanBodyBones.RightLowerLeg) + " piedD " + P(HumanBodyBones.RightFoot));
                        foreach (var s in ik)
                            Debug.Log($"[PD-LEGS] IK {s.upper.name} : cible {s.goal.TransformPoint(s.goalLocal):F3} pole {s.pole.TransformPoint(s.poleLocal):F3} (origine pole {s.pole.position:F3}) l1={s.l1:F4} l2={s.l2:F4}");
                        Debug.Log("[PD-LEGS] idle image 0 muscles : " + LegMuscles(pose));
                        var th = tgtBones[HumanBodyBones.Hips];
                        string Q(HumanBodyBones b) => tgtBones.TryGetValue(b, out var t) ? th.InverseTransformPoint(t.position).ToString("F3") + "/w" + t.position.ToString("F3") : "-";
                        Debug.Log("[PD-LEGS] idle image 0 (pantin reporté) : cuisseG " + Q(HumanBodyBones.LeftUpperLeg) + " genouG " + Q(HumanBodyBones.LeftLowerLeg) + " piedG " + Q(HumanBodyBones.LeftFoot)
                                  + " | genouD " + Q(HumanBodyBones.RightLowerLeg) + " piedD " + Q(HumanBodyBones.RightFoot) + $" | repère {Quaternion.Angle(Quaternion.identity, toTgt):0.0}°");
                    }
                    for (int i = 0; i < mc && i < pose.muscles.Length; i++) muscles[i][f] = pose.muscles[i];
                    rootT[f] = pose.bodyPosition;
                    var q = pose.bodyRotation;
                    if (f > 0 && Quaternion.Dot(q, rootQ[f - 1]) < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                    rootQ[f] = q;
                }

                // --- Clip Humanoid
                PDPaths.EnsureFolder(PDPaths.BakedAnimation);
                string outPath = PDPaths.BakedAnimation + "/PD_" + Safe(e.clipName) + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath);
                bool create = clip == null;
                if (create) clip = new AnimationClip();
                clip.ClearCurves();
                clip.frameRate = rate;
                var bindings = new List<EditorCurveBinding>();
                var built = new List<AnimationCurve>();
                void Add(string prop, Func<int, float> value)
                {
                    var keys = new Keyframe[frames];
                    for (int f = 0; f < frames; f++) keys[f] = new Keyframe(times[f], value(f));
                    var curve = new AnimationCurve(keys);
                    for (int k = 0; k < frames; k++)
                    {
                        AnimationUtility.SetKeyLeftTangentMode(curve, k, AnimationUtility.TangentMode.ClampedAuto);
                        AnimationUtility.SetKeyRightTangentMode(curve, k, AnimationUtility.TangentMode.ClampedAuto);
                    }
                    bindings.Add(EditorCurveBinding.FloatCurve("", typeof(Animator), prop));
                    built.Add(curve);
                }
                Add("RootT.x", f => rootT[f].x);
                Add("RootT.y", f => rootT[f].y);
                Add("RootT.z", f => rootT[f].z);
                Add("RootQ.x", f => rootQ[f].x);
                Add("RootQ.y", f => rootQ[f].y);
                Add("RootQ.z", f => rootQ[f].z);
                Add("RootQ.w", f => rootQ[f].w);
                foreach (int i in keep) { int mi2 = i; Add(muscleNames[i], f => muscles[mi2][f]); }
                AnimationUtility.SetEditorCurves(clip, bindings.ToArray(), built.ToArray());

                var st = AnimationUtility.GetAnimationClipSettings(clip);
                st.loopTime = e.loop;
                st.loopBlend = e.loop;
                st.cycleOffset = 0f;
                st.mirror = false;
                st.heightFromFeet = false;
                // Rotation et hauteur gardées dans la pose ; déplacement horizontal extrait (et ignoré :
                // applyRootMotion = false) → le pantin reste centré sur son contrôleur.
                st.loopBlendOrientation = true;
                st.keepOriginalOrientation = true;
                st.loopBlendPositionY = true;
                st.keepOriginalPositionY = true;
                st.loopBlendPositionXZ = false;
                st.keepOriginalPositionXZ = false;
                AnimationUtility.SetAnimationClipSettings(clip, st);
                if (create) AssetDatabase.CreateAsset(clip, outPath);
                EditorUtility.SetDirty(clip);

                // --- Rapport
                int legIndex = Array.IndexOf(muscleNames, "Left Upper Leg Front-Back");
                string range = legIndex >= 0 ? $"{muscles[legIndex].Min():0.00}..{muscles[legIndex].Max():0.00}" : "-";
                string mode = animatedJoints.Count > 0 ? $"{animatedJoints.Count} articulation(s) déjà animée(s), " : "";
                if (worstFoot > 0.08f) warnings.Add($"écart des pieds après relecture Humanoid : {worstFoot:P0} de la jambe");
                if (Mathf.Abs(FacingYaw(rootQ[0])) > 60f) warnings.Add($"le corps regarde à {FacingYaw(rootQ[0]):0}° de l'avant : le personnage risque de marcher à reculons");
                if (worstPose > 10f) warnings.Add($"pose modifiée par l'avatar Humanoid : un segment dévie de {worstPose:0}°");
                Debug.Log($"[PuppetDungeon] Animation cuite : {Path.GetFileName(outPath)} ({frames} images, {mode}{fk.Count} contrainte(s), {ik.Count} IK, jambe G avant-arrière {range})"
                          + $", écart pieds {worstFoot:P0}, écart membres {worstPose:0.0}°, orientation {FacingYaw(rootQ[0]):0}°" + (warnings.Count > 0 ? "\n  ⚠ " + string.Join("\n  ⚠ ", warnings) : ""));
                return clip;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PuppetDungeon] Échec de la cuisson de {path} : {ex.Message}\n{ex.StackTrace}");
                return null;
            }
            finally
            {
                handler?.Dispose();
                checkHandler?.Dispose();
                if (check != null) Object.DestroyImmediate(check);
                if (proxy != null) Object.DestroyImmediate(proxy);
                if (rest != null) Object.DestroyImmediate(rest);
                Object.DestroyImmediate(inst);
            }
        }

        // --- Report par directions -------------------------------------------------------------------------
        /// <summary>Lacet (°) du corps dans le clip : 0 = regarde vers l'avant du personnage (+Z).</summary>
        private static float FacingYaw(Quaternion body)
        {
            Vector3 fw = body * Vector3.forward;
            return Mathf.Atan2(fw.x, fw.z) * Mathf.Rad2Deg;
        }

        private static Quaternion Frame(Vector3 primary, Vector3 secondary)
        {
            if (primary.sqrMagnitude < 1e-12f) return Quaternion.identity;
            Vector3 s2 = Vector3.ProjectOnPlane(secondary, primary);
            if (s2.sqrMagnitude < 1e-12f) s2 = Vector3.ProjectOnPlane(Vector3.forward, primary);
            if (s2.sqrMagnitude < 1e-12f) s2 = Vector3.ProjectOnPlane(Vector3.right, primary);
            return Quaternion.LookRotation(primary.normalized, s2.normalized);
        }

        private static Vector3 Get(Dictionary<HumanBodyBones, Vector3> p, HumanBodyBones b, Vector3 fallback) => p.TryGetValue(b, out var v) ? v : fallback;

        /// <summary>Côté du bassin (cuisse droite - cuisse gauche).</summary>
        private static Vector3 PelvisSide(Dictionary<HumanBodyBones, Vector3> p)
            => Get(p, HumanBodyBones.RightUpperLeg, Vector3.right) - Get(p, HumanBodyBones.LeftUpperLeg, Vector3.zero);

        /// <summary>Côté des épaules (bras droit - bras gauche), ou du bassin à défaut.</summary>
        private static Vector3 ShoulderSide(Dictionary<HumanBodyBones, Vector3> p)
            => p.ContainsKey(HumanBodyBones.RightUpperArm) && p.ContainsKey(HumanBodyBones.LeftUpperArm)
                ? p[HumanBodyBones.RightUpperArm] - p[HumanBodyBones.LeftUpperArm] : PelvisSide(p);

        /// <summary>Repère du bassin : axe principal vers la colonne, axe secondaire vers la droite.</summary>
        private static Quaternion BodyFrame(Dictionary<HumanBodyBones, Vector3> p)
        {
            Vector3 mid = (Get(p, HumanBodyBones.LeftUpperLeg, Vector3.zero) + Get(p, HumanBodyBones.RightUpperLeg, Vector3.zero)) * 0.5f;
            Vector3 up = Get(p, HumanBodyBones.Spine, mid + Vector3.up) - mid;
            return Frame(up, PelvisSide(p));
        }

        private static readonly Dictionary<HumanBodyBones, HumanBodyBones[]> SegmentEnds = new Dictionary<HumanBodyBones, HumanBodyBones[]>
        {
            { HumanBodyBones.LeftUpperLeg, new[] { HumanBodyBones.LeftLowerLeg } },
            { HumanBodyBones.LeftLowerLeg, new[] { HumanBodyBones.LeftFoot } },
            { HumanBodyBones.RightUpperLeg, new[] { HumanBodyBones.RightLowerLeg } },
            { HumanBodyBones.RightLowerLeg, new[] { HumanBodyBones.RightFoot } },
            { HumanBodyBones.LeftUpperArm, new[] { HumanBodyBones.LeftLowerArm } },
            { HumanBodyBones.LeftLowerArm, new[] { HumanBodyBones.LeftHand } },
            { HumanBodyBones.RightUpperArm, new[] { HumanBodyBones.RightLowerArm } },
            { HumanBodyBones.RightLowerArm, new[] { HumanBodyBones.RightHand } },
            { HumanBodyBones.Spine, new[] { HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.Neck, HumanBodyBones.Head } },
            { HumanBodyBones.Chest, new[] { HumanBodyBones.UpperChest, HumanBodyBones.Neck, HumanBodyBones.Head } },
            { HumanBodyBones.UpperChest, new[] { HumanBodyBones.Neck, HumanBodyBones.Head } },
            { HumanBodyBones.Neck, new[] { HumanBodyBones.Head } },
        };

        /// <summary>Articulation suivante du segment, présente sur les deux squelettes.</summary>
        private static bool SegmentEnd(HumanBodyBones b, Dictionary<HumanBodyBones, Transform> src, Dictionary<HumanBodyBones, Transform> tgt, out HumanBodyBones end)
        {
            end = b;
            if (!SegmentEnds.TryGetValue(b, out var ends)) return false;
            foreach (var e in ends)
                if (src.ContainsKey(e) && tgt.ContainsKey(e)) { end = e; return true; }
            return false;
        }

        /// <summary>
        /// Bras : la rotation propre suit le plan de flexion du coude (comme la charnière de l'avatar Humanoid,
        /// qui plie l'avant-bras vers l'avant en pose T). Bras presque tendu : repli sur l'axe du buste.
        /// </summary>
        private static Vector3 UpperArmSecondary(HumanBodyBones b, Dictionary<HumanBodyBones, Vector3> p)
        {
            Vector3 body = SegmentSecondary(b, p);
            bool left = b == HumanBodyBones.LeftUpperArm;
            var lower = left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm;
            var hand = left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
            if (!p.ContainsKey(b) || !p.ContainsKey(lower) || !p.ContainsKey(hand)) return body;
            Vector3 u = (p[lower] - p[b]).normalized, f = (p[hand] - p[lower]).normalized;
            Vector3 n = Vector3.Cross(u, f);
            if (!left) n = -n;
            float bend = Vector3.Angle(u, f);
            float w = Mathf.Clamp01((bend - 10f) / 20f);
            if (n.sqrMagnitude < 1e-8f || w <= 0f) return body;
            Vector3 bodyPerp = Vector3.ProjectOnPlane(body, u).normalized;
            return Vector3.Slerp(bodyPerp, n.normalized, w);
        }

        private static bool IsArm(HumanBodyBones b) => b == HumanBodyBones.LeftUpperArm || b == HumanBodyBones.LeftLowerArm || b == HumanBodyBones.RightUpperArm || b == HumanBodyBones.RightLowerArm;

        /// <summary>Axe qui fixe la rotation propre du segment : côté du bassin pour jambes et colonne, avant du buste pour les bras.</summary>
        private static Vector3 SegmentSecondary(HumanBodyBones b, Dictionary<HumanBodyBones, Vector3> p)
        {
            switch (b)
            {
                case HumanBodyBones.LeftUpperArm: case HumanBodyBones.LeftLowerArm:
                case HumanBodyBones.RightUpperArm: case HumanBodyBones.RightLowerArm:
                    // Haut du buste, légèrement penché vers l'avant : jamais aligné avec un bras (sauf bras levé
                    // pile à la verticale), donc pas de saut de rotation quand le bras passe devant (visée).
                    Vector3 side = ShoulderSide(p).normalized;
                    Vector3 up = (Get(p, HumanBodyBones.Head, Get(p, HumanBodyBones.Spine, Vector3.zero) + Vector3.up) - Get(p, HumanBodyBones.Spine, Vector3.zero)).normalized;
                    return up + 0.25f * Vector3.Cross(side, up);
                case HumanBodyBones.Spine: case HumanBodyBones.Chest: case HumanBodyBones.UpperChest: case HumanBodyBones.Neck:
                    return ShoulderSide(p);
                default:
                    return PelvisSide(p);
            }
        }

        private static IkSolve MakeIk(Transform up, Transform lo, Transform en, Transform goal, Transform pole, Vector3 poleLocal, Vector3? storedGoal, out string warning)
        {
            warning = null;
            Vector3 a = up.position, b = lo.position, c = en.position, p = pole.TransformPoint(poleLocal);
            float l1 = Vector3.Distance(a, b), l2 = Vector3.Distance(b, c);
            if (l1 < 1e-5f || l2 < 1e-5f) { warning = "IK " + up.name + " : os de longueur nulle"; return null; }
            Vector3 n = Vector3.Cross(b - a, c - b);
            Vector3 axis = (c - a).normalized;
            Vector3 pd = ((p - a) - Vector3.Dot(p - a, axis) * axis).normalized;
            Vector3 planeN = Vector3.Cross(pd, axis).normalized;
            if (n.sqrMagnitude < 1e-10f) n = planeN; else n.Normalize();
            // (Seulement si la pose importée est cohérente, c.-à-d. sans décalages enregistrés.)
            if (!storedGoal.HasValue && Vector3.Dot(n, planeN) < 0.95f)
                warning = $"IK {up.name} : le genou n'est pas dans le plan du pole vector ({Vector3.Angle(n, planeN):0}°) — vérifiez le contrôleur de genou du profil.";
            var s = new IkSolve
            {
                upper = up, lower = lo, end = en, goal = goal, pole = pole, poleLocal = poleLocal, l1 = l1, l2 = l2,
                goalLocal = storedGoal ?? goal.InverseTransformPoint(c),
                upDirL = up.InverseTransformDirection(b - a).normalized,
                loDirL = lo.InverseTransformDirection(c - b).normalized,
                lastPd = pd
            };
            s.upHingeL = up.InverseTransformDirection(n);
            s.loHingeL = lo.InverseTransformDirection(n);
            Vector3.OrthoNormalize(ref s.upDirL, ref s.upHingeL);
            Vector3.OrthoNormalize(ref s.loDirL, ref s.loHingeL);
            return s;
        }

        private static void EnsureGenericImport(ModelImporter mi)
        {
            bool dirty = false;
            if (mi.animationType != ModelImporterAnimationType.Generic) { mi.animationType = ModelImporterAnimationType.Generic; dirty = true; }
            if (mi.avatarSetup != ModelImporterAvatarSetup.NoAvatar) { mi.avatarSetup = ModelImporterAvatarSetup.NoAvatar; dirty = true; }
            if (!mi.importAnimation) { mi.importAnimation = true; dirty = true; }
            if (!mi.resampleCurves) { mi.resampleCurves = true; dirty = true; }
            if (mi.animationCompression != ModelImporterAnimationCompression.Off) { mi.animationCompression = ModelImporterAnimationCompression.Off; dirty = true; }
            if (mi.clipAnimations != null && mi.clipAnimations.Length > 0) { mi.clipAnimations = new ModelImporterClipAnimation[0]; dirty = true; }
            if (dirty) mi.SaveAndReimport();
        }

        public static string LegMuscles(HumanPose p)
        {
            var names = HumanTrait.MuscleName;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < names.Length && i < p.muscles.Length; i++)
                if (names[i].Contains("Leg") || names[i].Contains("Foot")) sb.Append(names[i]).Append('=').Append(p.muscles[i].ToString("F2")).Append("  ");
            sb.Append("bodyRot=").Append(p.bodyRotation.eulerAngles.ToString("F0"));
            return sb.ToString();
        }

        private static bool IsFinger(string muscle)
            => muscle.Contains("Thumb") || muscle.Contains("Index") || muscle.Contains("Middle") || muscle.Contains("Ring") || muscle.Contains("Little");

        private static string Safe(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_');
        }
    }
}
