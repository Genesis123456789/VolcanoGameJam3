using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Génération procédurale « à la demande » (repris du prototype ElGenerator, corrigé) :
    /// chaque porte non liée génère la salle suivante, collée porte contre porte. La salle d'arrivée
    /// reçoit une porte d'entrée derrière le joueur.
    /// Le type de chaque nouvelle salle est tiré selon le nombre de salles déjà parcourues
    /// (courbes « exitChanceByRooms » et « bossChanceByRooms » de PD_GameConfig) : plus on explore,
    /// plus la sortie est probable ; les boss sont rares en début de donjon.
    /// Gère aussi la transition caméra « à la Zelda » entre deux salles.
    /// </summary>
    public class DungeonManager : MonoBehaviour
    {
        public static DungeonManager Instance { get; private set; }

        [Tooltip("Lumière directionnelle principale (teintée par le thème).")]
        public Light sun;
        public Transform playerSpawn;

        public RoomController CurrentRoom { get; private set; }
        public bool IsTraveling { get; private set; }
        public int RoomsGenerated => rooms.Count;
        public bool ExitSpawned { get; private set; }
        /// <summary>Salles parcourues dans ce donjon (salle de départ exclue).</summary>
        public int RoomsVisited => Mathf.Max(0, rooms.Count - 1);
        public int BossRoomsSpawned { get; private set; }
        public DungeonTheme Theme { get; private set; }
        public AudioLibrary Lib => GameManager.Instance.Config.audio;

        private readonly List<RoomController> rooms = new List<RoomController>();
        private Transform roomsRoot;
        private GameConfig cfg;
        private int dungeonIndex;
        private int roomsSinceExit;

        private void Awake()
        {
            Instance = this;
            rooms.Clear();
            IsTraveling = false;
            ExitSpawned = false;
            BossRoomsSpawned = 0;
            roomsSinceExit = 0;
            CurrentRoom = null;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            MaterialFactory.ClearCache();
        }

        private IEnumerator Start()
        {
            var gm = GameManager.Instance;
            gm.EnsureRun();
            cfg = gm.Config;
            dungeonIndex = gm.Run.dungeonIndex;
            Theme = cfg.ThemeFor(dungeonIndex);
            ApplyTheme();

            roomsRoot = new GameObject("Salles").transform;
            var start = Generate(null);
            CurrentRoom = start;

            // Joueur
            var player = PlayerController.Instance != null ? PlayerController.Instance : FindAnyObjectByType<PlayerController>();
            if (player == null && cfg.playerPrefab != null)
                player = Instantiate(cfg.playerPrefab).GetComponent<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[PuppetDungeon] Aucun prefab joueur dans PD_GameConfig. Relancez le setup.");
                yield break;
            }
            Vector3 spawn = start.Center + Vector3.up * 0.1f;
            player.Teleport(spawn);
            player.FaceTowards(Vector3.back);

            if (RoomCamera.Instance != null) RoomCamera.Instance.SnapTo(start);
            start.OnPlayerEnter(null);

            yield return null;
            if (UIRoot.Instance != null)
                UIRoot.Instance.TitleCard($"Donjon {dungeonIndex + 1}", cfg.DungeonName(dungeonIndex),
                    VillageCountdownText());
            if (AudioManager.Instance != null && Lib != null) AudioManager.Instance.PlayMusic(Lib.musicDungeon);
        }

        public string VillageCountdownText()
        {
            int per = Mathf.Max(1, cfg.dungeonsPerVillage);
            int left = per - (dungeonIndex % per);
            return left <= 1 ? "Le village des marionnettes est proche…" : $"Village des marionnettes dans {left} donjons";
        }

        private void ApplyTheme()
        {
            if (Theme == null) return;
            if (sun != null)
            {
                sun.color = Theme.lightColor;
                sun.intensity = Theme.lightIntensity;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Theme.ambient;
        }

        // ------------------------------------------------------------------ Génération

        /// <summary>Génère une salle. fromDoor == null : salle de départ.</summary>
        public RoomController Generate(Door fromDoor)
        {
            var plan = new RoomPlan { index = rooms.Count };
            plan.kind = fromDoor == null ? RoomKind.Start : PickRoomKind();

            bool placed = false;
            WallSide? entrySide = fromDoor != null ? EnumUtil.Opposite(fromDoor.side) : (WallSide?)null;

            for (int attempt = 0; attempt < 20 && !placed; attempt++)
            {
                float shrink = attempt / 20f;
                plan.sizeX = Odd(Mathf.RoundToInt(Mathf.Lerp(Random.Range(cfg.roomSizeX.x, cfg.roomSizeX.y + 1), cfg.roomSizeX.x - 2, shrink)));
                plan.sizeZ = Odd(Mathf.RoundToInt(Mathf.Lerp(Random.Range(cfg.roomSizeZ.x, cfg.roomSizeZ.y + 1), cfg.roomSizeZ.x - 2, shrink)));
                if (plan.kind == RoomKind.Boss) { plan.sizeX = Mathf.Max(plan.sizeX, cfg.roomSizeX.y); plan.sizeZ = Mathf.Max(plan.sizeZ, cfg.roomSizeZ.y); }
                if (plan.kind == RoomKind.Start) { plan.sizeX = cfg.roomSizeX.x; plan.sizeZ = cfg.roomSizeZ.x; }

                int extra = plan.kind == RoomKind.Exit ? 0 : (plan.kind == RoomKind.Start ? Random.Range(2, 4) : Random.Range(1, 3));
                plan.doors = ChooseDoors(plan.sizeX, plan.sizeZ, extra, entrySide);

                if (fromDoor == null)
                {
                    plan.origin = Vector3.zero;
                    placed = true;
                    break;
                }
                var entry = plan.doors[0];
                Vector3 entryLocal = entry.LocalCenter(plan.sizeX, plan.sizeZ);
                Vector3 outward = -EnumUtil.Inward(fromDoor.side);
                plan.origin = fromDoor.Center + outward * 1f - entryLocal;
                plan.origin.y = 0f;
                placed = !Overlaps(plan.origin, plan.sizeX, plan.sizeZ);
            }

            if (!placed)
            {
                // Plus de place : salle posée à l'écart (transition par fondu).
                float far = rooms.Max(r => r.transform.position.x + r.size.x);
                plan.origin = new Vector3(far + 12f, 0f, 0f);
            }

            var room = RoomBuilder.Build(plan, Theme, roomsRoot);
            rooms.Add(room);
            if (plan.kind == RoomKind.Exit)
            {
                ExitSpawned = true;
                roomsSinceExit = 0;
                Debug.Log($"[PuppetDungeon] Sortie générée (salle n°{rooms.Count}, {RoomsVisited} salles parcourues).");
            }
            else if (ExitSpawned && fromDoor != null) roomsSinceExit++;
            if (plan.kind == RoomKind.Boss) BossRoomsSpawned++;

            if (fromDoor != null)
            {
                var entryDoor = room.doors[0];
                entryDoor.linkedDoor = fromDoor;
                fromDoor.linkedDoor = entryDoor;
            }
            foreach (var d in room.doors) d.room = room;
            room.SetVisible(fromDoor == null);
            return room;
        }

        /// <summary>
        /// Type de la prochaine nouvelle salle. La sortie peut réapparaître si la précédente a été
        /// laissée derrière depuis « roomsBeforeAnotherExit » salles : on ne peut plus se perdre sans fin.
        /// </summary>
        private RoomKind PickRoomKind()
        {
            int visited = RoomsVisited;
            bool exitAllowed = !ExitSpawned || roomsSinceExit >= cfg.roomsBeforeAnotherExit;
            if (exitAllowed && Random.value < cfg.ExitChance(visited, dungeonIndex)) return RoomKind.Exit;
            if (visited >= 1 && BossRoomsSpawned < cfg.maxBossRoomsPerDungeon && Random.value < cfg.BossChance(visited, dungeonIndex)) return RoomKind.Boss;
            return RoomKind.Normal;
        }

        private static int Odd(int v) => (v % 2 == 0) ? v + 1 : v;

        private bool Overlaps(Vector3 origin, int sx, int sz)
        {
            foreach (var r in rooms)
            {
                Vector3 p = r.transform.position;
                bool separated = origin.x + sx <= p.x + 0.01f || p.x + r.size.x <= origin.x + 0.01f
                              || origin.z + sz <= p.z + 0.01f || p.z + r.size.y <= origin.z + 0.01f;
                if (!separated) return true;
            }
            return false;
        }

        private static List<DoorPlan> ChooseDoors(int sx, int sz, int extraCount, WallSide? entrySide)
        {
            var result = new List<DoorPlan>();
            var candidates = new List<DoorPlan>();
            foreach (WallSide side in System.Enum.GetValues(typeof(WallSide)))
            {
                int len = side == WallSide.North || side == WallSide.South ? sx : sz;
                for (int a = 2; a <= len - 4; a++) candidates.Add(new DoorPlan(side, a, false));
            }

            if (entrySide.HasValue)
            {
                var onSide = candidates.Where(c => c.side == entrySide.Value).ToList();
                var pick = onSide[Random.Range(0, onSide.Count)];
                pick.isEntry = true;
                result.Add(pick);
            }

            // Les autres portes : de préférence sur d'autres murs que l'entrée, une par mur.
            var sides = System.Enum.GetValues(typeof(WallSide)).Cast<WallSide>()
                .Where(s => !entrySide.HasValue || s != entrySide.Value)
                .OrderBy(_ => Random.value).ToList();
            foreach (var side in sides)
            {
                if (result.Count - (entrySide.HasValue ? 1 : 0) >= extraCount) break;
                var onSide = candidates.Where(c => c.side == side).ToList();
                if (onSide.Count == 0) continue;
                // Centre du mur privilégié (plus lisible)
                onSide = onSide.OrderBy(c => Mathf.Abs(c.offset - ((side == WallSide.North || side == WallSide.South ? sx : sz) / 2 - 1)) + Random.Range(0f, 3f)).ToList();
                result.Add(onSide[0]);
            }
            return result;
        }

        // ------------------------------------------------------------------ Voyage

        public void RequestTravel(Door door)
        {
            if (IsTraveling || door == null || door.IsLocked) return;
            if (GameManager.Instance.IsTransitioning) return;
            StartCoroutine(TravelRoutine(door));
        }

        private IEnumerator TravelRoutine(Door door)
        {
            IsTraveling = true;
            var player = PlayerController.Instance;
            player.Lock(true);
            var from = door.room;

            if (!door.IsLinked) Generate(door);
            var target = door.linkedDoor;
            var to = target.room;
            to.SetVisible(true);

            bool adjacent = Vector3.Distance(door.Center, target.Center) < 2.5f;
            if (adjacent)
            {
                // Glissement de caméra + le pantin traverse la porte
                if (RoomCamera.Instance != null) RoomCamera.Instance.TransitionTo(to, 0.6f);
                yield return player.ScriptedMove(target.Center + EnumUtil.Inward(target.side) * 0.2f, 0.32f);
                yield return player.ScriptedMove(target.ArrivalPoint, 0.3f);
            }
            else
            {
                yield return GameManager.Instance.Fade(1f, 0.25f);
                player.Teleport(target.ArrivalPoint);
                player.FaceTowards(EnumUtil.Inward(target.side));
                if (RoomCamera.Instance != null) RoomCamera.Instance.SnapTo(to);
                yield return GameManager.Instance.Fade(0f, 0.3f);
            }

            if (from != null && from != to) from.SetVisible(false);
            CurrentRoom = to;
            to.OnPlayerEnter(target);
            player.Lock(false);
            IsTraveling = false;
        }

        public void OnRoomCleared(RoomController room)
        {
            // Point d'extension (statistiques, succès…)
        }

        public int RoomNumber(RoomController room) => rooms.IndexOf(room) + 1;
    }
}
