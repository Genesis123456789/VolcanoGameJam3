using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PuppetDungeon.EditorTools
{
    /// <summary>
    /// Outils de rig : correspondance os humanoïdes ↔ transforms, construction des visuels de pièces
    /// à partir d'un FBX (chaque maillage est accroché à l'os le plus proche en pose de liaison),
    /// et construction des prefabs de pantins (joueur, villageois, ennemis).
    /// </summary>
    public static class PDRigTools
    {
        public const float PlayerHeight = 2.05f;

        public static readonly Dictionary<PuppetSlot, HumanBodyBones[]> SlotBones = new Dictionary<PuppetSlot, HumanBodyBones[]>
        {
            { PuppetSlot.Head, new[] { HumanBodyBones.Head, HumanBodyBones.Neck } },
            { PuppetSlot.Torso, new[] { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest } },
            { PuppetSlot.LeftArm, new[] { HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand } },
            { PuppetSlot.RightArm, new[] { HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand } },
            { PuppetSlot.LeftLeg, new[] { HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes } },
            { PuppetSlot.RightLeg, new[] { HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightToes } },
        };

        private static readonly Dictionary<HumanBodyBones, HumanBodyBones> ChildOf = new Dictionary<HumanBodyBones, HumanBodyBones>
        {
            { HumanBodyBones.Hips, HumanBodyBones.Spine },
            { HumanBodyBones.Spine, HumanBodyBones.Chest },
            { HumanBodyBones.Chest, HumanBodyBones.Neck },
            { HumanBodyBones.UpperChest, HumanBodyBones.Neck },
            { HumanBodyBones.Neck, HumanBodyBones.Head },
            { HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm },
            { HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm },
            { HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand },
            { HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm },
            { HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm },
            { HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand },
            { HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg },
            { HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot },
            { HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes },
            { HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg },
            { HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot },
            { HumanBodyBones.RightFoot, HumanBodyBones.RightToes },
        };

        public static GameObject RigModel => PDPaths.Find<GameObject>(PDPaths.Rig);

        // ------------------------------------------------------------------ Os

        /// <summary>
        /// Os humanoïdes d'une instance du rig : convention de noms + correction gauche/droite
        /// (indépendant de l'avatar importé, voir PDAvatarTools).
        /// </summary>
        public static Dictionary<HumanBodyBones, Transform> MapBones(GameObject rigInstance)
            => PDAvatarTools.ResolveBones(rigInstance.transform);

        // ------------------------------------------------------------------ Pose de liaison
        // Le squelette du FBX n'est pas forcément dans la pose où les maillages ont été modélisés :
        // on reconstruit la pose de liaison des os à partir des bindposes des SkinnedMeshRenderer.

        private static Dictionary<Transform, Matrix4x4> bindCache = new Dictionary<Transform, Matrix4x4>();

        public static void PrepareBindPoses(GameObject rig)
        {
            bindCache = new Dictionary<Transform, Matrix4x4>();
            foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = smr.sharedMesh;
                if (mesh == null) continue;
                var bp = mesh.bindposes;
                var bones = smr.bones;
                for (int i = 0; i < bones.Length && i < bp.Length; i++)
                    if (bones[i] != null && !bindCache.ContainsKey(bones[i]))
                        bindCache[bones[i]] = smr.transform.localToWorldMatrix * bp[i].inverse;
            }
        }

        public static Matrix4x4 BindWorld(Transform t)
        {
            if (t == null) return Matrix4x4.identity;
            if (bindCache.TryGetValue(t, out var m)) return m;
            if (t.parent != null && HasBoundAncestor(t.parent))
                m = BindWorld(t.parent) * Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
            else m = t.localToWorldMatrix;
            bindCache[t] = m;
            return m;
        }

        private static bool HasBoundAncestor(Transform t)
        {
            for (; t != null; t = t.parent) if (bindCache.ContainsKey(t)) return true;
            return false;
        }

        private static Vector3 BindPos(Transform t) => BindWorld(t).GetColumn(3);

        /// <summary>Applique une matrice (avec symétrie éventuelle) à la transformation locale.</summary>
        public static void ApplyLocalMatrix(Transform t, Matrix4x4 m)
        {
            Vector3 x = m.GetColumn(0), y = m.GetColumn(1), z = m.GetColumn(2);
            var scale = new Vector3(x.magnitude, y.magnitude, z.magnitude);
            if (Vector3.Dot(Vector3.Cross(x, y), z) < 0f) { scale.x = -scale.x; x = -x; }
            t.localPosition = m.GetColumn(3);
            t.localRotation = Quaternion.LookRotation(z.normalized, y.normalized);
            t.localScale = scale;
        }

        /// <summary>Parente un maillage rigide à un os en conservant sa position de pose de liaison.</summary>
        public static void AttachRigid(Transform mesh, Transform bone)
        {
            var rel = BindWorld(bone).inverse * mesh.localToWorldMatrix;
            mesh.SetParent(bone, false);
            ApplyLocalMatrix(mesh, rel);
        }

        private static float DistanceToBone(Vector3 p, HumanBodyBones bone, Dictionary<HumanBodyBones, Transform> map, float height)
        {
            Vector3 a = BindPos(map[bone]);
            Vector3 b;
            if (ChildOf.TryGetValue(bone, out var child) && map.TryGetValue(child, out var ct) && ct != null) b = BindPos(ct);
            else if (ChildOf.TryGetValue(bone, out child) && child == HumanBodyBones.Chest && map.TryGetValue(HumanBodyBones.Head, out var head)) b = BindPos(head);
            else
            {
                // Extrémités (main, pied, tête) : petit segment dans le prolongement du parent
                Vector3 dir = map[bone].parent != null ? (a - BindPos(map[bone].parent)) : Vector3.up;
                if (bone == HumanBodyBones.Head) dir = Vector3.up;
                if (bone == HumanBodyBones.LeftFoot || bone == HumanBodyBones.RightFoot || bone == HumanBodyBones.LeftToes || bone == HumanBodyBones.RightToes) dir = Vector3.forward;
                b = a + dir.normalized * height * 0.08f;
            }
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector3.Distance(p, a + ab * t);
        }

        public static HumanBodyBones NearestBone(Vector3 p, Dictionary<HumanBodyBones, Transform> map, float height, IEnumerable<HumanBodyBones> allowed = null)
        {
            float best = float.MaxValue;
            HumanBodyBones bestBone = HumanBodyBones.Hips;
            foreach (var kv in map)
            {
                if (allowed != null && !allowed.Contains(kv.Key)) continue;
                if (kv.Key >= HumanBodyBones.LeftThumbProximal && kv.Key <= HumanBodyBones.RightLittleDistal) continue;
                if (kv.Key == HumanBodyBones.LeftEye || kv.Key == HumanBodyBones.RightEye || kv.Key == HumanBodyBones.Jaw) continue;
                float d = DistanceToBone(p, kv.Key, map, height);
                if (d < best) { best = d; bestBone = kv.Key; }
            }
            return bestBone;
        }

        public static PuppetSlot SlotOf(HumanBodyBones b)
        {
            foreach (var kv in SlotBones) if (kv.Value.Contains(b)) return kv.Key;
            return PuppetSlot.Torso;
        }

        private static Vector3 RendererCenter(Renderer r)
        {
            if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null)
                return smr.transform.TransformPoint(smr.sharedMesh.bounds.center);
            var mf = r.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) return r.transform.TransformPoint(mf.sharedMesh.bounds.center);
            return r.bounds.center;
        }

        private static float ModelHeight(GameObject inst)
        {
            var rs = inst.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            if (rs.Length == 0) return 1f;
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return Mathf.Max(0.01f, b.size.y);
        }

        // ------------------------------------------------------------------ Alignement « mode » → rig
        // Les pièces (mode_sworlder, mode_shilder, mode_sniper, bras, jambe…) sont modélisées sur le
        // corps de référence mode_perso_en_vrais (pieds au sol, y = 0), alors que le pantin riggé
        // rig_ff002 est environ 5,2 unités plus haut (bras +5,5, tête +5,0). Sans compensation, chaque
        // pièce s'accrochait ~5 unités sous son os : jambes décollées du genou, bras flottants.
        // On mesure le décalage os par os en comparant les segments du corps de référence (nommés
        // comme les os : RightForeArm, LeftUpLeg, Spine, Head…) à ceux du rig.

        public class ModeAlignment
        {
            public Vector3 global;
            public readonly Dictionary<HumanBodyBones, Vector3> perBone = new Dictionary<HumanBodyBones, Vector3>();
            public Vector3 For(HumanBodyBones b) => perBone.TryGetValue(b, out var v) ? v : global;
        }

        private static ModeAlignment alignment;

        /// <summary>À appeler au début d'un setup pour recalculer l'alignement (modèles modifiés).</summary>
        public static void ResetAlignment() { alignment = null; referenceBounds = null; }

        private static List<Bounds> referenceBounds;

        /// <summary>Boîtes des maillages du corps de référence (mode_perso_en_vrais), dans l'espace d'import.</summary>
        private static List<Bounds> ReferenceBounds()
        {
            if (referenceBounds != null) return referenceBounds;
            referenceBounds = new List<Bounds>();
            var refModel = PDPaths.Find<GameObject>(PDPaths.PartReference);
            if (refModel == null) return referenceBounds;
            var inst = Object.Instantiate(refModel);
            inst.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true)) referenceBounds.Add(r.bounds);
            }
            finally { Object.DestroyImmediate(inst); }
            return referenceBounds;
        }

        private static float Overlap(float a0, float a1, float b0, float b1) => Mathf.Max(0f, Mathf.Min(a1, b1) - Mathf.Max(a0, b0));

        /// <summary>
        /// Décalage vertical qui ramène un modèle de pièce dans l'espace du corps de référence.
        /// Les modèles d'armes/jambes (mode_shilder, mode_sniper, jambe, bras…) ont été exportés ~5 cm plus bas
        /// que le corps : chaque maillage est comparé aux maillages du corps à la même place (vue de dessus)
        /// et de même hauteur ; on garde les meilleures correspondances.
        /// </summary>
        public static float ModelShift(IEnumerable<Renderer> renderers, out int matches)
        {
            var refs = ReferenceBounds();
            var pairs = new List<(float score, float dy)>();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var b = r.bounds;
                foreach (var rb in refs)
                {
                    float ix = Overlap(b.min.x, b.max.x, rb.min.x, rb.max.x), iz = Overlap(b.min.z, b.max.z, rb.min.z, rb.max.z);
                    float inter = ix * iz;
                    float union = b.size.x * b.size.z + rb.size.x * rb.size.z - inter;
                    if (union <= 1e-12f) continue;
                    float iou = inter / union;
                    if (iou < 0.3f) continue;
                    float hy = Mathf.Max(b.size.y, rb.size.y);
                    float sy = hy > 1e-9f ? 1f - Mathf.Abs(b.size.y - rb.size.y) / hy : 1f;
                    if (sy < 0.6f) continue;
                    pairs.Add((iou * sy, rb.center.y - b.center.y));
                }
            }
            matches = 0;
            if (pairs.Count == 0) return 0f;
            float best = pairs.Max(p => p.score);
            var kept = pairs.Where(p => p.score >= best * 0.85f).Select(p => p.dy).OrderBy(v => v).ToList();
            matches = kept.Count;
            return kept[kept.Count / 2];
        }

        private static Vector3 RendererSize(Renderer r)
        {
            Mesh m = r is SkinnedMeshRenderer smr ? smr.sharedMesh : (r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null);
            if (m == null) return r.bounds.size;
            var s = Vector3.Scale(m.bounds.size, r.transform.lossyScale);
            return new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }

        public static ModeAlignment GetAlignment(GameObject rig, Dictionary<HumanBodyBones, Transform> map, float height)
        {
            if (alignment != null) return alignment;
            alignment = new ModeAlignment();
            var refModel = PDPaths.Find<GameObject>(PDPaths.PartReference);
            if (refModel == null)
            {
                Debug.LogWarning("[PuppetDungeon] Corps de référence introuvable (" + PDPaths.PartReference + ") : les pièces sont accrochées sans compensation.");
                return alignment;
            }
            var refInst = Object.Instantiate(refModel);
            refInst.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                refInst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                // Segments du rig, rangés par os (pose de liaison)
                var rigSegs = new List<(HumanBodyBones bone, Vector3 center, Vector3 size)>();
                foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
                {
                    if (IsBlocking(r.transform)) continue;
                    var c = RendererCenter(r);
                    rigSegs.Add((NearestBone(c, map, height), c, RendererSize(r)));
                }
                var offsets = new List<Vector3>();
                var acc = new Dictionary<HumanBodyBones, List<Vector3>>();
                foreach (var r in refInst.GetComponentsInChildren<Renderer>(true))
                {
                    if (!TryBoneFromName(r.transform, out var bone)) continue;
                    Vector3 c = RendererCenter(r), size = RendererSize(r);
                    // Côté géométrique (le personnage regarde +Z : sa gauche est en -X)
                    bone = SideByPosition(bone, c.x);
                    var cands = rigSegs.Where(x => x.bone == bone).ToList();
                    if (cands.Count == 0) continue;
                    var best = cands.OrderBy(x => (x.size - size).magnitude).First();
                    if ((best.size - size).magnitude > Mathf.Max(0.3f * size.magnitude, 1e-4f)) continue;
                    var off = best.center - c;
                    offsets.Add(off);
                    if (!acc.TryGetValue(bone, out var list)) acc[bone] = list = new List<Vector3>();
                    list.Add(off);
                }
                if (offsets.Count == 0)
                {
                    Debug.LogWarning("[PuppetDungeon] Alignement des pièces : aucun segment commun entre le corps de référence et le rig.");
                    return alignment;
                }
                alignment.global = new Vector3(Median(offsets.Select(o => o.x)), Median(offsets.Select(o => o.y)), Median(offsets.Select(o => o.z)));
                foreach (var kv in acc)
                {
                    var avg = new Vector3(kv.Value.Average(v => v.x), kv.Value.Average(v => v.y), kv.Value.Average(v => v.z));
                    // Un écart aberrant (segment remodelé) retombe sur le décalage global
                    if ((avg - alignment.global).magnitude < height * 0.05f) alignment.perBone[kv.Key] = avg;
                }
                Debug.Log($"[PuppetDungeon] Alignement des pièces sur le rig : décalage global {alignment.global.ToString("F3")} ({offsets.Count} segments comparés, {alignment.perBone.Count} os affinés).");
            }
            finally { Object.DestroyImmediate(refInst); }
            return alignment;
        }

        private static float Median(IEnumerable<float> values)
        {
            var v = values.OrderBy(x => x).ToList();
            return v.Count == 0 ? 0f : v[v.Count / 2];
        }

        private static bool TryBoneFromName(Transform t, out HumanBodyBones bone)
        {
            foreach (var n in new[] { t.name, t.parent != null ? t.parent.name : "" })
            {
                string k = PDAvatarTools.Strip(n);
                if (Enum.TryParse(k, out bone) && bone != HumanBodyBones.LastBone) return true;
                switch (k)
                {
                    case "LeftArm": bone = HumanBodyBones.LeftUpperArm; return true;
                    case "RightArm": bone = HumanBodyBones.RightUpperArm; return true;
                    case "LeftForeArm": bone = HumanBodyBones.LeftLowerArm; return true;
                    case "RightForeArm": bone = HumanBodyBones.RightLowerArm; return true;
                    case "LeftUpLeg": bone = HumanBodyBones.LeftUpperLeg; return true;
                    case "RightUpLeg": bone = HumanBodyBones.RightUpperLeg; return true;
                    case "LeftLeg": bone = HumanBodyBones.LeftLowerLeg; return true;
                    case "RightLeg": bone = HumanBodyBones.RightLowerLeg; return true;
                    case "Spine1": bone = HumanBodyBones.Spine; return true;
                }
            }
            bone = HumanBodyBones.Hips;
            return false;
        }

        private static HumanBodyBones SideByPosition(HumanBodyBones b, float x)
        {
            string n = b.ToString();
            bool left = n.StartsWith("Left"), right = n.StartsWith("Right");
            if (left && x > 0f && Enum.TryParse("Right" + n.Substring(4), out HumanBodyBones r)) return r;
            if (right && x < 0f && Enum.TryParse("Left" + n.Substring(5), out HumanBodyBones l)) return l;
            return b;
        }

        // ------------------------------------------------------------------ Visuels de pièces

        /// <summary>
        /// Construit (ou reconstruit) un prefab PartVisual depuis un modèle.
        /// slot == null : toutes les pièces du modèle (décor d'ennemi). mirror : symétrie gauche/droite.
        /// Retourne null si aucun maillage ne correspond à l'emplacement demandé.
        /// </summary>
        public static PartVisual BuildPartVisual(GameObject model, PuppetSlot? slot, bool mirror, string prefabName, Material material, bool mountOnForearm = false)
        {
            var rigModel = RigModel;
            if (rigModel == null || model == null) return null;
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigModel);
            rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            rig.transform.localScale = Vector3.one;
            var part = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(part, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            part.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            part.transform.localScale = mirror ? new Vector3(-1f, 1f, 1f) : Vector3.one;

            var root = new GameObject(prefabName);
            PartVisual visual = null;
            try
            {
                var map = MapBones(rig);
                if (map.Count == 0)
                {
                    Debug.LogError("[PuppetDungeon] L'avatar du rig n'est pas humanoïde : impossible d'accrocher les pièces.");
                    return null;
                }
                PrepareBindPoses(rig);
                float height = ModelHeight(rig);
                var align = GetAlignment(rig, map, height);
                var groups = new Dictionary<HumanBodyBones, Transform>();
                var renderers = part.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToList();
                // Aplatit la hiérarchie : chaque maillage est classé indépendamment de ses parents.
                foreach (var r in renderers) r.transform.SetParent(part.transform, true);
                // Recalage vertical du modèle sur le corps de référence (modèles exportés à une autre hauteur).
                float shift = ModelShift(renderers, out int shiftMatches);
                if (Mathf.Abs(shift) > 1e-4f)
                    foreach (var r in renderers) r.transform.position += Vector3.up * shift;
                Debug.Log($"[PuppetDungeon] {prefabName} : recalage vertical {shift * 100f:0.0} cm ({shiftMatches} correspondance(s)).");
                foreach (var r in renderers)
                {
                    // Le maillage est déplacé de l'espace « mode » vers l'espace du rig avant d'être classé.
                    var bone = NearestBone(RendererCenter(r) + align.global, map, height);
                    var rigSpace = Matrix4x4.Translate(align.For(bone)) * r.transform.localToWorldMatrix;
                    // Armes (arbalète, bouclier, lame) : fixées à l'avant-bras et non à la main. Dans les animations
                    // Maya le poignet pivote fortement (jusqu'à 90° en visée) : l'arme suit ainsi l'axe du bras.
                    if (mountOnForearm)
                    {
                        if (bone == HumanBodyBones.RightHand && map.ContainsKey(HumanBodyBones.RightLowerArm)) bone = HumanBodyBones.RightLowerArm;
                        else if (bone == HumanBodyBones.LeftHand && map.ContainsKey(HumanBodyBones.LeftLowerArm)) bone = HumanBodyBones.LeftLowerArm;
                    }
                    if (slot.HasValue && !SlotBones[slot.Value].Contains(bone)) continue;
                    if (!groups.TryGetValue(bone, out var g))
                    {
                        g = new GameObject("Attache_" + bone).transform;
                        g.SetParent(root.transform, false);
                        ApplyLocalMatrix(g, BindWorld(map[bone])); // disposition en pose de liaison (aperçus)
                        groups[bone] = g;
                    }
                    // Position relative à l'os en pose de liaison : à l'exécution le groupe est remis
                    // en identité sous l'os animé, la pièce suit donc parfaitement le membre.
                    var rel = BindWorld(map[bone]).inverse * rigSpace;
                    r.transform.SetParent(g, false);
                    ApplyLocalMatrix(r.transform, rel);
                    if (material != null) r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    foreach (Transform child in r.transform.Cast<Transform>().ToList())
                        if (child.GetComponent<Renderer>() == null && child.GetComponentInChildren<Renderer>() == null) Object.DestroyImmediate(child.gameObject);
                }
                if (groups.Count == 0) return null;

                visual = root.AddComponent<PartVisual>();
                foreach (var kv in groups)
                {
                    visual.groups.Add(new PartVisual.AttachGroup { bone = kv.Key, root = kv.Value });
                    visual.coveredBones.Add(kv.Key);
                }
                PDPaths.EnsureFolder(PDPaths.PartVisuals);
                string path = PDPaths.PartVisuals + "/" + prefabName + ".prefab";
                var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                return saved != null ? saved.GetComponent<PartVisual>() : null;
            }
            finally
            {
                Object.DestroyImmediate(rig);
                Object.DestroyImmediate(part);
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Emplacements couverts par un modèle (pour l'assistant de création de pièce).</summary>
        public static HashSet<PuppetSlot> DetectSlots(GameObject model)
        {
            var result = new HashSet<PuppetSlot>();
            var rigModel = RigModel;
            if (rigModel == null || model == null) return result;
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigModel);
            var part = Object.Instantiate(model);
            try
            {
                rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                part.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var map = MapBones(rig);
                if (map.Count == 0) return result;
                PrepareBindPoses(rig);
                float h = ModelHeight(rig);
                var align = GetAlignment(rig, map, h);
                var rs = part.GetComponentsInChildren<Renderer>();
                float shift = ModelShift(rs, out _);
                foreach (var r in rs)
                    result.Add(SlotOf(NearestBone(RendererCenter(r) + align.global + Vector3.up * shift, map, h)));
            }
            finally
            {
                Object.DestroyImmediate(rig);
                Object.DestroyImmediate(part);
            }
            return result;
        }

        // ------------------------------------------------------------------ Pantins

        /// <summary>Crée la hiérarchie Visuel/Rig d'un pantin, mise à l'échelle et posée au sol.</summary>
        public static PuppetRig BuildPuppetVisual(Transform parent, PuppetAnimationSet animations, Material wood, float height, int layer)
        {
            var visual = new GameObject("Visuel");
            visual.transform.SetParent(parent, false);
            visual.AddComponent<SquashStretch>();

            var rigModel = RigModel;
            if (rigModel == null) return null;
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigModel, visual.transform);
            rig.name = "Rig";
            rig.transform.localPosition = Vector3.zero;
            rig.transform.localRotation = Quaternion.identity;
            rig.transform.localScale = Vector3.one;

            // Échelle : le FBX est en centimètres, on vise une hauteur de jeu lisible.
            var rs = rig.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                float k = height / Mathf.Max(0.01f, b.size.y);
                rig.transform.localScale = Vector3.one * k;
                b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                rig.transform.position += Vector3.up * (parent.position.y - b.min.y);
            }

            var animator = rig.GetComponent<Animator>();
            if (animator == null) animator = rig.AddComponent<Animator>();
            animator.avatar = PDAvatarTools.PuppetAvatar;
            animator.runtimeAnimatorController = animations != null ? animations.controller : null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // On dépaquette le modèle : les maillages rigides (tête, articulations des mains)
            // doivent devenir enfants de leurs os pour suivre l'animation.
            PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var puppet = rig.AddComponent<PuppetRig>();
            puppet.animator = animator;
            puppet.animationSet = animations;
            var map = MapBones(rig);
            PrepareBindPoses(rig);
            float h = ModelHeight(rig);
            var rigid = MakeSegmentsRigid(rig);
            var boneOf = map.GroupBy(kv => kv.Value).ToDictionary(g => g.Key, g => g.First().Key);
            foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
            {
                if (IsBlocking(r.transform)) { r.enabled = false; continue; }
                if (!r.enabled) continue;
                HumanBodyBones bone = HumanBodyBones.Hips;
                if (rigid.Contains(r))
                {
                    // Déjà accroché à son os dominant : on retrouve l'os humanoïde (ou le plus proche ancêtre)
                    for (var t = r.transform.parent; t != null; t = t.parent)
                        if (boneOf.TryGetValue(t, out bone)) break;
                }
                else if (!(r is SkinnedMeshRenderer) && map.Count > 0)
                {
                    bone = NearestBone(RendererCenter(r), map, h);
                    AttachRigid(r.transform, map[bone]);
                }
                else if (map.Count > 0) bone = NearestBone(RendererCenter(r), map, h);
                if (wood != null) r.sharedMaterials = Enumerable.Repeat(wood, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                puppet.baseSegments.Add(new PuppetRig.BaseSegment { renderer = r, bone = bone });
            }
            SetLayer(visual.transform, layer);
            return puppet;
        }

        /// <summary>
        /// Le modèle riggé est lissé (« smooth bind ») : chaque pièce de bois est répartie sur 2 à 3 os
        /// (ex. avant-bras : ForeArm 63 %, Arm 35 %, Hand 23 % ; main : 50/50), elle se plie et s'étire comme
        /// du caoutchouc dès qu'une articulation tourne. Un pantin est fait de segments rigides : chaque
        /// SkinnedMeshRenderer est remplacé par un maillage rigide accroché à son os dominant (en pose de
        /// liaison). Ni le modèle ni les animations ne sont modifiés ; les billes d'articulation cachent les jointures.
        /// </summary>
        public static HashSet<Renderer> MakeSegmentsRigid(GameObject rig)
        {
            var created = new HashSet<Renderer>();
            foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList())
            {
                var mesh = smr.sharedMesh;
                if (mesh == null || IsBlocking(smr.transform)) continue;
                var bone = DominantBone(smr);
                if (bone == null) continue;
                var go = new GameObject(smr.gameObject.name);
                go.layer = smr.gameObject.layer;
                go.transform.SetParent(bone, false);
                ApplyLocalMatrix(go.transform, BindWorld(bone).inverse * smr.transform.localToWorldMatrix);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = smr.sharedMaterials;
                mr.enabled = smr.enabled;
                mr.shadowCastingMode = smr.shadowCastingMode;
                created.Add(mr);
                if (smr.transform.childCount == 0) Object.DestroyImmediate(smr.gameObject);
                else Object.DestroyImmediate(smr);
            }
            return created;
        }

        /// <summary>Os qui porte le plus de poids ; à égalité (±15 %), l'os le plus profond (main plutôt qu'avant-bras).</summary>
        private static Transform DominantBone(SkinnedMeshRenderer smr)
        {
            var bones = smr.bones;
            var weights = smr.sharedMesh.boneWeights;
            if (bones == null || bones.Length == 0) return smr.rootBone;
            var sum = new float[bones.Length];
            foreach (var w in weights)
            {
                if (w.boneIndex0 < sum.Length) sum[w.boneIndex0] += w.weight0;
                if (w.boneIndex1 < sum.Length) sum[w.boneIndex1] += w.weight1;
                if (w.boneIndex2 < sum.Length) sum[w.boneIndex2] += w.weight2;
                if (w.boneIndex3 < sum.Length) sum[w.boneIndex3] += w.weight3;
            }
            int best = 0;
            for (int i = 1; i < sum.Length; i++) if (sum[i] > sum[best]) best = i;
            for (int i = 0; i < sum.Length; i++)
                if (i != best && bones[i] != null && bones[best] != null && sum[i] >= sum[best] * 0.85f && bones[i].IsChildOf(bones[best]))
                    best = i;
            return bones[best] != null ? bones[best] : smr.rootBone;
        }

        private static bool IsBlocking(Transform t)
        {
            for (; t != null; t = t.parent)
                if (t.name.ToLowerInvariant().Contains("bloking") || t.name.ToLowerInvariant().Contains("blocking")) return true;
            return false;
        }

        public static void SetLayer(Transform t, int layer)
        {
            if (layer < 0) return;
            t.gameObject.layer = layer;
            foreach (Transform c in t) SetLayer(c, layer);
        }

        public static float ClipLength(AnimationClip c) => c != null ? c.length : 0.8f;
    }
}
