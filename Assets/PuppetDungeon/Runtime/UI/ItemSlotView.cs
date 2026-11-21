using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>
    /// Case d'inventaire (sac, emplacement du pantin ou corbeille).
    /// Glisser-déposer, clic droit = équiper/retirer, survol = info-bulle avec comparaison.
    /// </summary>
    public class ItemSlotView : Selectable, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IPointerClickHandler, ISubmitHandler
    {
        public enum Kind { Bag, Equip, Trash }

        [Header("Case")]
        public Kind kind = Kind.Bag;
        public int bagIndex;
        public PuppetSlot equipSlot;

        [Header("Visuel")]
        public Image frame;
        public Image rarityGlow;
        public Image icon;
        public TMP_Text levelText;
        public TMP_Text placeholder;
        public Image highlight;

        public InventoryView Owner { get; set; }
        public PartInstance Part { get; private set; }

        private bool hovered;

        public void Bind(PartInstance part)
        {
            Part = part;
            var cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (part == null)
            {
                if (icon != null) { icon.enabled = false; icon.sprite = null; }
                if (rarityGlow != null) rarityGlow.color = new Color(1f, 1f, 1f, 0f);
                if (levelText != null) levelText.text = "";
                if (placeholder != null) placeholder.enabled = true;
                return;
            }
            if (placeholder != null) placeholder.enabled = false;
            if (levelText != null) levelText.text = part.itemLevel.ToString();
            if (rarityGlow != null && cfg != null)
            {
                var c = cfg.RarityColor(part.rarity);
                c.a = part.rarity == Rarity.Common ? 0.15f : 0.55f;
                rarityGlow.color = c;
            }
            if (icon != null)
            {
                var def = part.definition;
                var s = PartIconRenderer.Get(def, sp =>
                {
                    if (this != null && icon != null && Part != null && Part.definition == def) { icon.sprite = sp; icon.enabled = sp != null; }
                });
                icon.sprite = s;
                icon.enabled = s != null;
                icon.preserveAspect = true;
            }
        }

        public void SetHighlight(Color? c)
        {
            if (highlight == null) return;
            highlight.enabled = c.HasValue;
            if (c.HasValue) highlight.color = c.Value;
        }

        // ------------------------------------------------------------------ Survol / sélection

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            hovered = true;
            if (Owner != null) Owner.ShowTooltip(this);
            AudioManager.Sfx(l => l.uiHover, 0.25f, 1.3f);
            transform.localScale = Vector3.one * 1.06f;
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            hovered = false;
            if (Owner != null) Owner.HideTooltip(this);
            transform.localScale = Vector3.one;
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            if (Owner != null) Owner.ShowTooltip(this);
            transform.localScale = Vector3.one * 1.06f;
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            if (Owner != null && !hovered) Owner.HideTooltip(this);
            transform.localScale = Vector3.one;
        }

        // ------------------------------------------------------------------ Clics

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Owner == null) return;
            if (eventData.button == PointerEventData.InputButton.Right || eventData.clickCount >= 2)
                Owner.QuickAction(this);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (Owner != null) Owner.QuickAction(this);
        }

        // ------------------------------------------------------------------ Glisser-déposer

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Part == null || Owner == null || kind == Kind.Trash || eventData.button != PointerEventData.InputButton.Left) return;
            Owner.BeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Owner != null) Owner.Drag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Owner != null) Owner.EndDrag(eventData);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (Owner != null) Owner.DropOn(this);
        }
    }
}
