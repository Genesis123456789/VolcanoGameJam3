using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>Menu pause : reprendre, options, retour au menu.</summary>
    public class PauseMenuView : UIPanel
    {
        public Button resumeButton;
        public Button optionsButton;
        public Button menuButton;
        public OptionsView options;

        protected override void Awake()
        {
            base.Awake();
            if (resumeButton != null) resumeButton.onClick.AddListener(Close);
            if (optionsButton != null) optionsButton.onClick.AddListener(() => { if (options != null) options.Toggle(); });
            if (menuButton != null) menuButton.onClick.AddListener(() => GameManager.Instance.ReturnToMenu());
        }

        protected override void OnOpened()
        {
            GameManager.Instance.SetPaused(true);
            var p = PlayerController.Instance;
            if (p != null) p.Lock(true);
        }

        protected override void OnClosed()
        {
            if (options != null && options.IsOpen) options.Close();
            var p = PlayerController.Instance;
            if (p != null) p.Lock(false);
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        }
    }
}
