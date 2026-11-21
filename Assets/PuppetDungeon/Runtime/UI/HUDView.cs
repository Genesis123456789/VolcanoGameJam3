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
        public CanvasGroup promptGroup;
        public TMP_Text promptText;
        public RectTransform healthPunch;

        private float lag = 1f;
        private float punchUntil;
        private float lastHealth = -1f;

        private void Update()
        {
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
                if (near != null && promptText != null) promptText.text = $"<color=#F2C14E>[E]</color> {near.prompt}";
            }
        }
    }
}
