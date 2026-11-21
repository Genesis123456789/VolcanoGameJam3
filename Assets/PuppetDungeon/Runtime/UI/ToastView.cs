using System.Collections;
using TMPro;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Notification courte en bas d'écran (butin ramassé, sac plein…).</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ToastView : MonoBehaviour
    {
        public TMP_Text label;
        public float life = 2.6f;

        public void Show(string text, Color color)
        {
            if (label == null) label = GetComponentInChildren<TMP_Text>();
            label.text = text;
            label.color = color;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var g = GetComponent<CanvasGroup>();
            var rt = (RectTransform)transform;
            for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                g.alpha = t / 0.2f;
                rt.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, UIPanel.EaseOutBack(t / 0.2f));
                yield return null;
            }
            g.alpha = 1f;
            rt.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(life);
            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                g.alpha = 1f - t / 0.4f;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
