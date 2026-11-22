using System.IO;
using UnityEditor;
using UnityEngine;

namespace PuppetDungeon.EditorTools
{
    /// <summary>
    /// Mises à jour automatiques des données déjà générées (sans relancer le setup complet) :
    /// nom du jeu, titre des crédits, équilibrage des premiers donjons, jeu d'animations de l'arbalète.
    /// Chaque étape ne s'applique qu'une fois et ne touche que ce qu'elle doit changer.
    /// </summary>
    [InitializeOnLoad]
    public static class PDMigrations
    {
        static PDMigrations()
        {
            EditorApplication.delayCall += Run;
        }

        [MenuItem("Tools/Puppet Dungeon/Maintenance/Appliquer les mises à jour de données", priority = 90)]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Réessaie au retour en mode édition.
                EditorApplication.playModeStateChanged -= OnPlayMode;
                EditorApplication.playModeStateChanged += OnPlayMode;
                return;
            }
            bool changed = false;

            // Nom du jeu (fenêtre, build).
            if (PlayerSettings.productName != GameInfo.Title)
            {
                PlayerSettings.productName = GameInfo.Title;
                Debug.Log("[PuppetDungeon] Nom du produit : " + GameInfo.Title);
                changed = true;
            }

            // Titre des crédits.
            var credits = AssetDatabase.LoadAssetAtPath<CreditsData>(PDPaths.Credits);
            if (credits != null && GameInfo.Resolve(credits.gameTitle) != credits.gameTitle)
            {
                credits.gameTitle = GameInfo.Title;
                EditorUtility.SetDirty(credits);
                changed = true;
            }

            // Crédits : les sections encore vides « (votre nom ici) » deviennent une section Équipe (nom + rôle).
            if (credits != null && credits.sections != null && !credits.sections.Exists(s => s != null && s.people != null && s.people.Count > 0))
            {
                bool Placeholder(CreditsData.Section s) => s != null && (s.people == null || s.people.Count == 0) && s.lines != null && s.lines.Count > 0
                                                           && s.lines.TrueForAll(l => l != null && l.Contains("votre nom ici"));
                int first = credits.sections.FindIndex(Placeholder);
                if (first >= 0)
                {
                    var team = PDContent.DefaultTeam();
                    team.people.Clear();
                    foreach (var s in credits.sections.FindAll(Placeholder))
                        team.people.Add(new CreditsData.Person { name = "(Prénom Nom)", role = s.title });
                    credits.sections.RemoveAll(Placeholder);
                    credits.sections.Insert(Mathf.Min(first, credits.sections.Count), team);
                    EditorUtility.SetDirty(credits);
                    Debug.Log("[PuppetDungeon] Crédits : section « Équipe » avec un rôle par personne (Content/PD_Credits.asset).");
                    changed = true;
                }
            }

            // Équilibrage v2 : premiers donjons plus doux.
            var cfg = Resources.Load<GameConfig>(GameConfig.ResourcePath);
            if (cfg != null && cfg.balanceVersion < 2)
            {
                if (cfg.playerBase != null && cfg.playerBase.maxHealth < 75f) cfg.playerBase.maxHealth = 75f;
                cfg.balanceVersion = 2;
                EditorUtility.SetDirty(cfg);
                Debug.Log("[PuppetDungeon] Équilibrage v2 appliqué (PV de base 75, premiers donjons adoucis, boss plus rares au donjon 1).");
                changed = true;
            }
            // Équilibrage v3 : boss un peu plus fréquents, début un peu moins facile.
            if (cfg != null && cfg.balanceVersion < 3)
            {
                cfg.bossChanceByRooms = GameConfig.DefaultBossCurve();
                cfg.earlyEnemyHealth = 0.78f;
                cfg.earlyEnemyDamage = 0.72f;
                cfg.earlyEnemySlowness = 1.2f;
                cfg.firstDungeonBossFreeRooms = 4;
                cfg.firstDungeonBossMultiplier = 0.55f;
                cfg.firstDungeonExitGuardianChance = 0.25f;
                cfg.balanceVersion = 3;
                EditorUtility.SetDirty(cfg);
                Debug.Log("[PuppetDungeon] Équilibrage v3 appliqué (boss un peu plus fréquents, début un peu plus exigeant).");
                changed = true;
            }
            // Équilibrage v4 : gacha nettement plus cher (un tirage se mérite), mais chaque tirage rapporte mieux.
            if (cfg != null && cfg.balanceVersion < 4)
            {
                cfg.gachaCost = 100;
                cfg.gachaCostGrowthPerCycle = 1.3f;
                cfg.gachaMultiDiscount = 0.15f;
                cfg.gachaRarityWeights = new[] { 30f, 34f, 22f, 11f, 3f };
                cfg.gachaPity = 10;
                cfg.balanceVersion = 4;
                EditorUtility.SetDirty(cfg);
                Debug.Log($"[PuppetDungeon] Équilibrage v4 appliqué (gacha : {cfg.GachaCost(5)} {cfg.currencyName} le tirage, {cfg.GachaMultiCost(5)} les {cfg.gachaMultiCount}).");
                changed = true;
            }

