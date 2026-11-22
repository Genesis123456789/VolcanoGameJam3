using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Logique d'une salle : zone de sûreté, alerte (portes qui se ferment), ennemis, nettoyage.
    /// </summary>
    public class RoomController : MonoBehaviour
    {
        public int index;
        public RoomKind kind;
        public Vector2Int size;
        public List<Door> doors = new List<Door>();
        public Transform propsRoot;
        public Light[] lanterns;
        public ExitStairs exit;

        public bool Cleared { get; private set; }
        public bool Alerted { get; private set; }
        public bool Visited { get; private set; }
        public Vector3 SafeCenter { get; private set; }
        public float SafeRadius { get; private set; }
        public Bounds WorldBounds { get; private set; }
        public IReadOnlyList<EnemyController> Enemies => enemies;
        public int AliveEnemies => enemies.Count(e => e != null && !e.Health.IsDead);

        private readonly List<EnemyController> enemies = new List<EnemyController>();
        private GameObject safeRing;
        private bool spawned;

        public void Setup(int idx, RoomKind k, Vector2Int s)
        {
            index = idx;
            kind = k;
            size = s;
            var center = transform.position + new Vector3(s.x * 0.5f, 0.8f, s.y * 0.5f);
            WorldBounds = new Bounds(center, new Vector3(s.x, 2.4f, s.y));
        }

        public Vector3 Center => transform.position + new Vector3(size.x * 0.5f, 0f, size.y * 0.5f);

        public bool Contains(Vector3 p, float margin = 0f)
        {
            Vector3 o = transform.position;
            return p.x >= o.x + margin && p.x <= o.x + size.x - margin && p.z >= o.z + margin && p.z <= o.z + size.y - margin;
        }

        // ------------------------------------------------------------------ Entrée

        public void OnPlayerEnter(Door entry)
        {
            Visited = true;
            SafeCenter = entry != null ? entry.ArrivalPoint : Center;
            SafeRadius = GameManager.Instance.Config.safeZoneRadius;
            if (PlayerController.Instance != null) PlayerController.Instance.OnRoomEntered();

            if (!spawned)
            {
                spawned = true;
                SpawnEnemies();
            }

            if (safeRing != null) Destroy(safeRing);
            if (!Cleared && AliveEnemies > 0)
            {
                safeRing = Juice.SpawnObject(v => v.safeZoneRing, new Vector3(SafeCenter.x, 0.02f, SafeCenter.z), Quaternion.identity, transform);
                if (safeRing != null) safeRing.transform.localScale = new Vector3(SafeRadius * 2f, 1f, SafeRadius * 2f);
                if (UIRoot.Instance != null)
                    UIRoot.Instance.Banner(kind == RoomKind.Boss || HasBoss ? "Une présence immense sommeille…" : "Ils ne vous ont pas encore vu…", new Color(0.85f, 0.8f, 0.7f));
            }
            else if (!Cleared) Clear(false);

            if (kind == RoomKind.Exit && UIRoot.Instance != null)
                UIRoot.Instance.Banner(Cleared ? $"La trappe des profondeurs ! [{InputHints.Interact}] pour descendre" : "La trappe des profondeurs est gardée…", new Color(1f, 0.85f, 0.5f));

            var dm = DungeonManager.Instance;
            if (AudioManager.Instance != null && dm != null && dm.Lib != null)
                AudioManager.Instance.PlayMusic(dm.Lib.musicDungeon);
        }

        private bool HasBoss => enemies.Any(e => e != null && e.IsBoss);

        private void SpawnEnemies()
        {
            var dm = DungeonManager.Instance;
            var cfg = GameManager.Instance.Config;
            int dungeon = GameManager.Instance.Run.dungeonIndex;
            var defs = new List<EnemyDefinition>();

            bool boss = kind == RoomKind.Boss || (kind == RoomKind.Exit && Random.value < cfg.ExitGuardianChance(dungeon));
            if (kind == RoomKind.Start) return;
            if (kind == RoomKind.Exit && !boss) return;

            if (boss)
            {
                var b = PickWeighted(cfg.bosses, dungeon);
                if (b != null) defs.Add(b);
                int adds = Mathf.Min(2, dungeon / 3);
                for (int i = 0; i < adds; i++) { var e = PickWeighted(cfg.enemies, dungeon); if (e != null) defs.Add(e); }
            }
            else
            {
                int count = cfg.EnemiesForRoom(dungeon);
                count = Mathf.Min(count, Mathf.Max(1, (size.x - 4) * (size.y - 4) / 10));
                for (int i = 0; i < count; i++) { var e = PickWeighted(cfg.enemies, dungeon); if (e != null) defs.Add(e); }
            }
            if (defs.Count == 0) return;

            // Cases libres, loin des portes et de la zone d'entrée
            var free = new List<Vector3>();
            var keepAway = doors.Select(d => d.ArrivalPoint).ToList();
            keepAway.Add(SafeCenter);
            for (int x = 2; x < size.x - 2; x++)
                for (int z = 2; z < size.y - 2; z++)
                {
                    Vector3 p = transform.position + new Vector3(x + 0.5f, 0f, z + 0.5f);
                    if (keepAway.Any(k => Vector3.Distance(new Vector3(k.x, 0f, k.z), p) < SafeRadius + 2.2f)) continue;
                    if (Physics.CheckSphere(p + Vector3.up * 0.6f, 0.45f, ~(Layers.PlayerMask), QueryTriggerInteraction.Ignore)) continue;
                    free.Add(p);
                }
            if (free.Count == 0) free.Add(Center);

            // Boss au centre, les autres au hasard
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def.prefab == null) continue;
                Vector3 pos;
                if (def.isBoss) pos = free.OrderBy(p => Vector3.Distance(p, Center)).First();
                else
                {
                    int k = Random.Range(0, free.Count);
                    pos = free[k];
                    if (free.Count > 1) free.RemoveAt(k);
                }
                var e = Instantiate(def.prefab, pos, Quaternion.LookRotation(SafeCenter - pos == Vector3.zero ? Vector3.back : (new Vector3(SafeCenter.x, 0f, SafeCenter.z) - pos).normalized), transform);
                e.Init(def, dungeon, cfg);
                e.Room = this;
                e.Health.Died += _ => OnEnemyDied(e);
                enemies.Add(e);
            }
        }

        private static EnemyDefinition PickWeighted(List<EnemyDefinition> list, int dungeon)
        {
            if (list == null) return null;
            var pool = list.Where(e => e != null && e.minDungeon <= dungeon && e.spawnWeight > 0f).ToList();
            if (pool.Count == 0) pool = list.Where(e => e != null).ToList();
            if (pool.Count == 0) return null;
            float total = pool.Sum(e => e.spawnWeight);
            float r = Random.value * total;
            foreach (var e in pool) { if (r <= e.spawnWeight) return e; r -= e.spawnWeight; }
            return pool[pool.Count - 1];
        }

        // ------------------------------------------------------------------ Boucle

        private void Update()
        {
            if (Cleared || Alerted || !Visited) return;
            var player = PlayerController.Instance;
            var dm = DungeonManager.Instance;
            if (player == null || dm == null || dm.CurrentRoom != this || dm.IsTraveling) return;
            Vector3 p = player.transform.position; p.y = 0f;
            Vector3 c = SafeCenter; c.y = 0f;
            if (Vector3.Distance(p, c) > SafeRadius) TriggerAlert();
        }

        /// <summary>Les ennemis remarquent le joueur : les portes se ferment.</summary>
        public void TriggerAlert()
        {
            if (Alerted || Cleared) return;
            Alerted = true;
            if (safeRing != null) { Destroy(safeRing); safeRing = null; }
            foreach (var d in doors) d.SetLocked(true);
            float delay = 0f;
            foreach (var e in enemies)
            {
                if (e == null) continue;
                e.Alert(delay);
                delay += 0.12f;
            }
            Juice.Shake(0.5f);
            AudioManager.Sfx(l => l.alert);
            PostFX.Pulse(PostFX.PulseKind.Alert);
            var dm = DungeonManager.Instance;
            if (AudioManager.Instance != null && dm != null && dm.Lib != null)
                AudioManager.Instance.PlayMusic(HasBoss && dm.Lib.musicBoss != null ? dm.Lib.musicBoss : (dm.Lib.musicCombat != null ? dm.Lib.musicCombat : dm.Lib.musicDungeon), 0.6f);
            if (UIRoot.Instance != null)
            {
                UIRoot.Instance.Banner(HasBoss ? "Les fils se tendent… COMBAT !" : "Repéré ! Les portes se ferment !", new Color(1f, 0.45f, 0.35f));
                var boss = enemies.FirstOrDefault(e => e != null && e.IsBoss);
                if (boss != null) UIRoot.Instance.ShowBossBar(boss);
            }
        }

        private void OnEnemyDied(EnemyController e)
        {
            if (Cleared) return;
            if (AliveEnemies == 0)
            {
                GameManager.Instance.SlowMo(0.3f, 0.45f);
                Clear(true);
            }
        }

        private void Clear(bool fought)
        {
            if (Cleared) return;
            Cleared = true;
            if (safeRing != null) { Destroy(safeRing); safeRing = null; }
            foreach (var d in doors) d.SetLocked(false);
            if (exit != null) exit.Activate();
            if (!fought) return;

            var gm = GameManager.Instance;
            gm.Run.roomsCleared++;
            AudioManager.Sfx(l => l.roomCleared);
            var player = PlayerController.Instance;
            if (player != null)
            {
                player.HealPercent(gm.PassiveTotal(PassiveKind.SecondWind));
                if (player.rig != null) player.rig.Celebrate();
            }
            if (UIRoot.Instance != null)
            {
                UIRoot.Instance.Banner("Salle nettoyée", new Color(0.65f, 1f, 0.6f));
                UIRoot.Instance.HideBossBar();
            }
            var dm = DungeonManager.Instance;
            if (AudioManager.Instance != null && dm != null && dm.Lib != null) AudioManager.Instance.PlayMusic(dm.Lib.musicDungeon, 1.5f);
            if (dm != null) dm.OnRoomCleared(this);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
