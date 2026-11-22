using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PuppetDungeon.EditorTools
{
    /// <summary>Réglages d'import : sprites 9-slice, rig humanoïde, clips d'animation, audio.</summary>
    public static class PDImport
    {
        // ------------------------------------------------------------------ Sprites UI (9-slicing)

        public static void ConfigureUISprites()
        {
            // Bordures (gauche, bas, droite, haut) en pixels : les coins ne sont jamais étirés.
            SetupSprite(PDPaths.UIPanel, new Vector4(132, 132, 132, 132));
            SetupSprite(PDPaths.UIPanelClean, new Vector4(132, 132, 132, 132));
            SetupSprite(PDPaths.UIButton, new Vector4(290, 270, 290, 270));
        }

        public static void SetupSprite(string assetPath, Vector4 border)
        {
            string path = PDPaths.FindPath(assetPath);
            if (path == null) { Debug.LogWarning("[PuppetDungeon] Sprite introuvable : " + assetPath); return; }
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // indispensable pour le 9-slicing
            settings.spritePixelsPerUnit = 100f;
            settings.spriteBorder = border;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(settings);
            ti.spriteBorder = border;
            ti.SaveAndReimport();
        }

        // ------------------------------------------------------------------ Modèles

        /// <summary>
        /// Rig du pantin : importé en « Generic » sans avatar. L'avatar humanoïde est construit par
        /// l'outil (PD_Avatar_Pantin) car l'auto-configuration d'Unity se fie aux noms, or
        /// rig_ff002 nomme « LeftUpLeg » la jambe droite du personnage (corrigé géométriquement).
        /// </summary>
        public static void ConfigureRig()
        {
            string path = PDPaths.FindPath(PDPaths.Rig);
            if (path == null) { Debug.LogError("[PuppetDungeon] Rig introuvable : " + PDPaths.Rig); return; }
            var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            bool dirty = false;
            if (mi.animationType != ModelImporterAnimationType.Generic) { mi.animationType = ModelImporterAnimationType.Generic; dirty = true; }
            if (mi.avatarSetup != ModelImporterAvatarSetup.NoAvatar) { mi.avatarSetup = ModelImporterAvatarSetup.NoAvatar; dirty = true; }
            if (mi.importAnimation) { mi.importAnimation = false; dirty = true; }
            if (!mi.importVisibility) { mi.importVisibility = true; dirty = true; }
            if (dirty) mi.SaveAndReimport();
        }

        public static void ConfigurePartModel(string assetPath)
        {
            string path = PDPaths.FindPath(assetPath);
            if (path == null) return;
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) return;
            if (mi.animationType != ModelImporterAnimationType.None || mi.importAnimation)
            {
                mi.animationType = ModelImporterAnimationType.None;
                mi.importAnimation = false;
                mi.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------------ Audio

        public static void ConfigureAudio()
        {
            SetupAudio(PDPaths.Music, true);
            SetupAudio(PDPaths.SfxHit, false);
            SetupAudio(PDPaths.SfxClick, false);
        }

        public static void SetupAudio(string assetPath, bool music)
        {
            string path = PDPaths.FindPath(assetPath);
            if (path == null) return;
            var ai = AssetImporter.GetAtPath(path) as AudioImporter;
            if (ai == null) return;
            var s = ai.defaultSampleSettings;
            s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = music ? 0.6f : 0.8f;
            ai.defaultSampleSettings = s;
            ai.forceToMono = !music;
            ai.loadInBackground = music;
            ai.SaveAndReimport();
        }

    }
}
