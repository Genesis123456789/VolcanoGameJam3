using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>Info-bulle de pièce : nom coloré par rareté, stats, passifs, histoire, comparaison.</summary>
    public class TooltipView : MonoBehaviour
    {
        public RectTransform panel;
        public TMP_Text title;
        public TMP_Text rarity;
        public TMP_Text body;
        public TMP_Text compare;
        public Image rarityBar;
        public CanvasGroup group;

        private Canvas canvas;
        private RectTransform anchor;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            if (group == null) group = GetComponent<CanvasGroup>();
            Hide();
        }

        public void Show(PartInstance part, PartInstance equipped, RectTransform near)
        {
            if (part == null) { Hide(); return; }
            var cfg = GameManager.Instance.Config;
            gameObject.SetActive(true);
            anchor = near;
            Color c = cfg.RarityColor(part.rarity);
            if (title != null) { title.text = part.DisplayName; title.color = c; }
            if (rarity != null) rarity.text = EnumUtil.RarityLabel(part.rarity).ToUpperInvariant();
            if (rarityBar != null) rarityBar.color = c;
            if (body != null) body.text = part.BuildTooltipBody();
            if (compare != null)
            {
                compare.gameObject.SetActive(equipped != null && equipped != part);
                if (equipped != null && equipped != part) compare.text = BuildCompare(part, equipped);
            }
            if (group != null) group.alpha = 1f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            Place();
        }

        private static string BuildCompare(PartInstance part, PartInstance equipped)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<color=#C9B79C>Comparé à l'équipement actuel :</color>\n");
            var types = System.Enum.GetValues(typeof(StatType));
            bool any = false;
            foreach (StatType t in types)
            {
                float a = Sum(part, t), b = Sum(equipped, t);
                float d = a - b;
                if (Mathf.Abs(d) < 0.0001f) continue;
                any = true;
                bool better = t == StatType.DashCooldown ? d < 0 : d > 0;
                sb.Append(better ? "<color=#9BE37A>▲ " : "<color=#E36B5B>▼ ");
                sb.Append(new StatModifier(t, ModifierMode.Flat, d).Describe()).Append("</color>\n");
            }
            if (!any) sb.Append("<color=#A89A86>Stats identiques</color>");
            return sb.ToString().TrimEnd('\n');
        }

        private static float Sum(PartInstance p, StatType t)
        {
            float s = 0f;
            foreach (var m in p.stats) if (m.stat == t && m.mode == ModifierMode.Flat) s += m.value;
            return s;
        }

        private void Place()
        {
            if (anchor == null || panel == null) return;
            var parent = (RectTransform)panel.parent;
            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 screenRight = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            Vector2 screenLeft = RectTransformUtility.WorldToScreenPoint(cam, corners[1]);
            bool placeLeft = screenRight.x > Screen.width * 0.6f;
            Vector2 target = placeLeft ? screenLeft + new Vector2(-12f, 0f) : screenRight + new Vector2(12f, 0f);
            panel.pivot = new Vector2(placeLeft ? 1f : 0f, 1f);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, target, cam, out var local))
                panel.anchoredPosition = local;
            // Garde dans l'écran (verticalement)
            panel.GetWorldCorners(corners);
            Vector2 bottom = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            if (bottom.y < 8f)
            {
                Vector2 fix = target + new Vector2(0f, 8f - bottom.y);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, fix, cam, out local)) panel.anchoredPosition = local;
            }
        }

        public void Hide()
        {
            anchor = null;
            if (group != null) group.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
