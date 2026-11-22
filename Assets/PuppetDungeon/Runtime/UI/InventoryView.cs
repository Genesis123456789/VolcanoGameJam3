using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>
    /// Inventaire / personnalisation « à la Diablo » : pantin au centre (aperçu en direct),
    /// 6 emplacements de membres autour, sac en grille, stats et corbeille.
    /// Le jeu est en pause tant que l'inventaire est ouvert.
    /// </summary>
    public class InventoryView : UIPanel
    {
        [Header("Cases")]
        public ItemSlotView[] equipSlots = new ItemSlotView[6];
        public RectTransform bagGrid;
        public ItemSlotView slotPrefab;
        public ItemSlotView trashSlot;

        [Header("Textes")]
        public TMP_Text statsText;
        public TMP_Text titleText;
        public TMP_Text bagCountText;
        public TMP_Text hintText;

        [Header("Aperçu du pantin")]
        public RawImage puppetPreview;

        private readonly List<ItemSlotView> bagSlots = new List<ItemSlotView>();
        private ItemSlotView dragSource;
        private ItemSlotView tooltipOwner;
        private Camera previewCam;
        private RenderTexture previewRT;
        private bool built;

        private Inventory Inv => GameManager.Instance.Run.inventory;
        private UIRoot Root => UIRoot.Instance;

        protected override void Awake()
        {
            base.Awake();
            bagSlots.Clear();
            built = false;
            dragSource = null;
            tooltipOwner = null;
            previewCam = null;
            previewRT = null;
            for (int i = 0; i < equipSlots.Length; i++)
            {
                if (equipSlots[i] == null) continue;
                equipSlots[i].Owner = this;
                equipSlots[i].kind = ItemSlotView.Kind.Equip;
                equipSlots[i].equipSlot = (PuppetSlot)i;
            }
            if (trashSlot != null)
            {
                trashSlot.Owner = this;
                trashSlot.kind = ItemSlotView.Kind.Trash;
            }
        }

        private void BuildBag()
        {
            if (built || GameManager.Instance == null || GameManager.Instance.Run == null) return;
            built = true;
            for (int i = 0; i < Inv.Size; i++)
            {
                var s = Instantiate(slotPrefab, bagGrid);
                s.name = "Case " + i;
                s.kind = ItemSlotView.Kind.Bag;
                s.bagIndex = i;
                s.Owner = this;
                bagSlots.Add(s);
            }
            if (bagSlots.Count > 0) firstSelected = bagSlots[0].gameObject;
        }

        protected override void OnOpened()
        {
            BuildBag();
            GameManager.Instance.SetPaused(true);
            Inv.Changed += Refresh;
            var player = PlayerController.Instance;
            if (player != null) player.Lock(true);
            EnablePreview(true);
            Refresh();
            hintMode = -1;
            UpdateHint();
        }

        private int hintMode = -1;
        private ItemSlotView trashArmed;
        private float trashArmedUntil;

        private void UpdateHint()
        {
            if (hintText == null) return;
            int mode = InputHints.UsingGamepad ? 1 : 0;
            if (mode == hintMode) return;
            hintMode = mode;
            hintText.text = mode == 1
                ? "A : équiper / retirer  •  X deux fois : détruire  •  B : fermer"
                : "Glisser-déposer pour équiper  •  Clic droit : équiper / retirer  •  Glisser dans la corbeille pour détruire";
        }

        private void Update()
        {
            if (!IsOpen) return;
            UpdateHint();
            // Manette : X sur une pièce sélectionnée la détruit (confirmation par un second appui).
            var g = UnityEngine.InputSystem.Gamepad.current;
            if (g == null || !g.buttonWest.wasPressedThisFrame) return;
            var es = EventSystem.current;
            var slot = es != null && es.currentSelectedGameObject != null ? es.currentSelectedGameObject.GetComponent<ItemSlotView>() : null;
            if (slot == null || slot.Part == null || slot.kind == ItemSlotView.Kind.Trash) return;
            var part = slot.Part;
            if (trashArmed == slot && Time.unscaledTime < trashArmedUntil)
            {
                trashArmed = null;
                Inv.Remove(part);
                AudioManager.Sfx(l => l.hit, 0.8f, 0.7f);
                if (Root != null) Root.Toast($"{part.DisplayName} réduit en copeaux", new Color(0.85f, 0.75f, 0.6f));
            }
            else
            {
                trashArmed = slot;
                trashArmedUntil = Time.unscaledTime + 1.5f;
                if (Root != null) Root.Toast($"X encore une fois pour détruire {part.DisplayName}", new Color(1f, 0.6f, 0.45f));
            }
        }

        protected override void OnClosed()
        {
            if (GameManager.Instance != null && GameManager.Instance.Run != null) Inv.Changed -= Refresh;
            CancelDrag();
            if (Root != null && Root.tooltip != null) Root.tooltip.Hide();
            EnablePreview(false);
            var player = PlayerController.Instance;
            if (player != null) player.Lock(false);
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public void Refresh()
        {
            if (!IsOpen) return;
            for (int i = 0; i < bagSlots.Count; i++) bagSlots[i].Bind(Inv.bag[i]);
            for (int i = 0; i < equipSlots.Length; i++) if (equipSlots[i] != null) equipSlots[i].Bind(Inv.equipped[i]);
            if (bagCountText != null) bagCountText.text = $"Sac  {Inv.Size - Inv.FreeCells}/{Inv.Size}";
            var run = GameManager.Instance.Run;
            if (titleText != null) titleText.text = $"{run.puppetName} <size=65%><color=#C9B79C>Pantin n°{run.generation}</color></size>";
            RefreshStats();
            if (tooltipOwner != null) ShowTooltip(tooltipOwner);
        }

        private void RefreshStats()
        {
            if (statsText == null) return;
            var gm = GameManager.Instance;
            var s = gm.ComputePlayerStats();
            var sb = new StringBuilder();
            void Line(string label, string value) => sb.Append("<color=#C9B79C>").Append(label).Append("</color><pos=62%>").Append(value).Append('\n');
            Line("Points de vie", $"{s[StatType.MaxHealth]:0}");
            Line("Dégâts", $"{s[StatType.Damage]:0}");
            Line("Armure", $"{s[StatType.Armor]:0}");
            Line("Vitesse", $"{s[StatType.MoveSpeed]:0.0}");
            Line("Vit. d'attaque", $"{s[StatType.AttackSpeed] * 100f:0}%");
            Line("Critique", $"{s[StatType.CritChance] * 100f:0}%  x{s[StatType.CritDamage]:0.00}");
            Line("Vol de vie", $"{s[StatType.LifeSteal] * 100f:0.#}%");
            Line("Ruée", $"{s[StatType.DashCooldown]:0.00}s");
            Line("Attaque", EnumUtil.AttackStyleLabel(gm.CurrentAttackStyle()));
            var passives = new Dictionary<PassiveKind, float>();
            foreach (var p in gm.Run.inventory.AllEquipped())
                foreach (var r in p.passives)
                    passives[r.kind] = (passives.TryGetValue(r.kind, out var v) ? v : 0f) + r.magnitude;
            if (passives.Count > 0)
            {
                sb.Append("\n<color=#F2C14E>Passifs</color>\n");
                foreach (var kv in passives) sb.Append("<size=85%>» ").Append(PassiveCatalog.Describe(kv.Key, kv.Value)).Append("</size>\n");
            }
            statsText.text = sb.ToString();
        }

        // ------------------------------------------------------------------ Info-bulle

        public void ShowTooltip(ItemSlotView slot)
        {
            tooltipOwner = slot;
            if (Root == null || Root.tooltip == null) return;
            if (slot.Part == null || dragSource != null) { Root.tooltip.Hide(); return; }
            PartInstance equipped = slot.kind == ItemSlotView.Kind.Bag ? Inv.GetEquipped(slot.Part.Slot) : null;
            Root.tooltip.Show(slot.Part, equipped, (RectTransform)slot.transform);
        }

        public void HideTooltip(ItemSlotView slot)
        {
            if (tooltipOwner != slot) return;
            tooltipOwner = null;
            if (Root != null && Root.tooltip != null) Root.tooltip.Hide();
        }

        // ------------------------------------------------------------------ Actions

        /// <summary>Clic droit / double clic / Valider : équipe depuis le sac, retire si équipée.</summary>
        public void QuickAction(ItemSlotView slot)
        {
            if (slot.Part == null) return;
            if (slot.kind == ItemSlotView.Kind.Bag)
            {
                var part = slot.Part;
                if (Inv.EquipFromBag(slot.bagIndex)) PunchEquip(part);
            }
            else if (slot.kind == ItemSlotView.Kind.Equip)
            {
                if (Inv.Unequip(slot.equipSlot)) AudioManager.Sfx(l => l.unequip);
                else if (Root != null) Root.Toast("Sac plein !", new Color(1f, 0.5f, 0.4f));
            }
        }

        private void PunchEquip(PartInstance part)
        {
            if (part == null) return;
            AudioManager.Sfx(l => l.equip);
            var view = equipSlots[(int)part.Slot];
            if (view != null) StartCoroutine(Juice.ScalePunch(view.transform, 0.25f, 0.3f));
            var player = PlayerController.Instance;
            if (player != null && player.squash != null) player.squash.Punch(new Vector3(0.15f, 0.2f, 0.15f));
        }

        public void BeginDrag(ItemSlotView slot, PointerEventData e)
        {
            dragSource = slot;
            if (Root != null && Root.tooltip != null) Root.tooltip.Hide();
            var cfg = GameManager.Instance.Config;
            if (Root != null && Root.dragGhost != null)
            {
                Root.dragGhost.Show(slot.icon != null ? slot.icon.sprite : null, cfg.RarityColor(slot.Part.rarity));
                Root.dragGhost.Move(e.position);
            }
            slot.icon.color = new Color(1f, 1f, 1f, 0.3f);
            // Met en évidence les cases compatibles
            for (int i = 0; i < equipSlots.Length; i++)
            {
                if (equipSlots[i] == null) continue;
                bool ok = (PuppetSlot)i == slot.Part.Slot;
                equipSlots[i].SetHighlight(ok ? new Color(0.5f, 1f, 0.45f, 0.55f) : new Color(1f, 0.3f, 0.3f, 0.25f));
            }
            if (trashSlot != null) trashSlot.SetHighlight(new Color(1f, 0.35f, 0.25f, 0.45f));
            AudioManager.Sfx(l => l.uiClick, 0.5f, 1.2f);
        }

        public void Drag(PointerEventData e)
        {
            if (dragSource != null && Root != null && Root.dragGhost != null) Root.dragGhost.Move(e.position);
        }

        public void EndDrag(PointerEventData e)
        {
            CancelDrag();
        }

        private void CancelDrag()
        {
            if (dragSource != null && dragSource.icon != null) dragSource.icon.color = Color.white;
            dragSource = null;
            if (Root != null && Root.dragGhost != null) Root.dragGhost.Hide();
            foreach (var s in equipSlots) if (s != null) s.SetHighlight(null);
            if (trashSlot != null) trashSlot.SetHighlight(null);
        }

        /// <summary>Dépôt d'une pièce glissée sur une case cible.</summary>
        public void DropOn(ItemSlotView target)
        {
            var src = dragSource;
            if (src == null || src == target || src.Part == null) return;
            var part = src.Part;

            switch (target.kind)
            {
                case ItemSlotView.Kind.Equip:
                    if (part.Slot != target.equipSlot)
                    {
                        if (Root != null) Root.Toast($"Cette pièce se fixe sur : {EnumUtil.SlotLabel(part.Slot)}", new Color(1f, 0.7f, 0.5f));
                        AudioManager.Sfx(l => l.uiClick, 0.6f, 0.6f);
                        break;
                    }
                    if (src.kind == ItemSlotView.Kind.Bag && Inv.EquipFromBag(src.bagIndex)) PunchEquip(part);
                    break;

                case ItemSlotView.Kind.Bag:
                    if (src.kind == ItemSlotView.Kind.Bag) { Inv.MoveInBag(src.bagIndex, target.bagIndex); AudioManager.Sfx(l => l.uiClick, 0.5f); }
                    else if (src.kind == ItemSlotView.Kind.Equip)
                    {
                        if (Inv.Unequip(src.equipSlot, target.bagIndex)) AudioManager.Sfx(l => l.unequip);
                    }
                    break;

                case ItemSlotView.Kind.Trash:
                    Inv.Remove(part);
                    AudioManager.Sfx(l => l.hit, 0.8f, 0.7f);
                    if (Root != null) Root.Toast($"{part.DisplayName} réduit en copeaux", new Color(0.85f, 0.75f, 0.6f));
                    break;
            }
            CancelDrag();
        }

        // ------------------------------------------------------------------ Aperçu du pantin

        private void EnablePreview(bool on)
        {
            if (puppetPreview == null) return;
            var player = PlayerController.Instance;
            if (!on || player == null)
            {
                if (previewCam != null) previewCam.enabled = false;
                return;
            }
            if (previewCam == null)
            {
                var go = new GameObject("Puppet Preview Camera");
                previewCam = go.AddComponent<Camera>();
                previewCam.clearFlags = CameraClearFlags.SolidColor;
                previewCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                previewCam.cullingMask = Layers.PlayerMask;
                previewCam.fieldOfView = 24f;
                previewCam.nearClipPlane = 0.1f;
                previewCam.farClipPlane = 20f;
                previewRT = new RenderTexture(512, 640, 16, RenderTextureFormat.ARGB32) { name = "PD_PuppetPreview" };
                previewCam.targetTexture = previewRT;
                puppetPreview.texture = previewRT;
            }
            var t = player.transform;
            previewCam.transform.position = t.position + t.forward * 5.2f + Vector3.up * 1.4f;
            previewCam.transform.LookAt(t.position + Vector3.up * 0.85f);
            previewCam.enabled = true;
        }

        private void OnDestroy()
        {
            if (previewRT != null) previewRT.Release();
            if (previewCam != null) Destroy(previewCam.gameObject);
        }
    }
}
