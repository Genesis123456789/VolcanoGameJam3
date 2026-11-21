using TMPro;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Chiffre de dégâts flottant (TextMeshPro 3D, toujours face caméra).</summary>
    public class DamageNumber : MonoBehaviour
    {
        public TMP_Text label;
        public float life = 0.9f;
        public float rise = 1.6f;

        private float t;
        private Vector3 velocity;
        private float baseSize = 1f;
        private Color color;

        public void Show(float amount, bool crit, Color c)
        {
            if (label == null) label = GetComponentInChildren<TMP_Text>();
            label.text = crit ? $"{Mathf.RoundToInt(amount)}!" : Mathf.RoundToInt(amount).ToString();
            color = crit ? new Color(1f, 0.85f, 0.2f) : c;
            baseSize = crit ? 1.5f : 1f;
            velocity = new Vector3(Random.Range(-0.6f, 0.6f), rise, 0f);
            label.color = color;
        }

        public void ShowText(string text, Color c, float size)
        {
            if (label == null) label = GetComponentInChildren<TMP_Text>();
            label.text = text;
            color = c;
            baseSize = size;
            velocity = new Vector3(0f, rise * 0.6f, 0f);
            life *= 1.6f;
            label.color = color;
        }

        private void LateUpdate()
        {
            t += Time.unscaledDeltaTime;
            float k = t / life;
            if (k >= 1f) { Destroy(gameObject); return; }
            velocity.y -= 3.5f * Time.unscaledDeltaTime;
            transform.position += velocity * Time.unscaledDeltaTime;
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
            float pop = k < 0.15f ? Mathf.Lerp(0.4f, 1.25f, k / 0.15f) : Mathf.Lerp(1.25f, 1f, (k - 0.15f) / 0.2f);
            transform.localScale = Vector3.one * baseSize * pop;
            if (label != null)
            {
                var c = color;
                c.a = 1f - Mathf.Clamp01((k - 0.6f) / 0.4f);
                label.color = c;
            }
        }
    }
}
