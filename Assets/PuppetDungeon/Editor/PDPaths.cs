using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PuppetDungeon.EditorTools
{
    /// <summary>Chemins du projet + recherche tolérante des assets fournis par l'équipe.</summary>
    public static class PDPaths
    {
        public const string Root = "Assets/PuppetDungeon";
        public const string Generated = Root + "/Generated";
        public const string Content = Root + "/Content";
        public const string Resources = Root + "/Resources";

        public const string Prefabs = Generated + "/Prefabs";
        public const string PartVisuals = Generated + "/PartVisuals";
        public const string Materials = Generated + "/Materials";
        public const string Textures = Generated + "/Textures";
        public const string Meshes = Generated + "/Meshes";
        public const string Animation = Generated + "/Animation";
        public const string BakedAnimation = Animation + "/Baked";
        public const string AnimationSlots = Animation + "/Slots";
        public const string Avatars = Animation + "/Avatars";
        public const string UI = Generated + "/UI";
        public const string Vfx = Generated + "/VFX";
        public const string Data = Generated + "/Data";
        public const string Scenes = Generated + "/Scenes";

        public const string Parts = Content + "/Parts";
        public const string Enemies = Content + "/Enemies";
        public const string Themes = Content + "/Themes";
        public const string ContentAnimation = Content + "/Animation";
        public const string Credits = Content + "/PD_Credits.asset";
        public const string InputActions = Root + "/Input/PD_Controls.inputactions";

        // Assets fournis (cherchés aussi par nom si déplacés)
        public const string Rig = "Assets/Projet/Graphismes/rig_ff002.fbx";
        /// <summary>Dossier scanné par l'outil de cuisson : déposez-y vos FBX d'animation.</summary>
        public const string AnimationsFolder = "Assets/Projet/Animation";
        /// <summary>Fichier où le rig d'animation est en pose de repos (sert à l'avatar du rig).</summary>
        public const string AnimRestPose = "Assets/Projet/Animation/ALL_anime.fbx";
        public const string PartArm = "Assets/Projet/Graphismes/bras.fbx";
        public const string PartLeg = "Assets/Projet/Graphismes/jambe.fbx";
        public const string PartBody = "Assets/Projet/mode/mode_perso_en_vrais.fbx";
        /// <summary>Corps de référence sur lequel les pièces sont modélisées (segments nommés comme les os).</summary>
        public const string PartReference = PartBody;
        public const string PartSniper = "Assets/Projet/mode/additionell/mode_sniper.fbx";
        public const string PartShield = "Assets/Projet/mode/additionell/mode_shilder.fbx";
        public const string PartSword = "Assets/Projet/mode/additionell/mode_sworlder.fbx";
        public const string UIPanel = "Assets/Projet/Graphismes/UI/Wood_PANEL.png";
        public const string UIPanelClean = "Assets/Projet/Graphismes/UI/Wood_PANELCLEAN.png";
        public const string UIButton = "Assets/Projet/Graphismes/UI/Wood_Button.png";
        public const string Music = "Assets/Projet/song/musique.wav";
        public const string SfxHit = "Assets/Projet/song/hit-wood.mp3";
        public const string SfxClick = "Assets/Projet/song/wood-tap-click.wav";
        public const string UrpAssetPC = "Assets/Settings/PC_RPAsset.asset";

        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        public static void EnsureAllFolders()
        {
            foreach (var f in new[] { Prefabs, PartVisuals, Materials, Textures, Meshes, Animation, BakedAnimation, AnimationSlots, Avatars, UI, Vfx, Data, Scenes, Parts, Enemies, Themes, ContentAnimation, Resources })
                EnsureFolder(f);
        }

        /// <summary>Charge un asset par chemin ; sinon le cherche par nom de fichier dans tout le projet.</summary>
        public static T Find<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            string file = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path).ToLowerInvariant();
            foreach (var guid in AssetDatabase.FindAssets(file))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(p) != file) continue;
                if (!string.IsNullOrEmpty(ext) && Path.GetExtension(p).ToLowerInvariant() != ext) continue;
                a = AssetDatabase.LoadAssetAtPath<T>(p);
                if (a != null) return a;
            }
            return null;
        }

        public static string FindPath(string path)
        {
            if (File.Exists(path)) return path;
            var o = Find<Object>(path);
            return o != null ? AssetDatabase.GetAssetPath(o) : null;
        }

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            EnsureFolder(Path.GetDirectoryName(path));
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        public static AnimationClip FirstClip(string modelPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }
    }
}
