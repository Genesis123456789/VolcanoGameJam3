using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>Écran « Vos fils sont coupés » avec le bilan de la run.</summary>
    public class GameOverView : UIPanel
    {
        public TMP_Text title;
        public TMP_Text summary;
        public Button retryButton;
        public Button menuButton;

        protected override void Awake()
        {
            base.Awake();
            if (retryButton != null) retryButton.onClick.AddListener(() => GameManager.Instance.RetryRun());
            if (menuButton != null) menuButton.onClick.AddListener(() => GameManager.Instance.ReturnToMenu());
        }

        protected override void OnOpened()
        {
            var run = GameManager.Instance.Run;
            if (title != null) title.text = "Vos fils sont coupés";
            if (summary != null && run != null)
            {
                int minutes = Mathf.FloorToInt(run.playTime / 60f), seconds = Mathf.FloorToInt(run.playTime % 60f);
                string fallen = run.fallenPuppets.Count > 0 ? string.Join(", ", run.fallenPuppets) + ", " + run.puppetName : run.puppetName;
                summary.text =
                    $"<color=#C9B79C>Donjons traversés</color><pos=60%>{run.dungeonIndex}\n" +
                    $"<color=#C9B79C>Salles nettoyées</color><pos=60%>{run.roomsCleared}\n" +
                    $"<color=#C9B79C>Pantins vaincus</color><pos=60%>{run.enemiesKilled}\n" +
                    $"<color=#C9B79C>Géants abattus</color><pos=60%>{run.bossesKilled}\n" +
                    $"<color=#C9B79C>Temps</color><pos=60%>{minutes:00}:{seconds:00}\n\n" +
                    $"<i><color=#A89A86>Lignée : {fallen}</color></i>";
            }
            GameManager.Instance.SetPaused(true);
        }
    }
}
