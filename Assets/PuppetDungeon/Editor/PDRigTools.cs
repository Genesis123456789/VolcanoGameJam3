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

        /// <summary>Os humanoïdes d'une instance du rig (via la description humaine de l'avatar).</summary>
        public static Dictionary<HumanBodyBones, Transform> MapBones(GameObject rigInstance)
        {
            var map = new Dictionary<HumanBodyBones, Transform>();
            var animator = rigInstance.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return map;
            var all = rigInstance.GetComponentsInChildren<Transform>(true);
            foreach (var hb in animator.avatar.humanDescription.human)
            {
                string boneName = hb.humanName.Replace(" ", "");
                if (!Enum.TryParse(boneName, out HumanBodyBones bone)) continue;
                var t = all.FirstOrDefault(x => x.name == hb.boneName);
                if (t != null) map[bone] = t;
            }
            if (map.Count == 0)
            {
                for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
                {
                    var t = animator.GetBoneTransform((HumanBodyBones)i);
                    if (t != null) map[(HumanBodyBones)i] = t;
                }
            }
            return map;
        }

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

        // ------------------------------------------------------------------ Visuels de pièces

        /// <summary>
        /// Construit (ou reconstruit) un prefab PartVisual depuis un modèle.
        /// slot == null : toutes les pièces du modèle (décor d'ennemi). mirror : symétrie gauche/droite.
        /// Retourne null si aucun maillage ne correspond à l'emplacement demandé.
        /// </summary>
        public static PartVisual BuildPartVisual(GameObject model, PuppetSlot? slot, bool mirror, string prefabName, Material material)
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
                var groups = new Dictionary<HumanBodyBones, Transform>();
                var renderers = part.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToList();
                // Aplatit la hiérarchie : chaque maillage est classé indépendamment de ses parents.
                foreach (var r in renderers) r.transform.SetParent(part.transform, true);
                foreach (var r in renderers)
                {
                    var bone = NearestBone(RendererCenter(r), map, height);
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
                    var rel = BindWorld(map[bone]).inverse * r.transform.localToWorldMatrix;
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
                foreach (var r in part.GetComponentsInChildren<Renderer>())
                    result.Add(SlotOf(NearestBone(RendererCenter(r), map, h)));
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
        public static PuppetRig BuildPuppetVisual(Transform parent, AnimatorController controller, Material wood, float height, int layer)
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
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // On dépaquette le modèle : les maillages rigides (tête, articulations des mains)
            // doivent devenir enfants de leurs os pour suivre l'animation.
            PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var puppet = rig.AddComponent<PuppetRig>();
            puppet.animator = animator;
            var map = MapBones(rig);
            PrepareBindPoses(rig);
            float h = ModelHeight(rig);
            foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
            {
                if (IsBlocking(r.transform)) { r.enabled = false; continue; }
                if (!r.enabled) continue;
                if (!(r is SkinnedMeshRenderer) && map.Count > 0)
                {
                    var rigidBone = NearestBone(RendererCenter(r), map, h);
                    AttachRigid(r.transform, map[rigidBone]);
                }
                if (wood != null) r.sharedMaterials = Enumerable.Repeat(wood, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                var bone = map.Count > 0 ? NearestBone(RendererCenter(r), map, h) : HumanBodyBones.Hips;
                puppet.baseSegments.Add(new PuppetRig.BaseSegment { renderer = r, bone = bone });
            }
            SetLayer(visual.transform, layer);
            return puppet;
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
