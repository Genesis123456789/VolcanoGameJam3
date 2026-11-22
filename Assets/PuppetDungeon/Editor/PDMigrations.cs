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
            var crossbowVisual = AssetDatabase.LoadAssetAtPath<PartVisual>(PDPaths.PartVisuals + "/PV_Arbalete_Guetteur.prefab");
            if (crossbowVisual != null && crossbowVisual.groups.Exists(g => g != null && (g.bone == HumanBodyBones.RightHand || g.bone == HumanBodyBones.LeftHand)))
            {
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
                    Debug.Log("[PuppetDungeon] Armes (arbalète, bouclier, lame) fixées à l'avant-bras.");
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
