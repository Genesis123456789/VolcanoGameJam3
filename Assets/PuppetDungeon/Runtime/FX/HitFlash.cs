using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Teinte + flash blanc des renderers via MaterialPropertyBlock (pas de duplication de matériaux).
    /// Gère aussi la lueur des pièces légendaires.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private class Entry
        {
            public Renderer renderer;
            public Color original;
            public Color tint = Color.white;
            public Color glow = Color.black;
        }

        private readonly List<Entry> entries = new List<Entry>();
        private MaterialPropertyBlock block;
        private float flash;
        private Color flashColor = Color.white;
        private float flashDuration = 0.12f;
        private Color globalTint = Color.white;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            Refresh();
        }

        /// <summary>À appeler après ajout/suppression de pièces.</summary>
        public void Refresh()
        {
            if (block == null) block = new MaterialPropertyBlock();
            var keep = new Dictionary<Renderer, Entry>();
            foreach (var e in entries) if (e.renderer != null) keep[e.renderer] = e;
            entries.Clear();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                if (r.GetComponent<TMPro.TMP_Text>() != null) continue;
                if (keep.TryGetValue(r, out var existing)) { entries.Add(existing); continue; }
                var m = r.sharedMaterial;
                Color c = Color.white;
                if (m != null)
                {
                    if (m.HasProperty(BaseColorId)) c = m.GetColor(BaseColorId);
                    else if (m.HasProperty(ColorId)) c = m.GetColor(ColorId);
                }
                entries.Add(new Entry { renderer = r, original = c });
            }
            Apply();
        }

        public void SetGlobalTint(Color c)
        {
            globalTint = c;
            Apply();
        }

        public void SetTint(IEnumerable<Renderer> renderers, Color tint, Color glow)
        {
            var set = new HashSet<Renderer>(renderers);
            foreach (var e in entries)
            {
                if (!set.Contains(e.renderer)) continue;
                e.tint = tint;
                e.glow = glow;
            }
            Apply();
        }

        public void Flash(Color color, float duration = 0.12f)
        {
            flashColor = color;
            flashDuration = Mathf.Max(0.01f, duration);
            flash = 1f;
            Apply();
        }

        private void Update()
        {
            if (flash <= 0f) return;
            flash = Mathf.Max(0f, flash - Time.unscaledDeltaTime / flashDuration);
            Apply();
        }

        private void Apply()
        {
            if (block == null) return;
            foreach (var e in entries)
            {
                if (e.renderer == null) continue;
                Color c = e.original * e.tint * globalTint;
                c.a = e.original.a;
                c = Color.Lerp(c, flashColor, flash * 0.85f);
                e.renderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, c);
                block.SetColor(ColorId, c);
                block.SetColor(EmissionId, Color.Lerp(e.glow, flashColor * 0.6f, flash));
                e.renderer.SetPropertyBlock(block);
            }
        }
    }
}
