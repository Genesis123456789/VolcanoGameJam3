using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PuppetDungeon.EditorTools
{
    /// <summary>
    /// Avatars humanoïdes construits avec AvatarBuilder à partir de la pose de repos d'un rig.
    /// - Les os sont trouvés par convention de noms (Hips, Spine, LeftArm, LeftForeArm, LeftUpLeg…),
    ///   espaces de noms Maya ignorés (« rig_0010:Hips » → « Hips »).
    /// - Le côté gauche/droit est vérifié géométriquement : rig_ff002 nomme « LeftUpLeg » la jambe
    ///   située à droite du personnage ; sans correction, l'animation humanoïde serait inversée.
    /// </summary>
    public static class PDAvatarTools
    {
        private static readonly Dictionary<string, HumanBodyBones> Convention = new Dictionary<string, HumanBodyBones>
        {
            { "hips", HumanBodyBones.Hips }, { "pelvis", HumanBodyBones.Hips }, { "root_hips", HumanBodyBones.Hips },
            { "spine", HumanBodyBones.Spine }, { "spine1", HumanBodyBones.Chest }, { "chest", HumanBodyBones.Chest },
            { "spine2", HumanBodyBones.UpperChest }, { "upperchest", HumanBodyBones.UpperChest },
            { "neck", HumanBodyBones.Neck }, { "head", HumanBodyBones.Head },
            { "leftshoulder", HumanBodyBones.LeftShoulder }, { "rightshoulder", HumanBodyBones.RightShoulder },
            { "leftarm", HumanBodyBones.LeftUpperArm }, { "leftupperarm", HumanBodyBones.LeftUpperArm },
            { "rightarm", HumanBodyBones.RightUpperArm }, { "rightupperarm", HumanBodyBones.RightUpperArm },
            { "leftforearm", HumanBodyBones.LeftLowerArm }, { "leftlowerarm", HumanBodyBones.LeftLowerArm },
            { "rightforearm", HumanBodyBones.RightLowerArm }, { "rightlowerarm", HumanBodyBones.RightLowerArm },
            { "lefthand", HumanBodyBones.LeftHand }, { "righthand", HumanBodyBones.RightHand },
            { "leftupleg", HumanBodyBones.LeftUpperLeg }, { "leftupperleg", HumanBodyBones.LeftUpperLeg }, { "leftthigh", HumanBodyBones.LeftUpperLeg },
            { "rightupleg", HumanBodyBones.RightUpperLeg }, { "rightupperleg", HumanBodyBones.RightUpperLeg }, { "rightthigh", HumanBodyBones.RightUpperLeg },
            { "leftleg", HumanBodyBones.LeftLowerLeg }, { "leftlowerleg", HumanBodyBones.LeftLowerLeg }, { "leftshin", HumanBodyBones.LeftLowerLeg },
            { "rightleg", HumanBodyBones.RightLowerLeg }, { "rightlowerleg", HumanBodyBones.RightLowerLeg }, { "rightshin", HumanBodyBones.RightLowerLeg },
            { "leftfoot", HumanBodyBones.LeftFoot }, { "rightfoot", HumanBodyBones.RightFoot },
            { "lefttoebase", HumanBodyBones.LeftToes }, { "lefttoes", HumanBodyBones.LeftToes },
            { "righttoebase", HumanBodyBones.RightToes }, { "righttoes", HumanBodyBones.RightToes },
        };

        private static readonly HumanBodyBones[] LeftLeg = { HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes };
        private static readonly HumanBodyBones[] RightLeg = { HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightToes };
        private static readonly HumanBodyBones[] LeftArm = { HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand };
        private static readonly HumanBodyBones[] RightArm = { HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand };

        public static string Strip(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int i = name.LastIndexOf(':');
            return i >= 0 ? name.Substring(i + 1) : name;
        }

        private static string Key(string name) => Strip(name).Replace("_", "").Replace(" ", "").ToLowerInvariant();

        /// <summary>Os humanoïdes d'une hiérarchie (articulations seulement : les maillages sont ignorés).</summary>
        public static Dictionary<HumanBodyBones, Transform> ResolveBones(Transform root, AnimationRigProfile profile = null, bool log = false)
        {
            var map = new Dictionary<HumanBodyBones, Transform>();
            var all = root.GetComponentsInChildren<Transform>(true).Where(t => t.GetComponent<Renderer>() == null).ToList();

            // Hanches : la première articulation « hips » qui a une colonne sous elle
            Transform hips = null;
            var forcedHips = profile != null ? profile.boneNames.FirstOrDefault(b => b != null && b.bone == HumanBodyBones.Hips) : null;
            if (forcedHips != null && !string.IsNullOrEmpty(forcedHips.name))
                hips = all.FirstOrDefault(t => Strip(t.name) == forcedHips.name || t.name == forcedHips.name);
            if (hips == null)
                hips = all.FirstOrDefault(t => Convention.TryGetValue(Key(t.name), out var b) && b == HumanBodyBones.Hips
                                               && t.GetComponentsInChildren<Transform>(true).Any(c => Key(c.name) == "spine"));
            if (hips == null) return map;
            map[HumanBodyBones.Hips] = hips;

            foreach (var t in hips.GetComponentsInChildren<Transform>(true))
            {
                if (t == hips || t.GetComponent<Renderer>() != null) continue;
                if (!Convention.TryGetValue(Key(t.name), out var bone) || map.ContainsKey(bone)) continue;
                map[bone] = t;
            }
            if (profile != null)
            {
                foreach (var bn in profile.boneNames)
                {
                    if (bn == null || string.IsNullOrEmpty(bn.name)) continue;
                    var t = hips.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => Strip(x.name) == bn.name || x.name == bn.name);
                    if (t != null) map[bn.bone] = t;
                }
            }
            FixSides(root, map, LeftLeg, RightLeg, "jambes", log);
            FixSides(root, map, LeftArm, RightArm, "bras", log);
            return map;
        }

        /// <summary>Gauche = côté -X du modèle (personnage tourné vers +Z, convention Unity/FBX).</summary>
        private static void FixSides(Transform root, Dictionary<HumanBodyBones, Transform> map, HumanBodyBones[] left, HumanBodyBones[] right, string label, bool log)
        {
            // Premier os présent des deux côtés (épaule ou bras / cuisse)
            for (int i = 0; i < left.Length; i++)
            {
                if (!map.TryGetValue(left[i], out var l) || !map.TryGetValue(right[i], out var r)) continue;
                float lx = root.InverseTransformPoint(l.position).x;
                float rx = root.InverseTransformPoint(r.position).x;
                if (lx > rx + 1e-4f)
                {
                    for (int k = 0; k < left.Length; k++)
                    {
                        map.TryGetValue(left[k], out var a);
                        map.TryGetValue(right[k], out var b);
                        map.Remove(left[k]);
                        map.Remove(right[k]);
                        if (b != null) map[left[k]] = b;
                        if (a != null) map[right[k]] = a;
                    }
                    if (log) Debug.Log($"[PuppetDungeon] Rig « {root.name} » : {label} gauche/droite inversés dans les noms, corrigé pour l'avatar humanoïde.");
                }
                return;
            }
        }

        public static bool HasRequiredBones(Dictionary<HumanBodyBones, Transform> map, out string missing)
        {
            var miss = new List<string>();
            for (int i = 0; i < HumanTrait.BoneCount; i++)
                if (HumanTrait.RequiredBone(i) && !map.ContainsKey((HumanBodyBones)i)) miss.Add(((HumanBodyBones)i).ToString());
            missing = string.Join(", ", miss);
            return miss.Count == 0;
        }

        /// <summary>
        /// Construit un avatar humanoïde sur « root » dans sa pose actuelle (= pose de référence).
        /// Le squelette contient la racine, les ancêtres des hanches et toutes les articulations.
        /// </summary>
        public static Avatar Build(GameObject root, Dictionary<HumanBodyBones, Transform> map, string avatarName)
        {
            if (!HasRequiredBones(map, out var missing))
            {
                Debug.LogError($"[PuppetDungeon] Avatar « {avatarName} » impossible : os manquants ({missing}).");
                return null;
            }
            var hips = map[HumanBodyBones.Hips];
            var skeleton = new List<SkeletonBone>();
            var include = new HashSet<Transform>();
            for (var t = hips; t != null; t = t.parent)
            {
                include.Add(t);
                if (t == root.transform) break;
            }
            foreach (var t in hips.GetComponentsInChildren<Transform>(true))
                if (t.GetComponent<Renderer>() == null) include.Add(t);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t != root.transform && !include.Contains(t)) continue;
                skeleton.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
            }
            var human = map.Select(kv => new HumanBone
            {
                boneName = kv.Value.name,
                humanName = HumanTrait.BoneName[(int)kv.Key],
                limit = new HumanLimit { useDefaultValues = true }
            }).ToArray();
            var desc = new HumanDescription
            {
                human = human,
                skeleton = skeleton.ToArray(),
                upperArmTwist = 0.5f,
                lowerArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                lowerLegTwist = 0.5f,
                armStretch = 0.05f,
                legStretch = 0.05f,
                feetSpacing = 0f,
                hasTranslationDoF = false
            };
            var avatar = AvatarBuilder.BuildHumanAvatar(root, desc);
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogError($"[PuppetDungeon] AvatarBuilder n'a pas pu créer « {avatarName} » (noms d'articulations en double ?).");
                return null;
            }
            avatar.name = avatarName;
            return avatar;
        }

        /// <summary>Enregistre un avatar dans Generated/Animation/Avatars (remplace l'ancien).</summary>
        public static Avatar Save(Avatar avatar, string fileName)
        {
            if (avatar == null) return null;
            PDPaths.EnsureFolder(PDPaths.Avatars);
            string path = PDPaths.Avatars + "/" + fileName + ".asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
            avatar.name = fileName;
            AssetDatabase.CreateAsset(avatar, path);
            return avatar;
        }

        /// <summary>Avatar du pantin (rig_ff002, pose de repos du FBX).</summary>
        public static Avatar BuildPuppetAvatar()
        {
            var model = PDRigTools.RigModel;
            if (model == null) { Debug.LogError("[PuppetDungeon] Rig du pantin introuvable : " + PDPaths.Rig); return null; }
            var inst = (GameObject)Object.Instantiate(model);
            inst.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                inst.transform.localScale = Vector3.one;
                var map = ResolveBones(inst.transform, null, true);
                var avatar = Build(inst, map, "PD_Avatar_Pantin");
                return Save(avatar, "PD_Avatar_Pantin");
            }
            finally { Object.DestroyImmediate(inst); }
        }

        public static Avatar PuppetAvatar => AssetDatabase.LoadAssetAtPath<Avatar>(PDPaths.Avatars + "/PD_Avatar_Pantin.asset");
    }
}
