using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>
    /// Écran d'offrande : le joueur choisit la ou les pièces qu'il donne à la marionnette du village.
    /// La sélection multiple n'est possible que lors d'un événement rare.
    /// </summary>
    public class OfferingView : UIPanel
    {
        public RectTransform grid;
        public OfferingCardView cardPrefab;
        public TMP_Text header;
        public TMP_Text subHeader;
        public TMP_Text counter;
        public Button confirmButton;

        private readonly List<OfferingCardView> cards = new List<OfferingCardView>();
        private int maxSelection = 1;
        private System.Action<List<PartInstance>> onConfirm;

        protected override void Awake()
        {
            base.Awake();
            cards.Clear();
            if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        }

        public void Show(IEnumerable<PartInstance> equipped, IEnumerable<PartInstance> bag, int max, string villagerName, System.Action<List<PartInstance>> confirm)
        {
            onConfirm = confirm;
            maxSelection = Mathf.Max(1, max);
            foreach (var c in cards) if (c != null) Destroy(c.gameObject);
            cards.Clear();
            foreach (var p in equipped) AddCard(p, true);
            foreach (var p in bag) AddCard(p, false);
            if (header != null) header.text = maxSelection > 1 ? "Nuit des Fils Dorés !" : "L'Offrande";
            if (subHeader != null)
                subHeader.text = maxSelection > 1
                    ? $"Les fils brillent cette nuit : vous pouvez offrir jusqu'à <color=#F2C14E>{maxSelection}</color> pièces à {villagerName}."
                    : $"Choisissez la pièce que {villagerName} portera pour continuer votre voyage.";
            if (cards.Count > 0) firstSelected = cards[0].gameObject;
            Open();
            UpdateCounter();
            if (UIRoot.Instance != null) UIRoot.Instance.ModalOpen = true;
        }

        private void AddCard(PartInstance p, bool equipped)
        {
            var c = Instantiate(cardPrefab, grid);
            c.Bind(p, equipped);
            c.Clicked = OnCardClicked;
            cards.Add(c);
        }

        private void OnCardClicked(OfferingCardView card)
        {
            if (card.Selected) card.SetSelected(false);
            else
            {
                if (maxSelection == 1) foreach (var c in cards) c.SetSelected(false);
                else if (cards.Count(c => c.Selected) >= maxSelection)
                {
                    AudioManager.Sfx(l => l.uiClick, 0.6f, 0.6f);
                    return;
                }
                card.SetSelected(true);
                AudioManager.Sfx(l => l.equip, 0.7f);
            }
            UpdateCounter();
        }

        private void UpdateCounter()
        {
            int n = cards.Count(c => c.Selected);
            if (counter != null) counter.text = $"{n} / {maxSelection}";
            if (confirmButton != null) confirmButton.interactable = n > 0 || cards.Count == 0;
        }

        private void Confirm()
        {
            var chosen = cards.Where(c => c.Selected).Select(c => c.Part).ToList();
            if (chosen.Count == 0 && cards.Count > 0) return;
            if (UIRoot.Instance != null)
            {
                UIRoot.Instance.ModalOpen = false;
                if (UIRoot.Instance.tooltip != null) UIRoot.Instance.tooltip.Hide();
            }
            Close();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            onConfirm?.Invoke(chosen);
        }
    }
}
