using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PuppetDungeon.EditorTools
{
    /// <summary>
    /// Menu Tools > Puppet Dungeon. « Setup complet » installe URP si besoin (le setup reprend tout seul
    /// après la recompilation), règle le projet et génère tous les assets, prefabs, UI et scènes.
    /// </summary>
    [InitializeOnLoad]
    public static class PDSetup
    {
        private const string ContinueKey = "PD_ContinueSetupAfterURP";
        private const string Menu = "Tools/Puppet Dungeon/";

        static PDSetup()
        {
            if (!SessionState.GetBool(ContinueKey, false)) return;
            EditorApplication.delayCall += () =>
            {
                if (!PDProjectSettings.UrpInstalled) return;
                SessionState.SetBool(ContinueKey, false);
                Debug.Log("[PuppetDungeon] URP détecté, reprise du setup…");
                RunAll();
            };
        }

        [MenuItem(Menu + "1. Setup complet (tout construire)", priority = 0)]
        public static void SetupEverything()
        {
            // En mode Play, Unity interdit de sauvegarder/reconstruire les scènes : on sort du jeu puis on relance.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.Log("[PuppetDungeon] Sortie du mode Play, le setup démarre juste après…");
                EditorApplication.playModeStateChanged += RunAfterPlayMode;
                EditorApplication.isPlaying = false;
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PDProjectSettings.EnsureLayersAndTags();
            if (!PDProjectSettings.UrpInstalled)
            {
                SessionState.SetBool(ContinueKey, true);
                PDProjectSettings.InstallUrp();
                EditorUtility.DisplayDialog("Puppet Dungeon",
                    "L'Universal Render Pipeline est en cours d'installation.\n\nLe setup reprendra automatiquement après la recompilation (comptez une minute).", "OK");
                return;
            }
            RunAll();
        }

        private static void RunAfterPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= RunAfterPlayMode;
            EditorApplication.delayCall += SetupEverything;
        }

        [MenuItem(Menu + "2. Jouer (ouvrir le menu principal)", priority = 1)]
        public static void Play()
        {
            if (!System.IO.File.Exists(PDScenes.MenuPath))
            {
                EditorUtility.DisplayDialog("Puppet Dungeon", "Lancez d'abord « Setup complet ».", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(PDScenes.MenuPath);
            EditorApplication.isPlaying = true;
        }

        [MenuItem(Menu + "Rafraîchir la base de pièces", priority = 20)]
        public static void RefreshParts()
        {
            var db = PDContent.RefreshDatabase();
            AssetDatabase.SaveAssets();
            Debug.Log($"[PuppetDungeon] Base de pièces : {db.parts.Count} pièce(s).");
            Selection.activeObject = db;
        }

        [MenuItem(Menu + "Reconstruire seulement l'interface", priority = 21)]
        public static void RebuildUIOnly()
        {
            PDAssets.CreateTextures();
            PDImport.ConfigureUISprites();
            PDAssets.CreateOpaquePanels();
            UIBuilder.LoadTheme();
            var slot = PDUI.BuildSlot("PD_ItemSlot", "", 102f);
            var toast = PDUI.BuildToast();
            var card = PDUI.BuildOfferingCard();
            var ui = PDUI.BuildUIRoot(slot, toast, card);
            PDUI.BuildMainMenu();
            var cfg = GameConfig.Load();
            if (cfg != null) { cfg.uiRootPrefab = ui; EditorUtility.SetDirty(cfg); }
            AssetDatabase.SaveAssets();
            Debug.Log("[PuppetDungeon] Interface reconstruite. Relancez le setup complet pour mettre à jour les scènes.");
        }

        private static void Step(string label, float progress)
        {
            EditorUtility.DisplayProgressBar("Puppet Dungeon — setup", label, progress);
            Debug.Log("[PuppetDungeon] " + label);
        }

        public static void RunAll()
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                PDPaths.EnsureAllFolders();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            try
            {
                Step("Calques, tags et URP", 0.02f);
                PDProjectSettings.EnsureLayersAndTags();
                PDProjectSettings.ConfigureGraphics();

                Step("Import : sprites 9-slice (panneaux opaques), rig, audio", 0.06f);
                PDAssets.CreateTextures();
                PDImport.ConfigureUISprites();
                PDAssets.CreateOpaquePanels();
                PDImport.ConfigureRig();
                PDImport.ConfigureAudio();

                Step("Matériaux", 0.12f);
                var mats = PDAssets.CreateMaterials();
                PDAssets.UpgradeLegacyMaterials();
                var arc = PDAssets.CreateArcMesh();

                Step("Avatar humanoïde du pantin", 0.16f);
                PDAvatarTools.BuildPuppetAvatar();

                Step("Cuisson des animations (contrôleurs Maya → clips Humanoid)", 0.2f);
                var animLib = PDAnimBaker.EnsureLibrary();
                PDAnimBaker.BakeAll(animLib);

                Step("Animator du pantin et jeux d'animations par arme", 0.28f);
                var baseCtrl = PDAnimSets.CreateBaseController();
                var animSets = PDAnimSets.BuildSets(animLib, baseCtrl, out var defaultAnims);

                Step("Effets visuels, projectiles, butin", 0.32f);
                UIBuilder.LoadTheme();
                var vfx = PDVfx.BuildVfx(mats, arc);
                var bolt = PDVfx.BuildBolt(mats);
                var orb = PDVfx.BuildOrb(mats);
                var loot = PDVfx.BuildLoot(mats);

                Step("Pantins (joueur, villageois)", 0.42f);
                PDRigTools.ResetAlignment();
                var player = PDContent.BuildPlayer(defaultAnims, mats);
                var villager = PDContent.BuildVillager(defaultAnims, mats);

                Step("Pièces d'exemple à partir des FBX", 0.52f);
                PDContent.BuildExampleParts(mats);
                var db = PDContent.RefreshDatabase();

                Step("Ennemis et boss", 0.62f);
                var (enemies, bosses) = PDContent.BuildEnemies(animSets, defaultAnims, mats);

                Step("Thèmes et audio", 0.68f);
                var themes = PDContent.BuildThemes(mats, out var villageTheme);
                var audio = PDContent.BuildAudio();

                Step("Interface (panneaux bois 9-slice)", 0.74f);
                UIBuilder.LoadTheme();
                var slot = PDUI.BuildSlot("PD_ItemSlot", "", 102f);
                var toast = PDUI.BuildToast();
                var card = PDUI.BuildOfferingCard();
                var uiRoot = PDUI.BuildUIRoot(slot, toast, card);
                var mainMenu = PDUI.BuildMainMenu();

                Step("Configuration du jeu", 0.82f);
                var credits = PDContent.BuildCredits();
                PDContent.BuildConfig(db, enemies, bosses, player, villager, loot, bolt, orb, uiRoot, vfx, audio, themes, defaultAnims, animSets, credits);

#if PD_URP
                Step("Post-process (Volume URP)", 0.86f);
                PDPostProcess.CreateProfile();
#endif
                AssetDatabase.SaveAssets();

                Step("Scènes", 0.92f);
                PDScenes.BuildAll(uiRoot, mainMenu, villageTheme, themes.Count > 0 ? themes[0] : null);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                try { PDAnimPreview.RenderAll(); } catch (Exception pe) { Debug.LogWarning("[PD-PREVIEW] " + pe.Message); }
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Puppet Dungeon",
                    "Setup terminé !\n\n• Scènes : Assets/PuppetDungeon/Generated/Scenes\n• Pièces : Assets/PuppetDungeon/Content/Parts\n• Animations : Assets/PuppetDungeon/Content/Animation\n• Crédits : Assets/PuppetDungeon/Content/PD_Credits\n• Réglages : Assets/PuppetDungeon/Resources/PD_GameConfig\n\nTools > Puppet Dungeon > 2. Jouer pour lancer le jeu.", "Super");
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Puppet Dungeon", "Le setup a rencontré une erreur :\n" + e.Message + "\n\nDétails dans la Console.", "OK");
            }
        }
    }
}
