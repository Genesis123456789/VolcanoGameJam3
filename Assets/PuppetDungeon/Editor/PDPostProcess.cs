using UnityEditor;
using UnityEngine;
#if PD_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace PuppetDungeon.EditorTools
{
    /// <summary>Profil de post-process URP « diorama » : tonemapping, bloom, vignette, couleurs, grain, tilt-shift.</summary>
    public static class PDPostProcess
    {
        public const string ProfilePath = PDPaths.Data + "/PD_PostProcess.asset";

#if PD_URP
        public static VolumeProfile CreateProfile()
        {
            PDPaths.EnsureFolder(PDPaths.Data);
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath) != null) AssetDatabase.DeleteAsset(ProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var tone = Add<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.ACES);

            var bloom = Add<Bloom>(profile);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.75f);
            bloom.scatter.Override(0.68f);
            bloom.tint.Override(new Color(1f, 0.9f, 0.78f));
            bloom.highQualityFiltering.Override(true);

            var color = Add<ColorAdjustments>(profile);
            color.postExposure.Override(0.35f);
            color.contrast.Override(14f);
            color.saturation.Override(14f);
            color.colorFilter.Override(new Color(1f, 0.97f, 0.92f));

            var wb = Add<WhiteBalance>(profile);
            wb.temperature.Override(9f);
            wb.tint.Override(2f);

            var lgg = Add<LiftGammaGain>(profile);
            lgg.lift.Override(new Vector4(0.98f, 0.96f, 1.02f, -0.02f));
            lgg.gain.Override(new Vector4(1.02f, 1f, 0.97f, 0.02f));

            var vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.34f);
            vignette.smoothness.Override(0.45f);
            vignette.rounded.Override(false);
            vignette.color.Override(Color.black);

            var chroma = Add<ChromaticAberration>(profile);
            chroma.intensity.Override(0f);

            var grain = Add<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.15f);
            grain.response.Override(0.8f);

            var dof = Add<DepthOfField>(profile);
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(22f);
            dof.focalLength.Override(85f);
            dof.aperture.Override(4.5f);
            dof.bladeCount.Override(6);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var c = profile.Add<T>(true);
            c.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }

        public static void SetupVolume(GameObject go, VolumeProfile profile)
        {
            var v = go.AddComponent<Volume>();
            v.isGlobal = true;
            v.priority = 1f;
            v.sharedProfile = profile;
            var fx = go.AddComponent<PostFX>();
            fx.volume = v;
        }

        public static void SetupCamera(Camera cam)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
        }
#else
        public static void SetupCamera(Camera cam) { }
#endif
    }
}
