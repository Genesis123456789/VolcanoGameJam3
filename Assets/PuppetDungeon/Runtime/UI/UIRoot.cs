using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>
    /// Racine de l'interface en jeu (prefab PD_UIRoot généré par l'outil) : HUD, inventaire, pause,
    /// game over, bannières, notifications, barre de boss, carte de titre.
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; }

        [Header("Écrans")]
        public Canvas canvas;
        public HUDView hud;
        public InventoryView inventory;
        public PauseMenuView pause;
        public GameOverView gameOver;
        public TooltipView tooltip;
        public DragGhost dragGhost;
        public OfferingView offering;
        public DialogView dialog;
        public GachaView gacha;

        [Header("Messages")]
        public CanvasGroup bannerGroup;
        public TMP_Text bannerText;
        public RectTransform toastContainer;
        public ToastView toastPrefab;
        public CanvasGroup titleGroup;
        public TMP_Text titleTop;
        public TMP_Text titleMain;
        public TMP_Text titleSub;

        [Header("Boss")]
        public CanvasGroup bossGroup;
        public Image bossFill;
        public Image bossFillLag;
        public TMP_Text bossName;

        public bool InventoryOpen => inventory != null && inventory.IsOpen;
        public bool AnyMenuOpen => InventoryOpen || (pause != null && pause.IsOpen) || (gameOver != null && gameOver.IsOpen) || ModalOpen;
        public bool ModalOpen { get; set; }

        private Coroutine bannerRoutine;
        private EnemyController boss;
        private float bossLag = 1f;

        private void Awake()
        {
            Instance = this;
            ModalOpen = false;
            boss = null;
            if (bannerGroup != null) bannerGroup.alpha = 0f;
            if (titleGroup != null) titleGroup.alpha = 0f;
            if (bossGroup != null) bossGroup.alpha = 0f;
            EnsureEventSystem();
            NoEllipsis(transform);
        }

        /// <summary>La police n'a pas le caractère « … » : on tronque simplement (évite un avertissement par texte).</summary>
        public static void NoEllipsis(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
                if (t.overflowMode == TMPro.TextOverflowModes.Ellipsis) t.overflowMode = TMPro.TextOverflowModes.Truncate;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current == null && FindAnyObjectByType<EventSystem>() == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                go.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
            }
            GamepadUI.Ensure();
        }

        private void Update()
        {
            if (boss != null && bossGroup != null && bossGroup.alpha > 0f)
            {
                float n = boss.Health != null ? boss.Health.Normalized : 0f;
                if (bossFill != null) bossFill.fillAmount = n;
                bossLag = Mathf.MoveTowards(bossLag, n, Time.unscaledDeltaTime * 0.35f);
                if (bossFillLag != null) bossFillLag.fillAmount = Mathf.Max(bossLag, n);
                if (boss.Health.IsDead) HideBossBar();
            }
        }

        // ------------------------------------------------------------------ Entrées

        public void OnPausePressed()
        {
            if (gameOver != null && gameOver.IsOpen) return;
            if (InventoryOpen) { inventory.Close(); return; }
            if (gacha != null && gacha.IsOpen) { gacha.Close(); return; }
            if (ModalOpen) return;
            if (pause != null) pause.Toggle();
        }

        public void OnInventoryPressed()
        {
            if (inventory == null || (pause != null && pause.IsOpen) || ModalOpen) return;
            if (gameOver != null && gameOver.IsOpen) return;
            if (DungeonManager.Instance != null && DungeonManager.Instance.IsTraveling) return;
            inventory.Toggle();
        }

        // ------------------------------------------------------------------ Messages

        public void Banner(string text, Color color)
        {
            if (bannerText == null || bannerGroup == null) return;
            bannerText.text = text;
            bannerText.color = color;
            if (bannerRoutine != null) StopCoroutine(bannerRoutine);
            bannerRoutine = StartCoroutine(BannerRoutine());
        }

        private IEnumerator BannerRoutine()
        {
            var rt = (RectTransform)bannerGroup.transform;
            for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                bannerGroup.alpha = t / 0.25f;
                rt.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, t / 0.25f);
                yield return null;
            }
            bannerGroup.alpha = 1f;
            rt.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(1.8f);
            for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                bannerGroup.alpha = 1f - t / 0.5f;
                yield return null;
            }
            bannerGroup.alpha = 0f;
        }

        public void Toast(string text, Color color)
        {
            if (toastPrefab == null || toastContainer == null) return;
            var t = Instantiate(toastPrefab, toastContainer);
            t.Show(text, color);
        }

        public void TitleCard(string top, string main, string sub)
        {
            if (titleGroup == null) return;
            if (titleTop != null) titleTop.text = top;
            if (titleMain != null) titleMain.text = main;
            if (titleSub != null) titleSub.text = sub;
            StartCoroutine(TitleRoutine());
        }

        private IEnumerator TitleRoutine()
        {
            for (float t = 0; t < 0.8f; t += Time.unscaledDeltaTime)
            {
                titleGroup.alpha = t / 0.8f;
                yield return null;
            }
            titleGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(2.2f);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime)
            {
                titleGroup.alpha = 1f - t;
                yield return null;
            }
            titleGroup.alpha = 0f;
        }

        public void ShowBossBar(EnemyController e)
        {
            boss = e;
            bossLag = 1f;
            if (bossName != null)
                bossName.text = e.Definition != null && !string.IsNullOrEmpty(e.Definition.bossTitle) ? e.Definition.bossTitle : (e.Definition != null ? e.Definition.displayName : "???");
            if (bossGroup != null) StartCoroutine(FadeGroup(bossGroup, 1f, 0.4f));
        }

        public void HideBossBar()
        {
            boss = null;
            if (bossGroup != null && bossGroup.alpha > 0f) StartCoroutine(FadeGroup(bossGroup, 0f, 0.6f));
        }

        public static IEnumerator FadeGroup(CanvasGroup g, float target, float duration)
        {
            float start = g.alpha;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                g.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            g.alpha = target;
        }

        public void ShowGameOver(float delay)
        {
            if (gameOver != null) StartCoroutine(GameOverRoutine(delay));
        }

        private IEnumerator GameOverRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (inventory != null && inventory.IsOpen) inventory.Close();
            if (pause != null && pause.IsOpen) pause.Close();
            gameOver.Open();
        }
    }
}
