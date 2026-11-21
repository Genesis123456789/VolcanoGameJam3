using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>
    /// Chef d'orchestre de la run : état persistant, enchaînement donjons → village, temps de jeu.
    /// Créé automatiquement au lancement (aucune scène n'a besoin de le contenir).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public const string SceneMenu = "PD_MainMenu";
        public const string SceneDungeon = "PD_Dungeon";
        public const string SceneVillage = "PD_Village";

        public static GameManager Instance { get; private set; }

        public GameConfig Config { get; private set; }
        public RunState Run { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsTransitioning { get; private set; }

        /// <summary>Déclenché quand les stats du pantin doivent être recalculées.</summary>
        public event Action StatsDirty;

        private CanvasGroup fader;
        private float hitStopUntil;
        private float slowMoUntil;
        private float slowMoScale = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[PD] GameManager");
            go.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Config = GameConfig.Load();
            if (Config == null)
            {
                Debug.LogWarning("[PuppetDungeon] PD_GameConfig introuvable dans Resources. Lancez Tools > Puppet Dungeon > Setup complet.");
                Config = ScriptableObject.CreateInstance<GameConfig>();
            }
            BuildFader();
            if (GetComponent<AudioManager>() == null) gameObject.AddComponent<AudioManager>();
            Settings.Apply();
        }

        private void Update()
        {
            if (Run != null && !IsPaused) Run.playTime += Time.unscaledDeltaTime;
            UpdateTimeScale();
        }

        // ------------------------------------------------------------------ Run

        public void EnsureRun()
        {
            if (Run == null) NewRun();
        }

        public void NewRun()
        {
            Run = new RunState(Config.inventorySize);
            Run.puppetName = RandomPuppetName();
            Run.woodTint = RandomWoodTint();
            foreach (var def in Config.starterParts)
            {
                if (def == null) continue;
                var inst = LootGenerator.Create(Config, def, def.minRarity, 1);
                Run.inventory.SetEquipped(def.slot, inst);
            }
            Run.inventory.Changed += OnInventoryChanged;
        }

        private void OnInventoryChanged() => StatsDirty?.Invoke();

        public void MarkStatsDirty() => StatsDirty?.Invoke();

        public string RandomPuppetName()
        {
            var names = Config.puppetNames;
            if (names == null || names.Length == 0) return "Pantin";
            return names[UnityEngine.Random.Range(0, names.Length)];
        }

        public static Color RandomWoodTint()
        {
            float h = UnityEngine.Random.Range(0.06f, 0.10f);
            float s = UnityEngine.Random.Range(0.35f, 0.55f);
            float v = UnityEngine.Random.Range(0.7f, 0.92f);
            return Color.HSVToRGB(h, s, v);
        }

        /// <summary>Stats finales : base du pantin (x croissance) + toutes les pièces équipées + passifs.</summary>
        public StatBlock ComputePlayerStats()
        {
            EnsureRun();
            var baseBlock = Config.playerBase.ToBlock(Run.baseScale);
            var mods = Run.inventory.AllEquipped().SelectMany(p => p.stats).ToList();
            var stats = StatBlock.Compute(baseBlock, mods);
            stats[StatType.LifeSteal] = Mathf.Clamp(stats[StatType.LifeSteal] + PassiveTotal(PassiveKind.Sap), 0f, 0.5f);
            return stats;
        }

        public float PassiveTotal(PassiveKind kind)
        {
            if (Run == null) return 0f;
            float t = 0f;
            foreach (var p in Run.inventory.AllEquipped())
                foreach (var r in p.passives)
                    if (r.kind == kind) t += r.magnitude;
            return t;
        }

        public AttackStyle CurrentAttackStyle()
        {
            if (Run == null) return AttackStyle.Fist;
            var arm = Run.inventory.GetEquipped(PuppetSlot.RightArm);
            return arm != null ? arm.AttackStyle : AttackStyle.Fist;
        }

        // ------------------------------------------------------------------ Flux de scènes

        public void StartNewRunFromMenu()
        {
            NewRun();
            LoadScene(SceneDungeon);
        }

        public void ReturnToMenu()
        {
            SetPaused(false);
            Run = null;
            LoadScene(SceneMenu);
        }

        public void RetryRun()
        {
            SetPaused(false);
            NewRun();
            LoadScene(SceneDungeon);
        }

        /// <summary>Appelé par l'escalier de sortie d'un donjon.</summary>
        public void CompleteDungeon()
        {
            if (IsTransitioning) return;
            Run.dungeonIndex++;
            bool village = Run.dungeonIndex % Mathf.Max(1, Config.dungeonsPerVillage) == 0;
            LoadScene(village ? SceneVillage : SceneDungeon);
        }

        /// <summary>Renaissance au village : le nouveau pantin reçoit les pièces offertes.</summary>
        public void Rebirth(List<PartInstance> offered, string newName, Color newTint)
        {
            Run.fallenPuppets.Add(Run.puppetName);
            var inv = Run.inventory;
            var keepBag = Config.rebirthKeepsInventory ? inv.bag.Where(p => p != null && !offered.Contains(p)).ToList() : new List<PartInstance>();
            inv.Clear();
            foreach (var p in offered)
            {
                if (inv.GetEquipped(p.Slot) == null) inv.SetEquipped(p.Slot, p);
                else inv.TryAdd(p);
            }
            foreach (var p in keepBag) inv.TryAdd(p);
            Run.generation++;
            Run.puppetName = newName;
            Run.woodTint = newTint;
            Run.baseScale *= Config.rebirthBaseGrowth;
            Run.currentHealth = -1f;
            MarkStatsDirty();
        }

        public void LeaveVillage() => LoadScene(SceneDungeon);

        public void LoadScene(string scene)
        {
            if (IsTransitioning) return;
            StartCoroutine(LoadRoutine(scene));
        }

        private IEnumerator LoadRoutine(string scene)
        {
            IsTransitioning = true;
            yield return Fade(1f, 0.45f);
            SetPaused(false);
            hitStopUntil = 0f;
            slowMoUntil = 0f;
            var op = SceneManager.LoadSceneAsync(scene);
            if (op == null)
            {
                Debug.LogError($"[PuppetDungeon] La scène '{scene}' n'est pas dans les Build Settings. Relancez le setup.");
                IsTransitioning = false;
                yield return Fade(0f, 0.3f);
                yield break;
            }
            while (!op.isDone) yield return null;
            yield return null;
            IsTransitioning = false;
            yield return Fade(0f, 0.6f);
        }

        // ------------------------------------------------------------------ Fondu

        private void BuildFader()
        {
            var canvasGo = new GameObject("[PD] Fader", typeof(Canvas), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            fader = canvasGo.GetComponent<CanvasGroup>();
            fader.alpha = 0f;
            fader.blocksRaycasts = false;
            var img = new GameObject("Black", typeof(RectTransform), typeof(Image));
            img.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.GetComponent<Image>().color = Color.black;
        }

        public IEnumerator Fade(float target, float duration)
        {
            if (fader == null) yield break;
            float start = fader.alpha;
            fader.blocksRaycasts = target > 0.5f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                fader.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            fader.alpha = target;
            fader.blocksRaycasts = target > 0.5f;
        }

        // ------------------------------------------------------------------ Temps (pause, hit-stop, ralenti)

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            UpdateTimeScale();
        }

        /// <summary>Gel très court de l'image pour donner du poids à un coup.</summary>
        public void HitStop(float duration)
        {
            if (!Settings.HitStop) return;
            hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + duration);
            UpdateTimeScale();
        }

        /// <summary>Ralenti (ex : dernier ennemi d'une salle).</summary>
        public void SlowMo(float scale, float duration)
        {
            slowMoScale = scale;
            slowMoUntil = Mathf.Max(slowMoUntil, Time.unscaledTime + duration);
            UpdateTimeScale();
        }

        private void UpdateTimeScale()
        {
            float now = Time.unscaledTime;
            float scale = 1f;
            if (IsPaused) scale = 0f;
            else if (now < hitStopUntil) scale = 0.02f;
            else if (now < slowMoUntil) scale = slowMoScale;
            Time.timeScale = scale;
        }
    }
}
