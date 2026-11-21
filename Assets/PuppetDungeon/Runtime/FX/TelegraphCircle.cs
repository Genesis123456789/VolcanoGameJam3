using System.Collections;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Disque d'avertissement au sol : se remplit pendant l'armement d'une attaque.</summary>
    public class TelegraphCircle : MonoBehaviour
    {
        public Transform fill;
        public Renderer outline;
        public Color color = new Color(1f, 0.25f, 0.2f, 0.6f);

        public void Play(float duration)
        {
            StartCoroutine(Run(duration));
        }

        private IEnumerator Run(float duration)
        {
            var mpb = new MaterialPropertyBlock();
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                if (fill != null) fill.localScale = new Vector3(k, 1f, k);
                if (outline != null)
                {
                    outline.GetPropertyBlock(mpb);
                    var c = color;
                    c.a = color.a * (0.5f + 0.5f * Mathf.Sin(t * 25f));
                    mpb.SetColor("_BaseColor", c);
                    mpb.SetColor("_Color", c);
                    outline.SetPropertyBlock(mpb);
                }
                yield return null;
            }
            Destroy(gameObject, 0.3f);
        }
    }
}
