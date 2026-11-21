using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>Carte d'une pièce dans l'écran d'offrande du village.</summary>
    public class OfferingCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public Button button;
        public Image icon;
        public Image rarityBar;
        public Image selectedFrame;
        public TMP_Text nameText;
        public TMP_Text infoText;

        public PartInstance Part { get; private set; }
        public bool Selected { get; private set; }
        public System.Action<OfferingCardView> Clicked;

        public void Bind(PartInstance part, bool equipped)
        {
            Part = part;
            var cfg = GameManager.Instance.Config;
            Color c = cfg.RarityColor(part.rarity);
            if (rarityBar != null) rarityBar.color = c;
            if (nameText != null) { nameText.text = part.DisplayName; nameText.color = c; }
            if (infoText != null) infoText.text = $"{EnumUtil.SlotLabel(part.Slot)} • Niv. {part.itemLevel}{(equipped ? " • <color=#F2C14E>porté</color>" : "")}";
            if (icon != null)
            {
                var def = part.definition;
                var s = PartIconRenderer.Get(def, sp => { if (this != null && icon != null) { icon.sprite = sp; icon.enabled = sp != null; } });
                icon.sprite = s;
                icon.enabled = s != null;
                icon.preserveAspect = true;
            }
            if (button != null) button.onClick.AddListener(() => Clicked?.Invoke(this));
            SetSelected(false);
        }

        public void SetSelected(bool on)
        {
            Selected = on;
            if (selectedFrame != null) selectedFrame.enabled = on;
            transform.localScale = Vector3.one * (on ? 1.06f : 1f);
        }

        public void OnPointerEnter(PointerEventData e) => ShowTip();
        public void OnSelect(BaseEventData e) => ShowTip();
        public void OnPointerExit(PointerEventData e) => HideTip();
        public void OnDeselect(BaseEventData e) => HideTip();

        private void ShowTip()
        {
            var root = UIRoot.Instance;
            if (root != null && root.tooltip != null && Part != null) root.tooltip.Show(Part, null, (RectTransform)transform);
        }

        private void HideTip()
        {
            var root = UIRoot.Instance;
            if (root != null && root.tooltip != null) root.tooltip.Hide();
        }
    }
}
