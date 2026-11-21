using UnityEngine;
#if PD_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace PuppetDungeon
{
    /// <summary>
    /// Post-process dynamique (URP Volume) : tilt-shift (profondeur de champ Bokeh calée sur le joueur),
    /// impulsions de vignette/aberration chromatique quand on est touché, alerte, PV bas.
    /// Le profil de Volume est créé par l'outil de setup (Bloom, Tonemapping, Color Adjustments…).
    /// </summary>
    public class PostFX : MonoBehaviour
    {
        public enum PulseKind { Hurt, Alert, Heal }

        public static PostFX Instance { get; private set; }

#if PD_URP
        public Volume volume;
        private Vignette vignette;
        private ChromaticAberration chroma;
        private DepthOfField dof;
        private ColorAdjustments color;
        private float baseVignette = 0.32f;
        private float baseSaturation = 12f;
#endif
        [Header("Tilt-shift")]
        public float focalLength = 85f;
        public float aperture = 4.5f;

        private float hurt;
        private float alert;
        private float heal;

        private void Awake()
        {
            Instance = this;
#if PD_URP
            if (volume == null) volume = GetComponent<Volume>();
            if (volume != null && volume.profile != null)
            {
                var p = volume.profile; // copie d'exécution
                p.TryGet(out vignette);
                p.TryGet(out chroma);
                p.TryGet(out dof);
                p.TryGet(out color);
                if (vignette != null) baseVignette = vignette.intensity.value;
                if (color != null) baseSaturation = color.saturation.value;
            }
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Pulse(PulseKind kind)
        {
            if (Instance == null) return;
            switch (kind)
            {
                case PulseKind.Hurt: Instance.hurt = 1f; break;
                case PulseKind.Alert: Instance.alert = 1f; break;
                case PulseKind.Heal: Instance.heal = 1f; break;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            hurt = Mathf.Max(0f, hurt - dt * 2.2f);
            alert = Mathf.Max(0f, alert - dt * 1.2f);
            heal = Mathf.Max(0f, heal - dt * 1.5f);
#if PD_URP
            var player = PlayerController.Instance;
            float low = 0f;
            if (player != null && player.Health != null && !player.Health.IsDead && player.Health.Normalized < 0.3f)
                low = (0.3f - player.Health.Normalized) / 0.3f * (0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f));

            if (vignette != null)
            {
                vignette.intensity.Override(Mathf.Clamp01(baseVignette + hurt * 0.25f + alert * 0.15f + low * 0.2f));
                Color c = Color.Lerp(Color.black, new Color(0.55f, 0.02f, 0.02f), Mathf.Clamp01(hurt + low));
                c = Color.Lerp(c, new Color(0.1f, 0.35f, 0.1f), heal * 0.6f);
                vignette.color.Override(c);
            }
            if (chroma != null) chroma.intensity.Override(Mathf.Clamp01(hurt * 0.8f + alert * 0.35f));
            if (color != null) color.saturation.Override(baseSaturation - alert * 25f - (player != null && player.IsDead ? 60f : 0f));

            if (dof != null)
            {
                dof.active = Settings.TiltShift;
                var cam = RoomCamera.Instance;
                if (cam != null && dof.active)
                {
                    Vector3 focus = player != null ? player.transform.position + Vector3.up * 0.8f : cam.FocusPoint;
                    float d = Vector3.Distance(cam.transform.position, focus);
                    dof.mode.Override(DepthOfFieldMode.Bokeh);
                    dof.focusDistance.Override(d);
                    dof.focalLength.Override(focalLength);
                    dof.aperture.Override(aperture);
                }
            }
#endif
        }
    }
}
