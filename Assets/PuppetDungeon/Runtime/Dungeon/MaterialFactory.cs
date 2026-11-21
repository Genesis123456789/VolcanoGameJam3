using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PuppetDungeon
{
    /// <summary>Crée (et met en cache) des matériaux teintés compatibles URP ou Built-in.</summary>
    public static class MaterialFactory
    {
        private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static bool IsURP => GraphicsSettings.currentRenderPipeline != null;

        public static Shader LitShader
        {
            get
            {
                Shader s = IsURP ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
                if (s == null) s = Shader.Find("Standard");
                return s;
            }
        }

        /// <summary>Copie teintée d'un matériau de base (ou matériau Lit neuf si base == null).</summary>
        public static Material Tinted(Material baseMat, Color color, string key = null, float emission = 0f)
        {
            string k = (key ?? (baseMat != null ? baseMat.name : "lit")) + "_" + ColorUtility.ToHtmlStringRGBA(color) + "_" + emission.ToString("0.00");
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = baseMat != null ? new Material(baseMat) : new Material(LitShader);
            m.name = k;
            SetColor(m, color);
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * emission);
            }
            cache[k] = m;
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        public static void ClearCache() => cache.Clear();
    }
}
