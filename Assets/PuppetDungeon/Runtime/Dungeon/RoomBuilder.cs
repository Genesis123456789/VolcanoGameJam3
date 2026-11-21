using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PuppetDungeon
{
    /// <summary>Plan d'une porte : mur + première case de l'ouverture (largeur 2).</summary>
    public struct DoorPlan
    {
        public WallSide side;
        public int offset;
        public bool isEntry;

        public DoorPlan(WallSide side, int offset, bool isEntry)
        {
            this.side = side;
            this.offset = offset;
            this.isEntry = isEntry;
        }

        /// <summary>Centre de l'ouverture, en coordonnées locales de salle (sur l'axe des murs).</summary>
        public Vector3 LocalCenter(int sx, int sz)
        {
            switch (side)
            {
                case WallSide.North: return new Vector3(offset + 1f, 0f, sz - 0.5f);
                case WallSide.South: return new Vector3(offset + 1f, 0f, 0.5f);
                case WallSide.East: return new Vector3(sx - 0.5f, 0f, offset + 1f);
                default: return new Vector3(0.5f, 0f, offset + 1f);
            }
        }

        public void Cells(int sx, int sz, out Vector2Int a, out Vector2Int b)
        {
            switch (side)
            {
                case WallSide.North: a = new Vector2Int(offset, sz - 1); b = new Vector2Int(offset + 1, sz - 1); break;
                case WallSide.South: a = new Vector2Int(offset, 0); b = new Vector2Int(offset + 1, 0); break;
                case WallSide.East: a = new Vector2Int(sx - 1, offset); b = new Vector2Int(sx - 1, offset + 1); break;
                default: a = new Vector2Int(0, offset); b = new Vector2Int(0, offset + 1); break;
            }
        }
    }

    public class RoomPlan
    {
        public int index;
        public RoomKind kind;
        public Vector3 origin;
        public int sizeX, sizeZ;
        public List<DoorPlan> doors = new List<DoorPlan>();
    }

    /// <summary>Construit la géométrie « diorama » d'une salle avec des primitives Unity.</summary>
    public static class RoomBuilder
    {
        public const float WallHeight = 2.4f;
        public const float LowWallHeight = 0.7f;

        private static Mesh cube, cylinder;

        private static Mesh Cube => cube != null ? cube : (cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
        private static Mesh Cylinder => cylinder != null ? cylinder : (cylinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx"));

        public static RoomController Build(RoomPlan plan, DungeonTheme theme, Transform parent)
        {
            var root = new GameObject($"Salle {plan.index} ({plan.kind})");
            root.transform.SetParent(parent, false);
            root.transform.position = plan.origin;
            var room = root.AddComponent<RoomController>();
            room.Setup(plan.index, plan.kind, new Vector2Int(plan.sizeX, plan.sizeZ));

            var mFloorA = Mat(theme, theme != null ? theme.floorMaterial : null, theme != null ? theme.floorA : new Color(0.55f, 0.42f, 0.3f), "floorA");
            var mFloorB = Mat(theme, theme != null ? theme.floorMaterial : null, theme != null ? theme.floorB : new Color(0.48f, 0.36f, 0.25f), "floorB");
            var mFloorC = Mat(theme, theme != null ? theme.floorMaterial : null, (theme != null ? theme.floorB : new Color(0.48f, 0.36f, 0.25f)) * 0.85f, "floorC");
            var mWall = Mat(theme, theme != null ? theme.wallMaterial : null, theme != null ? theme.wall : new Color(0.36f, 0.27f, 0.22f), "wall");
            var mTop = Mat(theme, theme != null ? theme.wallTopMaterial : null, theme != null ? theme.wallTop : new Color(0.62f, 0.5f, 0.38f), "top");
            var mProp = Mat(theme, theme != null ? theme.propMaterial : null, theme != null ? theme.accent * 0.8f : new Color(0.6f, 0.4f, 0.25f), "prop");

            var geo = new GameObject("Géométrie").transform;
            geo.SetParent(root.transform, false);

            // Cases occupées par les portes
            var gaps = new HashSet<Vector2Int>();
            foreach (var d in plan.doors)
            {
                d.Cells(plan.sizeX, plan.sizeZ, out var a, out var b);
                gaps.Add(a); gaps.Add(b);
            }

            // Sol (tuiles légèrement irrégulières : effet jouet / diorama)
            for (int x = 0; x < plan.sizeX; x++)
                for (int z = 0; z < plan.sizeZ; z++)
                {
                    bool border = x == 0 || z == 0 || x == plan.sizeX - 1 || z == plan.sizeZ - 1;
                    if (border && !gaps.Contains(new Vector2Int(x, z))) continue;
                    var m = ((x + z) & 1) == 0 ? mFloorA : mFloorB;
                    if (Random.value < 0.12f) m = mFloorC;
                    float h = 0.2f + Random.Range(0f, 0.025f);
                    Block(geo, new Vector3(x + 0.5f, -0.2f + h * 0.5f, z + 0.5f), new Vector3(0.97f, h, 0.97f), m, Cube, false);
                }
            // Collision du sol
            var floorCol = new GameObject("Sol (collision)");
            floorCol.transform.SetParent(root.transform, false);
            var fc = floorCol.AddComponent<BoxCollider>();
            fc.center = new Vector3(plan.sizeX * 0.5f, -0.5f, plan.sizeZ * 0.5f);
            fc.size = new Vector3(plan.sizeX + 2f, 1f, plan.sizeZ + 2f);

            // Murs
            for (int x = 0; x < plan.sizeX; x++)
                for (int z = 0; z < plan.sizeZ; z++)
                {
                    bool border = x == 0 || z == 0 || x == plan.sizeX - 1 || z == plan.sizeZ - 1;
                    if (!border || gaps.Contains(new Vector2Int(x, z))) continue;
                    bool south = z == 0;
                    float h = south ? LowWallHeight : WallHeight;
                    Block(geo, new Vector3(x + 0.5f, h * 0.5f, z + 0.5f), new Vector3(1f, h, 1f), mWall, Cube, true);
                    Block(geo, new Vector3(x + 0.5f, h + 0.06f, z + 0.5f), new Vector3(1.04f, 0.12f, 1.04f), mTop, Cube, true);
                }
            // Collisions des murs (segments hauts, y compris au sud)
            WallColliders(root.transform, plan, gaps);

            // Portes
            foreach (var d in plan.doors)
                room.doors.Add(BuildDoor(root.transform, plan, d, mWall, mTop, theme));

            // Décor
            room.propsRoot = new GameObject("Décor").transform;
            room.propsRoot.SetParent(root.transform, false);
            BuildProps(room, plan, mProp, mTop, theme);
            room.lanterns = BuildLanterns(room.transform, plan, theme, mTop);

            if (plan.kind == RoomKind.Exit) room.exit = BuildExit(room.transform, plan, theme);
            return room;
        }

        private static Material Mat(DungeonTheme theme, Material baseMat, Color c, string key)
        {
            return MaterialFactory.Tinted(baseMat, c, (theme != null ? theme.name : "default") + key);
        }

        public static GameObject Block(Transform parent, Vector3 localPos, Vector3 scale, Material mat, Mesh mesh, bool shadows, string name = "b")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = true;
            return go;
        }

        private static void WallColliders(Transform root, RoomPlan plan, HashSet<Vector2Int> gaps)
        {
            var parent = new GameObject("Murs (collision)").transform;
            parent.SetParent(root, false);
            // Pour chaque côté, on fusionne les cases consécutives hors ouvertures.
            void Side(System.Func<int, Vector2Int> cellAt, int length)
            {
                int start = -1;
                for (int i = 0; i <= length; i++)
                {
                    bool solid = i < length && !gaps.Contains(cellAt(i));
                    if (solid && start < 0) start = i;
                    if (!solid && start >= 0)
                    {
                        var a = cellAt(start);
                        var b = cellAt(i - 1);
                        var col = new GameObject("mur").AddComponent<BoxCollider>();
                        col.transform.SetParent(parent, false);
                        Vector3 min = new Vector3(Mathf.Min(a.x, b.x), 0f, Mathf.Min(a.y, b.y));
                        Vector3 max = new Vector3(Mathf.Max(a.x, b.x) + 1f, 3f, Mathf.Max(a.y, b.y) + 1f);
                        col.center = (min + max) * 0.5f;
                        col.size = max - min;
                        start = -1;
                    }
                }
            }
            Side(i => new Vector2Int(i, 0), plan.sizeX);
            Side(i => new Vector2Int(i, plan.sizeZ - 1), plan.sizeX);
            Side(i => new Vector2Int(0, i), plan.sizeZ);
            Side(i => new Vector2Int(plan.sizeX - 1, i), plan.sizeZ);
        }

        private static Door BuildDoor(Transform root, RoomPlan plan, DoorPlan d, Material mWall, Material mTop, DungeonTheme theme)
        {
            var go = new GameObject($"Porte {d.side}");
            go.transform.SetParent(root, false);
            go.transform.localPosition = d.LocalCenter(plan.sizeX, plan.sizeZ);
            go.transform.localRotation = Quaternion.LookRotation(EnumUtil.Inward(d.side), Vector3.up);
            var door = go.AddComponent<Door>();
            door.side = d.side;

            bool south = d.side == WallSide.South;
            float h = south ? LowWallHeight + 0.5f : WallHeight;
            var t = go.transform;
            Block(t, new Vector3(-1.15f, h * 0.5f, 0f), new Vector3(0.32f, h, 0.7f), mTop, Cube, true, "montant");
            Block(t, new Vector3(1.15f, h * 0.5f, 0f), new Vector3(0.32f, h, 0.7f), mTop, Cube, true, "montant");
            if (!south) Block(t, new Vector3(0f, h, 0f), new Vector3(2.7f, 0.35f, 0.75f), mWall, Cube, true, "linteau");

            // Barreaux (tombent quand la salle se verrouille)
            var bars = new GameObject("Barreaux").transform;
            bars.SetParent(t, false);
            var mBars = MaterialFactory.Tinted(null, new Color(0.18f, 0.15f, 0.14f), "bars");
            for (int i = 0; i < 5; i++)
            {
                float x = Mathf.Lerp(-0.85f, 0.85f, i / 4f);
                Block(bars, new Vector3(x, h * 0.5f, 0f), new Vector3(0.12f, h * 0.5f, 0.12f), mBars, Cylinder, true, "barreau");
            }
            Block(bars, new Vector3(0f, h * 0.55f, 0f), new Vector3(2f, 0.1f, 0.14f), mBars, Cube, true, "traverse");
            door.bars = bars;
            door.barsClosedY = 0f;
            door.barsOpenY = -h - 0.3f;

            // Bloqueur (actif quand verrouillée)
            var blocker = go.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0f, 1.5f, 0f);
            blocker.size = new Vector3(2.2f, 3f, 0.4f);
            door.blocker = blocker;

            // Déclencheur de passage (moitié extérieure de l'ouverture)
            var trig = new GameObject("Passage");
            trig.transform.SetParent(t, false);
            var tc = trig.AddComponent<BoxCollider>();
            tc.isTrigger = true;
            tc.center = new Vector3(0f, 1f, -0.45f);
            tc.size = new Vector3(1.9f, 2f, 0.6f);
            var relay = trig.AddComponent<DoorTriggerRelay>();
            relay.door = door;

            // Lanterne de porte
            var lightGo = new GameObject("Lumière de porte");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(0f, h + 0.3f, 0.6f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 3.5f;
            l.intensity = 1.2f;
            l.shadows = LightShadows.None;
            door.doorLight = l;
            return door;
        }

        private static void BuildProps(RoomController room, RoomPlan plan, Material mProp, Material mTop, DungeonTheme theme)
        {
            var parent = room.propsRoot;
            var doorCenters = new List<Vector3>();
            foreach (var d in plan.doors) doorCenters.Add(d.LocalCenter(plan.sizeX, plan.sizeZ));
            bool NearDoor(Vector3 p) { foreach (var c in doorCenters) if (Vector3.Distance(p, c) < 3.2f) return true; return false; }

            var mDark = MaterialFactory.Tinted(null, new Color(0.25f, 0.2f, 0.18f), "propDark");
            var mThread = MaterialFactory.Tinted(null, theme != null ? theme.accent : new Color(0.8f, 0.3f, 0.25f), "thread");

            // Le long des murs : caisses, bobines, tonneaux
            int count = (plan.sizeX + plan.sizeZ) / 4;
            for (int i = 0; i < count; i++)
            {
                Vector3 p;
                int side = Random.Range(0, 3); // pas contre le mur sud (côté caméra)
                if (side == 0) p = new Vector3(Random.Range(1, plan.sizeX - 1) + 0.5f, 0f, plan.sizeZ - 1.5f);
                else if (side == 1) p = new Vector3(1.5f, 0f, Random.Range(2, plan.sizeZ - 1) + 0.5f);
                else p = new Vector3(plan.sizeX - 1.5f, 0f, Random.Range(2, plan.sizeZ - 1) + 0.5f);
                if (NearDoor(p)) continue;
                SpawnProp(parent, p, mProp, mDark, mThread);
            }

            // Piliers dans les grandes salles
            if (plan.sizeX >= 16 && plan.sizeZ >= 11 && plan.kind != RoomKind.Exit && Random.value < 0.6f)
            {
                float px = Mathf.Round(plan.sizeX * 0.3f) + 0.5f, pz = Mathf.Round(plan.sizeZ * 0.35f) + 0.5f;
                foreach (var p in new[] { new Vector3(px, 0, pz), new Vector3(plan.sizeX - px, 0, pz), new Vector3(px, 0, plan.sizeZ - pz), new Vector3(plan.sizeX - px, 0, plan.sizeZ - pz) })
                {
                    if (NearDoor(p)) continue;
                    var pillar = Block(parent, p + Vector3.up * 1.1f, new Vector3(0.9f, 2.2f, 0.9f), mTop, Cube, true, "pilier");
                    pillar.AddComponent<BoxCollider>();
                    Block(parent, p + Vector3.up * 2.25f, new Vector3(1.15f, 0.2f, 1.15f), mDark, Cube, true, "chapiteau");
                }
            }
        }

        private static void SpawnProp(Transform parent, Vector3 p, Material mProp, Material mDark, Material mThread)
        {
            int kind = Random.Range(0, 3);
            float rot = Random.Range(-20f, 20f);
            var holder = new GameObject("objet").transform;
            holder.SetParent(parent, false);
            holder.localPosition = p;
            holder.localRotation = Quaternion.Euler(0f, rot, 0f);
            switch (kind)
            {
                case 0: // caisse
                    Block(holder, new Vector3(0, 0.4f, 0), new Vector3(0.8f, 0.8f, 0.8f), mProp, Cube, true, "caisse");
                    Block(holder, new Vector3(0, 0.4f, 0), new Vector3(0.84f, 0.12f, 0.84f), mDark, Cube, true, "cerclage");
                    if (Random.value < 0.4f) Block(holder, new Vector3(0.05f, 1.05f, 0.05f), new Vector3(0.5f, 0.5f, 0.5f), mProp, Cube, true, "caisse");
                    break;
                case 1: // bobine de fil géante
                    Block(holder, new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0.45f, 0.5f), mThread, Cylinder, true, "fil");
                    Block(holder, new Vector3(0, 0.06f, 0), new Vector3(0.8f, 0.06f, 0.8f), mProp, Cylinder, true, "flasque");
                    Block(holder, new Vector3(0, 0.94f, 0), new Vector3(0.8f, 0.06f, 0.8f), mProp, Cylinder, true, "flasque");
                    break;
                default: // tonneau
                    Block(holder, new Vector3(0, 0.45f, 0), new Vector3(0.7f, 0.45f, 0.7f), mProp, Cylinder, true, "tonneau");
                    Block(holder, new Vector3(0, 0.25f, 0), new Vector3(0.74f, 0.04f, 0.74f), mDark, Cylinder, true, "cercle");
                    Block(holder, new Vector3(0, 0.68f, 0), new Vector3(0.74f, 0.04f, 0.74f), mDark, Cylinder, true, "cercle");
                    break;
            }
            var col = holder.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.5f, 0);
            col.size = new Vector3(0.8f, 1f, 0.8f);
        }

        private static Light[] BuildLanterns(Transform root, RoomPlan plan, DungeonTheme theme, Material mTop)
        {
            var list = new List<Light>();
            Color c = theme != null ? theme.lanternColor : new Color(1f, 0.62f, 0.3f);
            var mGlow = MaterialFactory.Tinted(null, c, "lanternGlow", 3f);
            var positions = new[]
            {
                new Vector3(1.5f, 1.7f, plan.sizeZ - 1.5f),
                new Vector3(plan.sizeX - 1.5f, 1.7f, plan.sizeZ - 1.5f),
                new Vector3(1.5f, 1.2f, 1.5f),
                new Vector3(plan.sizeX - 1.5f, 1.2f, 1.5f)
            };
            foreach (var p in positions)
            {
                var go = new GameObject("Lanterne");
                go.transform.SetParent(root, false);
                go.transform.localPosition = p;
                Block(go.transform, Vector3.zero, new Vector3(0.22f, 0.3f, 0.22f), mGlow, Cube, false, "flamme");
                Block(go.transform, new Vector3(0, 0.2f, 0), new Vector3(0.3f, 0.06f, 0.3f), mTop, Cube, false, "chapeau");
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = c;
                l.range = 6.5f;
                l.intensity = 2.2f;
                l.shadows = LightShadows.None;
                go.AddComponent<LanternFlicker>();
                list.Add(l);
            }
            return list.ToArray();
        }

        private static ExitStairs BuildExit(Transform root, RoomPlan plan, DungeonTheme theme)
        {
            var go = new GameObject("Trappe de sortie");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(plan.sizeX * 0.5f, 0f, plan.sizeZ * 0.5f + 1f);
            var t = go.transform;
            Color accent = theme != null ? theme.accent : new Color(0.9f, 0.6f, 0.3f);
            var mRim = MaterialFactory.Tinted(null, accent, "exitRim", 0.6f);
            var mHole = MaterialFactory.Tinted(null, new Color(0.02f, 0.015f, 0.02f), "exitHole");
            var mBars = MaterialFactory.Tinted(null, new Color(0.18f, 0.15f, 0.14f), "bars");

            Block(t, new Vector3(0, 0.02f, 0), new Vector3(2.2f, 0.04f, 2.2f), mHole, Cube, false, "trou");
            Block(t, new Vector3(0, 0.06f, 1.15f), new Vector3(2.5f, 0.12f, 0.2f), mRim, Cube, false, "rebord");
            Block(t, new Vector3(0, 0.06f, -1.15f), new Vector3(2.5f, 0.12f, 0.2f), mRim, Cube, false, "rebord");
            Block(t, new Vector3(1.15f, 0.06f, 0), new Vector3(0.2f, 0.12f, 2.5f), mRim, Cube, false, "rebord");
            Block(t, new Vector3(-1.15f, 0.06f, 0), new Vector3(0.2f, 0.12f, 2.5f), mRim, Cube, false, "rebord");

            var locked = new GameObject("Grille").transform;
            locked.SetParent(t, false);
            for (int i = 0; i < 5; i++)
            {
                var bar = Block(locked, new Vector3(Mathf.Lerp(-0.8f, 0.8f, i / 4f), 0.1f, 0f), new Vector3(0.12f, 1f, 0.12f), mBars, Cylinder, true, "barreau");
                bar.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            var open = new GameObject("Marches").transform;
            open.SetParent(t, false);
            for (int i = 0; i < 3; i++)
                Block(open, new Vector3(0f, -0.1f - i * 0.25f, 0.6f - i * 0.45f), new Vector3(1.8f, 0.12f, 0.45f), mRim, Cube, false, "marche");

            var lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = accent;
            light.range = 5f;
            light.intensity = 2.5f;

            var inter = go.AddComponent<Interactable>();
            inter.radius = 2.2f;
            inter.prompt = "Descendre";
            var exit = go.AddComponent<ExitStairs>();
            exit.Configure(locked.gameObject, open.gameObject, light);
            return exit;
        }
    }
}
