using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PuppetDungeon
{
    /// <summary>Boîte de dialogue avec effet machine à écrire (clic / touche pour avancer).</summary>
    public class DialogView : UIPanel
    {
        public TMP_Text speaker;
        public TMP_Text text;
        public GameObject continueHint;
        public float charsPerSecond = 45f;


        public IEnumerator Say(string who, string line)
        {
            if (!IsOpen) Open();
            if (speaker != null) speaker.text = who;
            if (continueHint != null) continueHint.SetActive(false);
            text.text = line;
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate();
            int total = text.textInfo.characterCount;
            float shown = 0f;
            yield return null;
            while (text.maxVisibleCharacters < total)
            {
                if (Pressed()) { text.maxVisibleCharacters = total; break; }
                shown += Time.unscaledDeltaTime * charsPerSecond;
                int n = Mathf.Min(total, Mathf.FloorToInt(shown));
                if (n != text.maxVisibleCharacters && n % 3 == 0) AudioManager.Sfx(l => l.uiHover, 0.15f, Random.Range(1.4f, 1.7f));
                text.maxVisibleCharacters = n;
                yield return null;
            }
            if (continueHint != null) continueHint.SetActive(true);
            yield return null;
            while (!Pressed()) yield return null;
            AudioManager.Sfx(l => l.uiClick, 0.5f);
        }

        private static bool Pressed()
        {
            var k = Keyboard.current;
            var m = Mouse.current;
            var g = Gamepad.current;
            return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame))
                || (m != null && m.leftButton.wasPressedThisFrame)
                || (g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame));
        }
    }
}
