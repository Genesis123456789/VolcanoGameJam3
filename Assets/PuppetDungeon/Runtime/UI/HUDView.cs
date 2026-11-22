using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>Barre de vie, identité du pantin, progression du donjon, ruée, invite d'interaction.</summary>
    public class HUDView : MonoBehaviour
    {
        public Image healthFill;
        public Image healthLag;
        public TMP_Text healthText;
        public TMP_Text puppetName;
        public TMP_Text dungeonLabel;
        public TMP_Text villageLabel;
        public Image dashFill;
        public TMP_Text styleLabel;
        public TMP_Text currencyLabel;
        public CanvasGroup promptGroup;
        public TMP_Text promptText;
        public RectTransform healthPunch;
        [Tooltip("Aide des touches (vide = recherchée sous « Touches »).")]
        public TMP_Text keysText;
        private int keysMode = -1;

        private float lag = 1f;
        private float punchUntil;
        private float lastHealth = -1f;

        private void Start()
        {
            if (keysText == null)
                foreach (var t in GetComponentsInChildren<Transform>(true))
                    if (t.name == "Touches") { keysText = t.GetComponentInChildren<TMP_Text>(true); break; }
        }

        private void Update()
        {
            // Aide des touches : clavier ou manette selon le dernier périphérique utilisé.
            int mode = InputHints.UsingGamepad ? 1 : 0;
            if (keysText != null && mode != keysMode) { keysMode = mode; keysText.text = InputHints.HudHelp(); }

            var player = PlayerController.Instance;
            var gm = GameManager.Instance;
            if (gm == null || gm.Run == null) return;

            if (player != null && player.Health != null)
            {
                float n = player.Health.Normalized;
                if (healthFill != null) healthFill.fillAmount = n;
                lag = n > lag ? n : Mathf.MoveTowards(lag, n, Time.unscaledDeltaTime * 0.4f);
                if (healthLag != null) healthLag.fillAmount = lag;
                if (healthText != null) healthText.text = $"{Mathf.CeilToInt(player.Health.Current)} / {Mathf.CeilToInt(player.Health.maxHealth)}";
                if (lastHealth >= 0f && player.Health.Current < lastHealth && healthPunch != null && Time.unscaledTime > punchUntil)
                {
                    punchUntil = Time.unscaledTime + 0.3f;
                    StartCoroutine(Juice.ScalePunch(healthPunch, 0.12f, 0.25f));
                }
                lastHealth = player.Health.Current;
                if (dashFill != null) dashFill.fillAmount = player.DashReady01;
            }

            if (puppetName != null) puppetName.text = $"{gm.Run.puppetName} <size=70%><color=#C9B79C>• Pantin n°{gm.Run.generation}</color></size>";
            if (styleLabel != null) styleLabel.text = EnumUtil.AttackStyleLabel(gm.CurrentAttackStyle());
            if (currencyLabel != null) currencyLabel.text = gm.Run.currency > 0 ? $"<color=#F2C14E>{gm.Run.currency}</color> {gm.Config.currencyName}" : "";

            var dm = DungeonManager.Instance;
            if (dm != null)
            {
                if (dungeonLabel != null)
                {
                    int roomNo = dm.CurrentRoom != null ? dm.RoomNumber(dm.CurrentRoom) : 1;
                    dungeonLabel.text = $"Donjon {gm.Run.dungeonIndex + 1} <size=75%><color=#C9B79C>• Salle {roomNo} • {gm.Config.DungeonName(gm.Run.dungeonIndex)}</color></size>";
                }
                if (villageLabel != null) villageLabel.text = dm.VillageCountdownText();
            }
            else
            {
                if (dungeonLabel != null) dungeonLabel.text = "Village des marionnettes";
                if (villageLabel != null) villageLabel.text = "";
            }

            // Invite d'interaction
            if (promptGroup != null)
            {
                Interactable near = player != null && !player.ControlLocked ? Interactable.Nearest(player.transform.position) : null;
                float target = near != null ? 1f : 0f;
                promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, target, Time.unscaledDeltaTime * 6f);
                if (near != null && promptText != null) promptText.text = $"<color=#F2C14E>[{InputHints.Interact}]</color> {near.prompt}";
            }
        }
    }
}
