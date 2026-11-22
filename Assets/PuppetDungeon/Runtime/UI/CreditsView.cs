using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>Crédits défilants (contenu : asset CreditsData référencé dans PD_GameConfig).</summary>
    public class CreditsView : UIPanel
    {
        public RectTransform viewport;
        public RectTransform content;
        public TMP_Text body;
        public Button closeButton;
        public Color titleColor = new Color(0.95f, 0.76f, 0.31f);
        public Color headingColor = new Color(0.95f, 0.76f, 0.31f);

        private CreditsData data;
        private Coroutine scroll;

        protected override void Awake()
        {
            base.Awake();
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void Show()
        {
            var gm = GameManager.Instance;
            data = gm != null && gm.Config != null ? gm.Config.credits : null;
            if (body != null) body.text = data != null ? data.BuildText(titleColor, headingColor) : GameInfo.Title;
            if (closeButton != null) firstSelected = closeButton.gameObject;
            Open();
            if (data != null && data.music != null && AudioManager.Instance != null) AudioManager.Instance.PlayMusic(data.music, 1f);
            if (scroll != null) StopCoroutine(scroll);
            scroll = StartCoroutine(Scroll());
        }

        /// <summary>Échap / B sont gérés ici (fermeture + reprise de la musique).</summary>
        public override bool CloseOnCancel => false;

        public override void Close()
        {
            if (!IsOpen) return;
            if (scroll != null) StopCoroutine(scroll);
            scroll = null;
            base.Close();
            var gm = GameManager.Instance;
            if (data != null && data.music != null && gm != null && gm.Config.audio != null && AudioManager.Instance != null)
                AudioManager.Instance.PlayMusic(gm.Config.audio.musicMenu != null ? gm.Config.audio.musicMenu : gm.Config.audio.musicVillage);
        }

        private IEnumerator Scroll()
        {
            yield return null;
            if (content == null || viewport == null || body == null) yield break;
            body.ForceMeshUpdate();
            float h = Mathf.Max(body.preferredHeight, 10f);
            content.sizeDelta = new Vector2(content.sizeDelta.x, h);
            float start = -viewport.rect.height;
            float end = h;
            float speed = data != null ? data.scrollSpeed : 70f;
            while (true)
            {
                for (float y = start; y < end; y += speed * Time.unscaledDeltaTime * (FastForward() ? 4f : 1f))
                {
                    content.anchoredPosition = new Vector2(0f, y);
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        private static bool FastForward()
        {
            var k = Keyboard.current;
            var m = Mouse.current;
            var g = Gamepad.current;
            return (k != null && (k.spaceKey.isPressed || k.enterKey.isPressed)) || (m != null && m.leftButton.isPressed)
                || (g != null && (g.buttonWest.isPressed || g.rightTrigger.isPressed));
        }

        private void Update()
        {
            if (!IsOpen) return;
            var k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame) Close();
            var g = Gamepad.current;
            if (g != null && g.buttonEast.wasPressedThisFrame) Close();
        }
    }
}
