using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Rendering;
#if PD_URP
using UnityEngine.Rendering.Universal;
#endif

namespace PuppetDungeon.EditorTools
{
    /// <summary>Calques, tags, URP, réglages graphiques et Build Settings.</summary>
    public static class PDProjectSettings
    {
        public const string UrpPackage = "com.unity.render-pipelines.universal";
        private static AddRequest addRequest;

        public static bool UrpInstalled
        {
            get
            {
#if PD_URP
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>Installe URP via le Package Manager (version liée à l'éditeur).</summary>
        public static void InstallUrp()
        {
            if (UrpInstalled || addRequest != null) return;
            Debug.Log("[PuppetDungeon] Installation de l'Universal Render Pipeline…");
            addRequest = Client.Add(UrpPackage);
            EditorApplication.update += WatchInstall;
        }

        private static void WatchInstall()
        {
            if (addRequest == null || !addRequest.IsCompleted)
            {
                if (addRequest != null)
                    EditorUtility.DisplayProgressBar("Puppet Dungeon", "Installation d'URP (Package Manager)…", 0.5f);
                return;
            }
            EditorUtility.ClearProgressBar();
            EditorApplication.update -= WatchInstall;
            if (addRequest.Status == StatusCode.Success)
                Debug.Log("[PuppetDungeon] URP installé : " + addRequest.Result.version + ". Le setup reprendra après la recompilation.");
            else
                Debug.LogError("[PuppetDungeon] Échec d'installation d'URP : " + (addRequest.Error != null ? addRequest.Error.message : "?") +
                               "\nInstallez « Universal RP » depuis Window > Package Manager puis relancez le setup.");
            addRequest = null;
        }

        // ------------------------------------------------------------------ Calques et tags

        public static void EnsureLayersAndTags()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            EnsureLayer(layers, "Enemy", 3);
            EnsureLayer(layers, "Player", -1);
            EnsureLayer(layers, "Debris", -1);
            EnsureLayer(layers, "Projectile", -1);
            tagManager.ApplyModifiedPropertiesWithoutUndo();

            int debris = LayerMask.NameToLayer("Debris");
            int player = LayerMask.NameToLayer("Player");
            int enemy = LayerMask.NameToLayer("Enemy");
            int proj = LayerMask.NameToLayer("Projectile");
            if (debris >= 0)
            {
                if (player >= 0) Physics.IgnoreLayerCollision(debris, player, true);
                if (enemy >= 0) Physics.IgnoreLayerCollision(debris, enemy, true);
                if (proj >= 0) Physics.IgnoreLayerCollision(debris, proj, true);
            }
            if (proj >= 0)
            {
                Physics.IgnoreLayerCollision(proj, proj, true);
            }
        }

        private static void EnsureLayer(SerializedProperty layers, string name, int preferred)
        {
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == name) return;
            if (preferred >= 8 || preferred == 3)
            {
                var p = layers.GetArrayElementAtIndex(preferred);
                if (string.IsNullOrEmpty(p.stringValue)) { p.stringValue = name; return; }
            }
            for (int i = 8; i < layers.arraySize; i++)
            {
                var p = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(p.stringValue)) { p.stringValue = name; return; }
            }
            Debug.LogWarning($"[PuppetDungeon] Plus de calque libre pour « {name} ».");
        }

        // ------------------------------------------------------------------ Graphismes

        public static void ConfigureGraphics()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
#if PD_URP
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PDPaths.UrpAssetPC);
            if (asset == null)
            {
                PDPaths.EnsureFolder("Assets/Settings");
                var data = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(data, "Assets/Settings/PD_Renderer.asset");
                asset = UniversalRenderPipelineAsset.Create(data);
                AssetDatabase.CreateAsset(asset, "Assets/Settings/PD_RPAsset.asset");
            }
            asset.supportsHDR = true;
            asset.shadowDistance = 80f;
            asset.shadowCascadeCount = 2;
            asset.msaaSampleCount = 4;
            EditorUtility.SetDirty(asset);

            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline == null) QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);
#endif
        }

        // ------------------------------------------------------------------ Build Settings

        public static void SetBuildScenes(params string[] scenePaths)
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in scenePaths) list.Add(new EditorBuildSettingsScene(p, true));
            foreach (var s in EditorBuildSettings.scenes)
                if (!scenePaths.Contains(s.path)) list.Add(new EditorBuildSettingsScene(s.path, false));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
