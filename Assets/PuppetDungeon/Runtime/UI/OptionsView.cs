using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>Options : volumes, secousses d'écran, arrêt sur image, effet maquette.</summary>
    public class OptionsView : UIPanel
    {
        public Slider master;
        public Slider music;
        public Slider sfx;
        public Slider shake;
        public Toggle hitStop;
        public Toggle tiltShift;
        public Button closeButton;

        protected override void Awake()
        {
            base.Awake();
            if (master != null) master.onValueChanged.AddListener(v => Settings.Master = v);
            if (music != null) music.onValueChanged.AddListener(v => Settings.Music = v);
            if (sfx != null) sfx.onValueChanged.AddListener(v => { Settings.Sfx = v; AudioManager.Sfx(l => l.uiClick, 0.6f); });
            if (shake != null) shake.onValueChanged.AddListener(v => Settings.ScreenShake = v);
            if (hitStop != null) hitStop.onValueChanged.AddListener(v => Settings.HitStop = v);
            if (tiltShift != null) tiltShift.onValueChanged.AddListener(v => Settings.TiltShift = v);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        protected override void OnOpened()
        {
            if (master != null) master.SetValueWithoutNotify(Settings.Master);
            if (music != null) music.SetValueWithoutNotify(Settings.Music);
            if (sfx != null) sfx.SetValueWithoutNotify(Settings.Sfx);
            if (shake != null) shake.SetValueWithoutNotify(Settings.ScreenShake);
            if (hitStop != null) hitStop.SetIsOnWithoutNotify(Settings.HitStop);
            if (tiltShift != null) tiltShift.SetIsOnWithoutNotify(Settings.TiltShift);
        }
    }
}
