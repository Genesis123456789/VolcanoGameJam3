using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>Menu principal.</summary>
    public class MainMenuView : MonoBehaviour
    {
        public Button playButton;
        public Button optionsButton;
        public Button quitButton;
        public OptionsView options;
        public TMP_Text title;
        public RectTransform titleRoot;

        private void Start()
        {
            UIRoot.EnsureEventSystem();
            if (playButton != null) playButton.onClick.AddListener(() => GameManager.Instance.StartNewRunFromMenu());
            if (optionsButton != null) optionsButton.onClick.AddListener(() => { if (options != null) options.Toggle(); });
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
            if (playButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(playButton.gameObject);
            var gm = GameManager.Instance;
            if (gm != null && gm.Config.audio != null && AudioManager.Instance != null)
                AudioManager.Instance.PlayMusic(gm.Config.audio.musicMenu != null ? gm.Config.audio.musicMenu : gm.Config.audio.musicVillage);
            if (gm != null) gm.SetPaused(false);
        }

        private void Update()
        {
            if (titleRoot != null)
                titleRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 0.8f) * 2f);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
