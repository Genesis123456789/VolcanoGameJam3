using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Décor procédural du village (maisonnettes, lampadaires, scène de théâtre, habitants).</summary>
    public static class VillageDecor
    {
        private static Mesh Cube => Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        private static Mesh Cylinder => Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
        private static Mesh Sphere => Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

        public static void Build(RoomController plaza, DungeonTheme theme)
        {
            var root = new GameObject("Village").transform;
            root.SetParent(plaza.transform, false);
            int sx = plaza.size.x, sz = plaza.size.y;
            Color accent = theme != null ? theme.accent : new Color(0.85f, 0.4f, 0.3f);

            var mWall = MaterialFactory.Tinted(null, new Color(0.86f, 0.78f, 0.64f), "vHouse");
            var mRoofA = MaterialFactory.Tinted(null, new Color(0.62f, 0.25f, 0.2f), "vRoofA");
            var mRoofB = MaterialFactory.Tinted(null, new Color(0.3f, 0.36f, 0.5f), "vRoofB");
            var mWood = MaterialFactory.Tinted(null, new Color(0.45f, 0.3f, 0.2f), "vWood");
            var mWindow = MaterialFactory.Tinted(null, new Color(1f, 0.75f, 0.4f), "vWindow", 2.5f);
            var mLeaf = MaterialFactory.Tinted(null, new Color(0.35f, 0.5f, 0.3f), "vLeaf");
            var mCurtain = MaterialFactory.Tinted(null, accent, "vCurtain");

            // Maisonnettes contre le mur nord (en laissant le centre libre pour la porte)
            float[] xs = { 3.5f, 7.5f, sx - 7.5f, sx - 3.5f };
            for (int i = 0; i < xs.Length; i++)
                House(root, new Vector3(xs[i], 0f, sz - 3f), mWall, i % 2 == 0 ? mRoofA : mRoofB, mWood, mWindow);

            // Petites maisons sur les côtés
            House(root, new Vector3(2.8f, 0f, sz * 0.45f), mWall, mRoofB, mWood, mWindow, 90f, 0.8f);
            House(root, new Vector3(sx - 2.8f, 0f, sz * 0.45f), mWall, mRoofA, mWood, mWindow, -90f, 0.8f);

            // Petit théâtre de marionnettes au centre
            var stage = new GameObject("Théâtre").transform;
            stage.SetParent(root, false);
            stage.localPosition = new Vector3(sx * 0.5f + 4.5f, 0f, sz * 0.5f + 0.5f);
            Block(stage, new Vector3(0, 0.35f, 0), new Vector3(2.6f, 0.7f, 1.4f), mWood);
            Block(stage, new Vector3(-1.2f, 1.4f, -0.5f), new Vector3(0.15f, 1.4f, 0.15f), mWood);
            Block(stage, new Vector3(1.2f, 1.4f, -0.5f), new Vector3(0.15f, 1.4f, 0.15f), mWood);
            Block(stage, new Vector3(0, 2.15f, -0.5f), new Vector3(2.6f, 0.25f, 0.2f), mCurtain);
            Block(stage, new Vector3(-0.95f, 1.4f, -0.45f), new Vector3(0.45f, 1.3f, 0.08f), mCurtain);
            Block(stage, new Vector3(0.95f, 1.4f, -0.45f), new Vector3(0.45f, 1.3f, 0.08f), mCurtain);
            stage.gameObject.AddComponent<BoxCollider>().size = new Vector3(2.6f, 2.4f, 1.4f);

            // Arbre
            var tree = new GameObject("Arbre").transform;
            tree.SetParent(root, false);
            tree.localPosition = new Vector3(sx * 0.5f - 5f, 0f, sz * 0.5f + 1f);
            Block(tree, new Vector3(0, 0.9f, 0), new Vector3(0.45f, 0.9f, 0.45f), mWood, Cylinder);
            Block(tree, new Vector3(0, 2.3f, 0), new Vector3(2.2f, 1.7f, 2.2f), mLeaf, Sphere);
            Block(tree, new Vector3(0.5f, 2.9f, 0.3f), new Vector3(1.4f, 1.2f, 1.4f), mLeaf, Sphere);
            var tc = tree.gameObject.AddComponent<CapsuleCollider>();
            tc.radius = 0.35f; tc.height = 2f; tc.center = new Vector3(0, 1f, 0);

            // Lampadaires
            foreach (var p in new[] { new Vector3(sx * 0.5f - 2.5f, 0f, 3f), new Vector3(sx * 0.5f + 2.5f, 0f, 3f), new Vector3(sx * 0.5f - 2.5f, 0f, sz - 5f), new Vector3(sx * 0.5f + 2.5f, 0f, sz - 5f) })
            {
                var lamp = new GameObject("Lampadaire").transform;
                lamp.SetParent(root, false);
                lamp.localPosition = p;
                Block(lamp, new Vector3(0, 1f, 0), new Vector3(0.12f, 1f, 0.12f), mWood, Cylinder);
                Block(lamp, new Vector3(0, 2.05f, 0), new Vector3(0.3f, 0.3f, 0.3f), mWindow);
                var lg = new GameObject("Lumière");
                lg.transform.SetParent(lamp, false);
                lg.transform.localPosition = new Vector3(0, 2.1f, 0);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.7f, 0.4f);
                l.range = 6f;
                l.intensity = 2.2f;
                lg.AddComponent<LanternFlicker>();
                lamp.gameObject.AddComponent<CapsuleCollider>().radius = 0.2f;
            }

            // Habitants qui se balancent
            var cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (cfg != null && cfg.villagerPrefab != null)
            {
                Vector3[] spots = { new Vector3(sx * 0.5f + 4.5f, 0f, sz * 0.5f - 1.4f), new Vector3(3.5f, 0f, sz - 5f), new Vector3(sx - 7.5f, 0f, sz - 5f) };
                foreach (var s in spots)
                {
                    var npcGo = Object.Instantiate(cfg.villagerPrefab, plaza.transform.position + s, Quaternion.Euler(0f, Random.Range(150f, 210f), 0f), root);
                    var rig = npcGo.GetComponentInChildren<PuppetRig>();
                    if (rig != null) { rig.ApplyLoadout(null, cfg); rig.SetWoodTint(GameManager.RandomWoodTint()); }
                }
            }
        }

        private static void House(Transform root, Vector3 pos, Material wall, Material roof, Material wood, Material window, float yaw = 0f, float scale = 1f)
        {
            var h = new GameObject("Maisonnette").transform;
            h.SetParent(root, false);
            h.localPosition = pos;
            h.localRotation = Quaternion.Euler(0f, yaw, 0f);
            h.localScale = Vector3.one * scale;
            Block(h, new Vector3(0, 1f, 0), new Vector3(2.6f, 2f, 2f), wall);
            var r1 = Block(h, new Vector3(0, 2.35f, 0), new Vector3(2.9f, 1.15f, 1.15f), roof);
            r1.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            Block(h, new Vector3(0, 0.6f, -1.01f), new Vector3(0.7f, 1.2f, 0.06f), wood);
            Block(h, new Vector3(-0.85f, 1.2f, -1.01f), new Vector3(0.45f, 0.45f, 0.06f), window);
            Block(h, new Vector3(0.85f, 1.2f, -1.01f), new Vector3(0.45f, 0.45f, 0.06f), window);
            var col = h.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.2f, 0);
            col.size = new Vector3(2.6f, 2.4f, 2f);
        }

        private static GameObject Block(Transform parent, Vector3 pos, Vector3 scale, Material mat, Mesh mesh = null)
        {
            return RoomBuilder.Block(parent, pos, scale, mat, mesh != null ? mesh : Cube, true, "décor");
        }

        /// <summary>Porte du nord : mène aux donjons suivants.</summary>
        public static Interactable BuildGate(RoomController plaza, DungeonTheme theme)
        {
            int sx = plaza.size.x, sz = plaza.size.y;
            var gate = new GameObject("Porte des donjons").transform;
            gate.SetParent(plaza.transform, false);
            gate.localPosition = new Vector3(sx * 0.5f, 0f, sz - 1.6f);
            var mWood = MaterialFactory.Tinted(null, new Color(0.35f, 0.24f, 0.17f), "vGate");
            var mGlow = MaterialFactory.Tinted(null, theme != null ? theme.accent : new Color(1f, 0.6f, 0.3f), "vGateGlow", 1.5f);
            Block(gate, new Vector3(-1.3f, 1.3f, 0f), new Vector3(0.4f, 2.6f, 0.4f), mWood);
            Block(gate, new Vector3(1.3f, 1.3f, 0f), new Vector3(0.4f, 2.6f, 0.4f), mWood);
            Block(gate, new Vector3(0f, 2.7f, 0f), new Vector3(3.2f, 0.4f, 0.5f), mWood);
            Block(gate, new Vector3(0f, 1.2f, 0.25f), new Vector3(2.2f, 2.4f, 0.05f), mGlow);
            var lg = new GameObject("Lueur");
            lg.transform.SetParent(gate, false);
            lg.transform.localPosition = new Vector3(0f, 1.3f, -0.6f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = theme != null ? theme.accent : new Color(1f, 0.6f, 0.3f);
            l.range = 5f;
            l.intensity = 2f;
            var inter = gate.gameObject.AddComponent<Interactable>();
            inter.prompt = "Repartir vers les donjons";
            inter.radius = 2.4f;
            return inter;
        }
    }
}
