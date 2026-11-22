using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>
    /// Écran du gacha (la Tisseuse, au village) : monnaie, probabilités, garantie, tirage simple ou
    /// multiple avec révélation carte par carte. Les pièces obtenues vont dans le sac.
    /// </summary>
    public class GachaView : UIPanel
    {
        public TMP_Text title;
        public TMP_Text flavor;
        public TMP_Text currencyText;
        public TMP_Text oddsText;
        public TMP_Text pityText;
        public Button pullOneButton;
        public Button pullMultiButton;
        public Button closeButton;
        public TMP_Text pullOneLabel;
        public TMP_Text pullMultiLabel;
        public RectTransform resultsGrid;
        public OfferingCardView cardPrefab;
        public TMP_Text resultHint;

        private readonly List<OfferingCardView> cards = new List<OfferingCardView>();
        private bool revealing;

        protected override void Awake()
        {
            base.Awake();
            cards.Clear();
            revealing = false;
            if (pullOneButton != null) pullOneButton.onClick.AddListener(() => DoPull(false));
            if (pullMultiButton != null) pullMultiButton.onClick.AddListener(() => DoPull(true));
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void Show(string npcName)
        {
            if (title != null) title.text = npcName;
            ClearCards();
            if (resultHint != null) resultHint.text = $"Les pièces tirées rejoignent votre sac ({InputHints.Inventory} pour les équiper).";
            if (UIRoot.Instance != null) UIRoot.Instance.ModalOpen = true;
            firstSelected = pullOneButton != null ? pullOneButton.gameObject : null;
            Open();
            Refresh();
        }

        public override void Close()
        {
            if (!IsOpen) return;
            StopAllCoroutines();
            revealing = false;
            base.Close();
            if (UIRoot.Instance != null)
            {
                UIRoot.Instance.ModalOpen = false;
                if (UIRoot.Instance.tooltip != null) UIRoot.Instance.tooltip.Hide();
            }
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void Refresh()
        {
            var cfg = GameManager.Instance.Config;
            string cur = cfg.currencyName;
            if (currencyText != null) currencyText.text = $"<color=#F2C14E>{Gacha.Currency}</color> {cur}";
            if (pullOneLabel != null) pullOneLabel.text = $"Tirer x1 <size=75%>({Gacha.Cost(false)})</size>";
            if (pullMultiLabel != null) pullMultiLabel.text = $"Tirer x{Gacha.Count(true)} <size=75%>({Gacha.Cost(true)})</size>";
            if (pullOneButton != null) pullOneButton.interactable = !revealing && Gacha.CanAfford(false);
            if (pullMultiButton != null) pullMultiButton.interactable = !revealing && Gacha.CanAfford(true);
            if (closeButton != null) closeButton.interactable = !revealing;

            if (oddsText != null)
            {
                var odds = Gacha.Odds();
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < odds.Length; i++)
                {
                    if (i > 0) sb.Append("   ");
                    sb.Append($"<color={cfg.RarityHex((Rarity)i)}>{EnumUtil.RarityLabel((Rarity)i)}</color> {odds[i] * 100f:0.#}%");
                }
                oddsText.text = sb.ToString();
            }
            if (pityText != null)
                pityText.text = cfg.gachaPity > 0
                    ? $"Pièce <color={cfg.RarityHex(Rarity.Epic)}>Épique</color> garantie dans <b>{Gacha.PullsBeforePity}</b> tirage(s)"
                    : "";
        }

        private void DoPull(bool multi)
        {
            if (revealing || !Gacha.CanAfford(multi)) { AudioManager.Sfx(l => l.uiClick, 0.6f, 0.6f); return; }
            var results = Gacha.Pull(multi);
            if (results == null) return;
            StartCoroutine(Reveal(results));
        }

        private void ClearCards()
        {
            foreach (var c in cards) if (c != null) Destroy(c.gameObject);
            cards.Clear();
        }

        private IEnumerator Reveal(List<Gacha.Result> results)
        {
            revealing = true;
            Refresh();
            ClearCards();
            var cfg = GameManager.Instance.Config;
            foreach (var r in results)
            {
                if (cardPrefab == null || resultsGrid == null) break;
                var card = Instantiate(cardPrefab, resultsGrid);
                card.Bind(r.part, false);
                if (card.infoText != null)
                    card.infoText.text = r.recycled
                        ? $"{EnumUtil.SlotLabel(r.part.Slot)} • <color=#E08060>sac plein : +{r.refund} {cfg.currencyName}</color>"
                        : $"{EnumUtil.SlotLabel(r.part.Slot)} • Niv. {r.part.itemLevel}{(r.pity ? " • <color=#F2C14E>garantie</color>" : "")}";
                cards.Add(card);
                yield return PopIn(card.transform, r.part.rarity);
            }
            revealing = false;
            Refresh();
            if (cards.Count > 0 && EventSystem.current != null && pullOneButton != null)
                EventSystem.current.SetSelectedGameObject(pullOneButton.interactable ? pullOneButton.gameObject : (closeButton != null ? closeButton.gameObject : null));
        }

        private IEnumerator PopIn(Transform t, Rarity rarity)
        {
            bool big = rarity >= Rarity.Epic;
            AudioManager.Sfx(l => big ? l.legendaryDrop : l.lootDrop, big ? 1f : 0.8f, 0.9f + (int)rarity * 0.08f);
            if (big) Juice.Shake(rarity == Rarity.Legendary ? 0.35f : 0.18f);
            float dur = big ? 0.45f : 0.25f;
            for (float k = 0f; k < dur; k += Time.unscaledDeltaTime)
            {
                float x = k / dur;
                t.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, 1f, EaseOutBack(x));
                t.localRotation = Quaternion.Euler(0f, 0f, (1f - x) * (big ? 12f : 5f) * Mathf.Sin(x * 30f));
                yield return null;
            }
            t.localScale = Vector3.one;
            t.localRotation = Quaternion.identity;
            yield return new WaitForSecondsRealtime(big ? 0.25f : 0.08f);
        }
    }
}
