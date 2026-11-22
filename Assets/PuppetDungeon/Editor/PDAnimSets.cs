using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PuppetDungeon.EditorTools
{
    /// <summary>
    /// Animator du pantin + jeux d'animations par arme.
    ///
    /// PD_Puppet.controller (unique, partagé par joueur, ennemis et villageois) :
    ///  • Calque de base (corps entier) : Locomotion (blend tree Idle → Marche → Course sur « Speed »)
    ///    et AttaqueSurPlace (trigger « AttackStill », corps entier quand on attaque à l'arrêt).
    ///  • Calque « HautDuCorps » (masque haut du corps, override) : Attaque (trigger « Attack », jouée
    ///    PAR-DESSUS la marche/course), Garde (bool « Guard »), Célébration (trigger « Celebrate »).
    ///    L'état « Vide » a Write Defaults désactivé : il laisse passer la locomotion.
    /// Les états utilisent des clips « emplacements » (Generated/Animation/Slots) ; chaque jeu
    /// d'animations (PuppetAnimationSet) génère un AnimatorOverrideController qui les remplace.
    /// </summary>
    public static class PDAnimSets
    {
        public const string SlotIdle = "Idle", SlotWalk = "Walk", SlotRun = "Run", SlotAttack = "Attack",
            SlotAttackStill = "AttackStill", SlotGuard = "Guard", SlotCelebrate = "Celebrate", SlotHit = "Hit";
        private static readonly string[] Slots = { SlotIdle, SlotWalk, SlotRun, SlotAttack, SlotAttackStill, SlotGuard, SlotCelebrate, SlotHit };

        public static AnimationClip SlotClip(string slot)
        {
            PDPaths.EnsureFolder(PDPaths.AnimationSlots);
            string path = PDPaths.AnimationSlots + "/PD_Slot_" + slot + ".anim";
            var c = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (c != null) return c;
            c = new AnimationClip { name = "PD_Slot_" + slot };
            if (slot == SlotIdle || slot == SlotWalk || slot == SlotRun || slot == SlotGuard)
            {
                var st = AnimationUtility.GetAnimationClipSettings(c);
                st.loopTime = slot != SlotGuard;
                AnimationUtility.SetAnimationClipSettings(c, st);
            }
            AssetDatabase.CreateAsset(c, path);
            return c;
        }

        public static AnimatorController CreateBaseController()
        {
            string path = PDPaths.Animation + "/PD_Puppet.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter(new AnimatorControllerParameter { name = "AttackSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("AttackStill", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Guard", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Celebrate", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

            // ---- Calque de base : corps entier
            var sm = ctrl.layers[0].stateMachine;
            var tree = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(tree, ctrl);
            tree.AddChild(SlotClip(SlotIdle), 0f);
            tree.AddChild(SlotClip(SlotWalk), 0.35f);
            tree.AddChild(SlotClip(SlotRun), 1f);
            var loco = sm.AddState("Locomotion", new Vector3(300f, 80f, 0f));
            loco.motion = tree;
            loco.writeDefaultValues = false;
            sm.defaultState = loco;

            var still = sm.AddState("AttaqueSurPlace", new Vector3(300f, 200f, 0f));
            still.motion = SlotClip(SlotAttackStill);
            still.speedParameterActive = true;
            still.speedParameter = "AttackSpeed";
            still.writeDefaultValues = false;
            var toStill = sm.AddAnyStateTransition(still);
            toStill.hasExitTime = false;
            toStill.duration = 0.06f;
            toStill.canTransitionToSelf = true;
            toStill.AddCondition(AnimatorConditionMode.If, 0f, "AttackStill");
            var back = still.AddTransition(loco);
            back.hasExitTime = true;
            back.exitTime = 0.88f;
            back.duration = 0.15f;
            var interrupt = still.AddTransition(loco);
            interrupt.hasExitTime = false;
            interrupt.duration = 0.2f;
            interrupt.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");

            // ---- Calque haut du corps : attaque mélangée à la marche, garde, célébration
            var mask = PDAssets.CreateUpperBodyMask();
            ctrl.AddLayer("HautDuCorps");
            var layers = ctrl.layers;
            var layer = layers[1];
            layer.defaultWeight = 1f;
            layer.avatarMask = mask;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            var asm = layer.stateMachine;
            var empty = asm.AddState("Vide", new Vector3(300f, 80f, 0f));
            empty.writeDefaultValues = false;
            asm.defaultState = empty;

            var atk = asm.AddState("Attaque", new Vector3(560f, 0f, 0f));
            atk.motion = SlotClip(SlotAttack);
            atk.speedParameterActive = true;
            atk.speedParameter = "AttackSpeed";
            atk.writeDefaultValues = false;
            var toAtk = asm.AddAnyStateTransition(atk);
            toAtk.hasExitTime = false;
            toAtk.duration = 0.05f;
            toAtk.canTransitionToSelf = true;
            toAtk.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            var atkBack = atk.AddTransition(empty);
            atkBack.hasExitTime = true;
            atkBack.exitTime = 0.9f;
            atkBack.duration = 0.12f;

            var guard = asm.AddState("Garde", new Vector3(560f, 120f, 0f));
            guard.motion = SlotClip(SlotGuard);
            guard.writeDefaultValues = false;
            var toGuard = empty.AddTransition(guard);
            toGuard.hasExitTime = false;
            toGuard.duration = 0.2f;
            toGuard.AddCondition(AnimatorConditionMode.If, 0f, "Guard");
            var guardBack = guard.AddTransition(empty);
            guardBack.hasExitTime = false;
            guardBack.duration = 0.2f;
            guardBack.AddCondition(AnimatorConditionMode.IfNot, 0f, "Guard");

            var cele = asm.AddState("Celebration", new Vector3(560f, 240f, 0f));
            cele.motion = SlotClip(SlotCelebrate);
            cele.writeDefaultValues = false;
            var toCele = asm.AddAnyStateTransition(cele);
            toCele.hasExitTime = false;
            toCele.duration = 0.1f;
            toCele.AddCondition(AnimatorConditionMode.If, 0f, "Celebrate");
            var celeBack = cele.AddTransition(empty);
            celeBack.hasExitTime = true;
            celeBack.exitTime = 0.92f;
            celeBack.duration = 0.2f;

            var hit = asm.AddState("Touche", new Vector3(560f, 360f, 0f));
            hit.motion = SlotClip(SlotHit);
            hit.speed = 1.5f;
            hit.writeDefaultValues = false;
            var toHit = asm.AddAnyStateTransition(hit);
            toHit.hasExitTime = false;
            toHit.duration = 0.05f;
            toHit.canTransitionToSelf = true;
            toHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            var hitBack = hit.AddTransition(empty);
            hitBack.hasExitTime = true;
            hitBack.exitTime = 0.85f;
            hitBack.duration = 0.2f;

            ctrl.layers = layers;
            // Les clips cuits n'ont pas de cibles IK de pieds : l'IK des pieds doit rester désactivé.
            foreach (var l in ctrl.layers)
                foreach (var cs in l.stateMachine.states) cs.state.iKOnFeet = false;
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        private class SetSpec
        {
            public string id, label, idle, walk, run, attack, attackStill, guard, celebrate, hit = "hit_Shilder";
            public AttackStyle? style;
        }

        // Noms = noms des FBX dans Assets/Projet/Animation (clips cuits « PD_<nom> »).
        private static readonly SetSpec[] Specs =
        {
            new SetSpec { id = "Defaut", label = "Poings (défaut)", idle = "idle", walk = "walk_cicle", run = "run_cicle", attack = "sworld_att", attackStill = "sworld_att", guard = "garde_pose", celebrate = "67", style = AttackStyle.Fist },
            new SetSpec { id = "Lame", label = "Lame", attack = "sworld_att", attackStill = "sworld_att", guard = "garde_pose", style = AttackStyle.Blade },
            new SetSpec { id = "Arbalete", label = "Arbalète", attack = "shoot", attackStill = "shoot", guard = "aim_pose", style = AttackStyle.Crossbow },
            new SetSpec { id = "Bouclier", label = "Bouclier", attack = "sworld_att", attackStill = "sworld_att", guard = "garde_pose", style = AttackStyle.Shield },
        };

        /// <summary>
        /// Crée les jeux d'animations manquants (Content/Animation), complète les emplacements vides avec
        /// les clips cuits correspondants, puis génère un AnimatorOverrideController par jeu.
        /// Vos modifications dans les jeux existants sont conservées.
        /// </summary>
        public static Dictionary<AttackStyle, PuppetAnimationSet> BuildSets(AnimationLibrary lib, AnimatorController baseCtrl, out PuppetAnimationSet defaultSet)
        {
            var result = new Dictionary<AttackStyle, PuppetAnimationSet>();
            defaultSet = null;
            foreach (var spec in Specs)
            {
                string path = PDPaths.ContentAnimation + "/PD_Anim_" + spec.id + ".asset";
                var set = PDPaths.LoadOrCreate<PuppetAnimationSet>(path);
                if (string.IsNullOrEmpty(set.label) || set.label == "Défaut") set.label = spec.label;
                if (set.dataVersion < 2)
                {
                    // v2 : « hit_Shilder » est une réaction au coup reçu (et non une attaque).
                    set.attack = null;
                    set.attackStill = null;
                    set.hit = null;
                    set.dataVersion = 2;
                }
                if (set.dataVersion < 3)
                {
                    // v3 : à l'arrêt, l'arbalète tire avec « shoot » (« sot_genoux » lève le genou, ce n'est pas un tir).
                    if (set.attackStill != null && set.attackStill.name.Contains("sot_genoux")) set.attackStill = null;
                    set.dataVersion = 3;
                }
                Fill(ref set.idle, lib, spec.idle);
                Fill(ref set.walk, lib, spec.walk);
                Fill(ref set.run, lib, spec.run);
                Fill(ref set.attack, lib, spec.attack);
                Fill(ref set.attackStill, lib, spec.attackStill);
                Fill(ref set.guard, lib, spec.guard);
                Fill(ref set.celebrate, lib, spec.celebrate);
                Fill(ref set.hit, lib, spec.hit);
                EditorUtility.SetDirty(set);
                if (spec.id == "Defaut") defaultSet = set;
                if (spec.style.HasValue) result[spec.style.Value] = set;
            }
            // Jeux ajoutés à la main par l'équipe (n'importe quel PuppetAnimationSet du projet)
            var all = AssetDatabase.FindAssets("t:PuppetAnimationSet")
                .Select(g => AssetDatabase.LoadAssetAtPath<PuppetAnimationSet>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null).ToList();
            foreach (var set in all) BuildOverride(set, defaultSet, baseCtrl);
            return result;
        }

        private static void Fill(ref AnimationClip field, AnimationLibrary lib, string clipName)
        {
            if (field != null || string.IsNullOrEmpty(clipName) || lib == null) return;
            field = lib.Get(clipName);
        }

        public static AnimatorOverrideController BuildOverride(PuppetAnimationSet set, PuppetAnimationSet fallback, AnimatorController baseCtrl)
        {
            string path = PDPaths.Animation + "/PD_AOC_" + set.name + ".overrideController";
            var aoc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (aoc == null)
            {
                aoc = new AnimatorOverrideController(baseCtrl) { name = "PD_AOC_" + set.name };
                AssetDatabase.CreateAsset(aoc, path);
            }
            aoc.runtimeAnimatorController = baseCtrl;
            AnimationClip Pick(System.Func<PuppetAnimationSet, AnimationClip> f)
                => f(set) != null ? f(set) : (fallback != null ? f(fallback) : null);
            var map = new Dictionary<string, AnimationClip>
            {
                { SlotIdle, Pick(s => s.idle) },
                { SlotWalk, Pick(s => s.walk) },
                { SlotRun, Pick(s => s.run) },
                { SlotAttack, Pick(s => s.attack) ?? Pick(s => s.attackStill) },
                { SlotAttackStill, Pick(s => s.attackStill) ?? Pick(s => s.attack) },
                { SlotGuard, Pick(s => s.guard) },
                { SlotCelebrate, Pick(s => s.celebrate) },
                { SlotHit, Pick(s => s.hit) },
            };
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            aoc.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                var original = overrides[i].Key;
                string slot = Slots.FirstOrDefault(s => original != null && original.name == "PD_Slot_" + s);
                AnimationClip clip = null;
                if (slot != null) map.TryGetValue(slot, out clip);
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, clip);
            }
            aoc.ApplyOverrides(overrides);
            EditorUtility.SetDirty(aoc);
            set.controller = aoc;
            EditorUtility.SetDirty(set);
            return aoc;
        }
    }
}
