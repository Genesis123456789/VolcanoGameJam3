using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
#if PD_URP
using UnityEngine.Rendering;
#endif

namespace PuppetDungeon.EditorTools
{
    /// <summary>Crée les trois scènes du jeu : menu, donjon, village.</summary>
    public static class PDScenes
    {
        public static string MenuPath => PDPaths.Scenes + "/" + GameManager.SceneMenu + ".unity";
        public static string DungeonPath => PDPaths.Scenes + "/" + GameManager.SceneDungeon + ".unity";
        public static string VillagePath => PDPaths.Scenes + "/" + GameManager.SceneVillage + ".unity";

        public static void BuildAll(GameObject uiRoot, GameObject mainMenu, DungeonTheme villageTheme, DungeonTheme menuTheme)
        {
            PDPaths.EnsureFolder(PDPaths.Scenes);
#if PD_URP
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PDPostProcess.ProfilePath);
#endif
            // ----- Menu
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = MakeCamera(false);
            var sun = MakeSun();
#if PD_URP
            PDPostProcess.SetupVolume(new GameObject("Post-process"), profile);
#endif
            var diorama = new GameObject("Diorama").AddComponent<MenuDiorama>();
            diorama.sun = sun;
            diorama.theme = menuTheme;
            if (mainMenu != null) PrefabUtility.InstantiatePrefab(mainMenu);
            MakeEventSystem();
            RenderSettingsDefaults();
            EditorSceneManager.SaveScene(scene, MenuPath);

            // ----- Donjon
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            cam = MakeCamera(true);
            sun = MakeSun();
#if PD_URP
            PDPostProcess.SetupVolume(new GameObject("Post-process"), profile);
#endif
            var dm = new GameObject("Donjon").AddComponent<DungeonManager>();
            dm.sun = sun;
            if (uiRoot != null) PrefabUtility.InstantiatePrefab(uiRoot);
            MakeEventSystem();
            RenderSettingsDefaults();
            EditorSceneManager.SaveScene(scene, DungeonPath);

            // ----- Village
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            cam = MakeCamera(true);
            sun = MakeSun();
#if PD_URP
            PDPostProcess.SetupVolume(new GameObject("Post-process"), profile);
#endif
            var vd = new GameObject("Village").AddComponent<VillageDirector>();
            vd.sun = sun;
            vd.villageTheme = villageTheme;
            if (uiRoot != null) PrefabUtility.InstantiatePrefab(uiRoot);
            MakeEventSystem();
            RenderSettingsDefaults();
            EditorSceneManager.SaveScene(scene, VillagePath);

            PDProjectSettings.SetBuildScenes(MenuPath, DungeonPath, VillagePath);
            EditorSceneManager.OpenScene(MenuPath);
        }

        private static Camera MakeCamera(bool roomCamera)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.fieldOfView = roomCamera ? 30f : 40f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            go.AddComponent<AudioListener>();
            if (roomCamera) go.AddComponent<RoomCamera>();
            go.transform.SetPositionAndRotation(new Vector3(8f, 18f, -8f), Quaternion.Euler(58f, 0f, 0f));
            PDPostProcess.SetupCamera(cam);
            return cam;
        }

        private static Light MakeSun()
        {
            var go = new GameObject("Soleil");
            go.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.88f, 0.72f);
            l.intensity = 1.2f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.85f;
            return l;
        }

        private static void MakeEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static void RenderSettingsDefaults()
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.27f, 0.3f);
        }
    }
}