            // Jeu d'animations de l'arbalète : tir « shoot » aussi à l'arrêt.
            var crossbow = AssetDatabase.LoadAssetAtPath<PuppetAnimationSet>(PDPaths.ContentAnimation + "/PD_Anim_Arbalete.asset");
            if (crossbow != null && crossbow.dataVersion < 3)
            {
                var lib = AssetDatabase.LoadAssetAtPath<AnimationLibrary>(PDPaths.ContentAnimation + "/PD_AnimationLibrary.asset");
                if (lib != null)
                {
                    var baseCtrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(PDPaths.Animation + "/PD_Puppet.controller");
                    if (baseCtrl == null) baseCtrl = PDAnimSets.CreateBaseController();
                    PDAnimSets.BuildSets(lib, baseCtrl, out _);
                    Debug.Log("[PuppetDungeon] Arbalète : l'attaque à l'arrêt utilise maintenant « shoot ».");
                    changed = true;
                }
            }

            // Armes fixées à l'avant-bras (et non plus à la main) : on reconstruit leurs visuels sur place
            // (mêmes fichiers, donc les pièces et ennemis qui les utilisent restent branchés).
            // + v1 : modèles d'armes et de jambes recalés en hauteur sur le corps (ils flottaient ~5 cm trop bas).
            var crossbowVisual = AssetDatabase.LoadAssetAtPath<PartVisual>(PDPaths.PartVisuals + "/PV_Arbalete_Guetteur.prefab");
            bool handMounted = crossbowVisual != null && crossbowVisual.groups.Exists(g => g != null && (g.bone == HumanBodyBones.RightHand || g.bone == HumanBodyBones.LeftHand));
            if (handMounted || (cfg != null && cfg.contentVersion < 1))
            {
                PDRigTools.ResetAlignment();
                var m = new PDAssets.MaterialSet
                {
                    partWood = AssetDatabase.LoadAssetAtPath<Material>(PDPaths.Materials + "/PD_PartWood.mat"),
                    woodDark = AssetDatabase.LoadAssetAtPath<Material>(PDPaths.Materials + "/PD_WoodCorrupted.mat"),
                };
                if (m.partWood != null && m.woodDark != null)
                {
                    PDContent.BuildExampleParts(m);
                    PDContent.BuildDecoration(PDPaths.PartSword, "Epee", m.woodDark);
                    PDContent.BuildDecoration(PDPaths.PartSniper, "Arbalete", m.woodDark);
                    PDContent.BuildDecoration(PDPaths.PartShield, "Bouclier", m.woodDark);
                    Debug.Log("[PuppetDungeon] Visuels des pièces reconstruits (armes à l'avant-bras, modèles recalés en hauteur).");
                    if (cfg != null) { cfg.contentVersion = 1; EditorUtility.SetDirty(cfg); }
                    changed = true;
                }
            }

            if (changed) AssetDatabase.SaveAssets();
        }

        private static void OnPlayMode(PlayModeStateChange s)
        {
            if (s != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.delayCall += Run;
        }
    }
}
