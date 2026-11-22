using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PuppetDungeon.EditorTools
{
    /// <summary>
    /// Données et prefabs de jeu : pantins (joueur, villageois, ennemis), pièces d'exemple
    /// construites à partir des FBX fournis, thèmes, bibliothèque audio, configuration.
    /// Les assets de contenu (Content/) ne sont créés que s'ils n'existent pas : vos réglages sont préservés.
    /// </summary>
    public static class PDContent
    {
        // ------------------------------------------------------------------ Pantins

        public static GameObject BuildPlayer(PuppetAnimationSet anims, PDAssets.MaterialSet m)
        {
            int layer = LayerMask.NameToLayer("Player");
            var root = new GameObject("PD_Player");
            root.tag = "Player";
            var cc = root.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, PDRigTools.PlayerHeight * 0.5f, 0f);
            cc.height = PDRigTools.PlayerHeight;
            cc.radius = 0.35f;
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.04f;
            root.AddComponent<Health>();
            root.AddComponent<HitFlash>();
            var input = root.AddComponent<PlayerInput>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(PDPaths.InputActions);
            if (actions == null) Debug.LogError("[PuppetDungeon] Actions d'entrée introuvables : " + PDPaths.InputActions);
            input.actions = actions;
            input.defaultActionMap = "Player";
            input.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            root.AddComponent<PlayerCombat>();

            var rig = PDRigTools.BuildPuppetVisual(root.transform, anims, m.wood, PDRigTools.PlayerHeight, layer);
            var pc = root.AddComponent<PlayerController>();
            pc.rig = rig;
            pc.visualRoot = root.transform.Find("Visuel");
            pc.squash = pc.visualRoot != null ? pc.visualRoot.GetComponent<SquashStretch>() : null;
            PDRigTools.SetLayer(root.transform, layer);
            return Save(root, PDPaths.Prefabs + "/PD_Player.prefab");
        }

        public static GameObject BuildVillager(PuppetAnimationSet anims, PDAssets.MaterialSet m)
        {
            var root = new GameObject("PD_Villager");
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, PDRigTools.PlayerHeight * 0.48f, 0f);
            col.height = PDRigTools.PlayerHeight * 0.95f;
            col.radius = 0.3f;
            root.AddComponent<HitFlash>();
            var rig = PDRigTools.BuildPuppetVisual(root.transform, anims, m.wood, PDRigTools.PlayerHeight * 0.95f, 0);
            var npc = root.AddComponent<VillagerNPC>();
            npc.rig = rig;
            return Save(root, PDPaths.Prefabs + "/PD_Villager.prefab");
        }

        public static EnemyController BuildEnemyPrefab(string id, PuppetAnimationSet anims, PDAssets.MaterialSet m, IEnumerable<PartVisual> decorations, float height)
        {
            int layer = LayerMask.NameToLayer("Enemy");
            var root = new GameObject("PD_Enemy_" + id);
            var cc = root.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, height * 0.5f, 0f);
            cc.height = height;
            cc.radius = 0.4f;
            cc.stepOffset = 0.3f;
            root.AddComponent<Health>();
            root.AddComponent<HitFlash>();
            var rig = PDRigTools.BuildPuppetVisual(root.transform, anims, m.woodDark, height, layer);
            if (rig != null) rig.decorations = decorations.Where(d => d != null).ToList();
            var ec = root.AddComponent<EnemyController>();
            ec.rig = rig;
            var vis = root.transform.Find("Visuel");
            ec.squash = vis != null ? vis.GetComponent<SquashStretch>() : null;
            var alert = new GameObject("Alerte").transform;
            alert.SetParent(root.transform, false);
            alert.localPosition = new Vector3(0f, height + 0.2f, 0f);
            ec.alertAnchor = alert;
            var muzzle = new GameObject("Bouche").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = new Vector3(0f, height * 0.62f, 0.6f);
            ec.muzzle = muzzle;
            PDRigTools.SetLayer(root.transform, layer);
            var saved = Save(root, PDPaths.Prefabs + "/PD_Enemy_" + id + ".prefab");
            return saved != null ? saved.GetComponent<EnemyController>() : null;
        }

        private static GameObject Save(GameObject go, string path)
        {
            PDPaths.EnsureFolder(PDPaths.Prefabs);
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        // ------------------------------------------------------------------ Pièces d'exemple (FBX fournis)

        private class PartSpec
        {
            public string id, name, model, lore;
            public PuppetSlot slot;
            public AttackStyle style = AttackStyle.Fist;
            public StatRange[] stats;
            public PassiveKind[] passives;
            public Rarity minRarity = Rarity.Common;
            public int minDungeon;
            public float weight = 1f;
        }

        private static StatRange S(StatType t, float min, float max, ModifierMode mode = ModifierMode.Flat) => new StatRange(t, mode, min, max);

        private static List<PartSpec> ExampleSpecs()
        {
            const string loreHead = "Une tête ronde, poncée par mille mains d'enfants. Derrière les yeux peints, quelqu'un a gravé un prénom que plus personne ne sait lire.";
            const string loreTorso = "Taillé dans le cœur d'un chêne foudroyé. On dit que l'orage y dort encore, et qu'il gronde quand son porteur a peur.";
            const string loreArm = "Un bras sans prétention, articulé au fil de lin. Il a porté des lanternes, des seaux d'eau, et une fois la main d'un enfant qu'il n'a jamais voulu lâcher.";
            const string loreLeg = "Une jambe de pantin de foire, usée aux genoux à force de révérences. Elle connaît encore les pas de la gigue du Grand Soir.";
            const string loreBlade = "Le duelliste Arlequin refusait de lâcher son épée ; le marionnettiste a fini par les visser l'un à l'autre. Depuis, la lame salue toute seule avant chaque combat.";
            const string loreIron = "Récupérée sur le pont d'un navire de fête foraine. Elle claque sur les dalles comme un métronome : on l'entend venir, rarement repartir.";
            const string loreCrossbow = "Les guetteurs du Théâtre Englouti voyaient les ombres avant qu'elles ne bougent. Celui-ci a greffé son arbalète à son coude pour ne plus jamais avoir à la ramasser.";
            const string loreScoutLeg = "Fine et nerveuse, montée sur un ressort de pendule. Elle permettait au guetteur de changer de perchoir entre deux battements de cœur.";
            const string loreShield = "Les Remparts portaient les décors du grand spectacle. Quand le théâtre a sombré, ils ont gardé leurs planches… et n'ont plus jamais reculé d'un pas.";
            const string loreRampartLeg = "Lourde comme un pilier de scène. Ceux qui la portent marchent plus lentement, mais personne ne les a jamais vus tomber.";

            return new List<PartSpec>
            {
                new PartSpec { id = "Tete_Pantin", name = "Tête de Pantin", model = PDPaths.PartBody, slot = PuppetSlot.Head, lore = loreHead,
                    stats = new[] { S(StatType.MaxHealth, 8, 12), S(StatType.Armor, 1, 2) }, passives = new[] { PassiveKind.SecondWind, PassiveKind.Varnish, PassiveKind.LuckyCharm } },
                new PartSpec { id = "Buste_Chene", name = "Buste de Chêne", model = PDPaths.PartBody, slot = PuppetSlot.Torso, lore = loreTorso,
                    stats = new[] { S(StatType.MaxHealth, 16, 24), S(StatType.Armor, 2, 3) }, passives = new[] { PassiveKind.Thorns, PassiveKind.Varnish, PassiveKind.SecondWind } },
                new PartSpec { id = "Bras_Pantin_D", name = "Bras de Bois Tendre", model = PDPaths.PartBody, slot = PuppetSlot.RightArm, lore = loreArm,
                    stats = new[] { S(StatType.Damage, 2, 3), S(StatType.AttackSpeed, 0.03f, 0.06f) }, passives = new[] { PassiveKind.Sap, PassiveKind.Momentum, PassiveKind.Frenzy } },
                new PartSpec { id = "Bras_Pantin_G", name = "Bras de Bois Tendre", model = PDPaths.PartBody, slot = PuppetSlot.LeftArm, lore = loreArm,
                    stats = new[] { S(StatType.Damage, 1, 2), S(StatType.CritChance, 0.02f, 0.04f) }, passives = new[] { PassiveKind.Sap, PassiveKind.Executioner } },
                new PartSpec { id = "Jambe_Pantin_D", name = "Jambe de Gigue", model = PDPaths.PartBody, slot = PuppetSlot.RightLeg, lore = loreLeg,
                    stats = new[] { S(StatType.MoveSpeed, 0.3f, 0.5f), S(StatType.MaxHealth, 5, 8) }, passives = new[] { PassiveKind.Momentum, PassiveKind.StringDash } },
                new PartSpec { id = "Jambe_Pantin_G", name = "Jambe de Gigue", model = PDPaths.PartBody, slot = PuppetSlot.LeftLeg, lore = loreLeg,
                    stats = new[] { S(StatType.MoveSpeed, 0.3f, 0.5f), S(StatType.MaxHealth, 5, 8) }, passives = new[] { PassiveKind.Momentum, PassiveKind.StringDash } },
                new PartSpec { id = "Bras_Lame", name = "Bras-Lame du Duelliste", model = PDPaths.PartArm, slot = PuppetSlot.RightArm, style = AttackStyle.Blade, lore = loreBlade,
                    stats = new[] { S(StatType.Damage, 5, 7), S(StatType.CritChance, 0.03f, 0.05f) }, passives = new[] { PassiveKind.Executioner, PassiveKind.Splinters, PassiveKind.Frenzy }, weight = 1.2f },
                new PartSpec { id = "Jambe_Ferree_D", name = "Jambe Ferrée", model = PDPaths.PartLeg, slot = PuppetSlot.RightLeg, lore = loreIron,
                    stats = new[] { S(StatType.MoveSpeed, 0.4f, 0.6f), S(StatType.Knockback, 1f, 2f), S(StatType.Armor, 1, 1) }, passives = new[] { PassiveKind.StringDash, PassiveKind.Momentum } },
                new PartSpec { id = "Jambe_Ferree_G", name = "Jambe Ferrée", model = PDPaths.PartLeg, slot = PuppetSlot.LeftLeg, lore = loreIron,
                    stats = new[] { S(StatType.MoveSpeed, 0.4f, 0.6f), S(StatType.Knockback, 1f, 2f), S(StatType.Armor, 1, 1) }, passives = new[] { PassiveKind.StringDash, PassiveKind.Momentum } },
                new PartSpec { id = "Arbalete_Guetteur", name = "Arbalète du Guetteur", model = PDPaths.PartSniper, slot = PuppetSlot.RightArm, style = AttackStyle.Crossbow, lore = loreCrossbow,
                    stats = new[] { S(StatType.Damage, 4, 6), S(StatType.AttackSpeed, 0.04f, 0.07f) }, passives = new[] { PassiveKind.Executioner, PassiveKind.LuckyCharm, PassiveKind.Splinters } },
                new PartSpec { id = "Jambe_Guetteur", name = "Jambe du Guetteur", model = PDPaths.PartSniper, slot = PuppetSlot.RightLeg, lore = loreScoutLeg,
                    stats = new[] { S(StatType.MoveSpeed, 0.5f, 0.8f), S(StatType.DashCooldown, -0.2f, -0.1f) }, passives = new[] { PassiveKind.StringDash, PassiveKind.Momentum } },
                new PartSpec { id = "Jambe_Guetteur_G", name = "Jambe du Guetteur", model = PDPaths.PartSniper, slot = PuppetSlot.LeftLeg, lore = loreScoutLeg,
                    stats = new[] { S(StatType.MoveSpeed, 0.5f, 0.8f), S(StatType.DashCooldown, -0.2f, -0.1f) }, passives = new[] { PassiveKind.StringDash, PassiveKind.Momentum } },
                new PartSpec { id = "Bras_Bouclier", name = "Bras-Bouclier du Rempart", model = PDPaths.PartShield, slot = PuppetSlot.RightArm, style = AttackStyle.Shield, lore = loreShield,
                    stats = new[] { S(StatType.Armor, 3, 5), S(StatType.MaxHealth, 10, 15), S(StatType.Knockback, 2f, 3f) }, passives = new[] { PassiveKind.Thorns, PassiveKind.Varnish } },
                new PartSpec { id = "Bouclier_Main_Gauche", name = "Pavois du Rempart", model = PDPaths.PartShield, slot = PuppetSlot.LeftArm, lore = loreShield,
                    stats = new[] { S(StatType.Armor, 2, 4), S(StatType.MaxHealth, 8, 12) }, passives = new[] { PassiveKind.Thorns, PassiveKind.Varnish, PassiveKind.SecondWind } },
                new PartSpec { id = "Jambe_Rempart", name = "Jambe du Rempart", model = PDPaths.PartShield, slot = PuppetSlot.RightLeg, lore = loreRampartLeg,
                    stats = new[] { S(StatType.Armor, 2, 3), S(StatType.MaxHealth, 8, 12), S(StatType.MoveSpeed, -0.2f, -0.1f) }, passives = new[] { PassiveKind.Thorns, PassiveKind.SecondWind } },
                new PartSpec { id = "Jambe_Rempart_G", name = "Jambe du Rempart", model = PDPaths.PartShield, slot = PuppetSlot.LeftLeg, lore = loreRampartLeg,
                    stats = new[] { S(StatType.Armor, 2, 3), S(StatType.MaxHealth, 8, 12), S(StatType.MoveSpeed, -0.2f, -0.1f) }, passives = new[] { PassiveKind.Thorns, PassiveKind.SecondWind } },
            };
        }

        /// <summary>Construit les visuels et crée les définitions de pièces d'exemple manquantes.</summary>
        public static List<PartDefinition> BuildExampleParts(PDAssets.MaterialSet m)
        {
            PDPaths.EnsureFolder(PDPaths.Parts);
            var result = new List<PartDefinition>();
            foreach (var spec in ExampleSpecs())
            {
                var model = PDPaths.Find<GameObject>(spec.model);
                if (model == null) { Debug.LogWarning("[PuppetDungeon] Modèle introuvable pour " + spec.name + " : " + spec.model); continue; }
                PDImport.ConfigurePartModel(spec.model);
                bool weapon = spec.style != AttackStyle.Fist || spec.model == PDPaths.PartShield;
                var visual = PDRigTools.BuildPartVisual(model, spec.slot, false, "PV_" + spec.id, m.partWood, weapon)
                             ?? PDRigTools.BuildPartVisual(model, spec.slot, true, "PV_" + spec.id, m.partWood, weapon);
                if (visual == null) { Debug.LogWarning($"[PuppetDungeon] Aucun maillage de « {spec.model} » ne correspond à l'emplacement {spec.slot}."); continue; }

                string path = PDPaths.Parts + "/PD_Part_" + spec.id + ".asset";
                var def = AssetDatabase.LoadAssetAtPath<PartDefinition>(path);
                if (def == null)
                {
                    def = ScriptableObject.CreateInstance<PartDefinition>();
                    def.displayName = spec.name;
                    def.slot = spec.slot;
                    def.attackStyle = spec.style;
                    def.baseStats = spec.stats.ToList();
                    def.possiblePassives = spec.passives.ToList();
                    def.minRarity = spec.minRarity;
                    def.minDungeon = spec.minDungeon;
                    def.dropWeight = spec.weight;
                    def.lore = spec.lore;
                    def.visualPrefab = visual;
                    AssetDatabase.CreateAsset(def, path);
                }
                else
                {
                    def.visualPrefab = visual;
                    EditorUtility.SetDirty(def);
                }
                result.Add(def);
            }
            return result;
        }

        public static PartVisual BuildDecoration(string model, string id, Material mat)
        {
            var go = PDPaths.Find<GameObject>(model);
            if (go == null) return null;
            PDImport.ConfigurePartModel(model);
            return PDRigTools.BuildPartVisual(go, null, false, "PV_Deco_" + id, mat, true);
        }

        // ------------------------------------------------------------------ Ennemis

        public static (List<EnemyDefinition> enemies, List<EnemyDefinition> bosses) BuildEnemies(Dictionary<AttackStyle, PuppetAnimationSet> sets, PuppetAnimationSet fallback, PDAssets.MaterialSet m)
        {
            PuppetAnimationSet A(AttackStyle s) => sets != null && sets.TryGetValue(s, out var v) && v != null ? v : fallback;
            var sword = BuildDecoration(PDPaths.PartSword, "Epee", m.woodDark);
            var sniper = BuildDecoration(PDPaths.PartSniper, "Arbalete", m.woodDark);
            var shield = BuildDecoration(PDPaths.PartShield, "Bouclier", m.woodDark);

            var wanderer = BuildEnemyPrefab("Errant", A(AttackStyle.Blade), m, new[] { sword }, 2.0f);
            var scout = BuildEnemyPrefab("Guetteur", A(AttackStyle.Crossbow), m, new[] { sniper }, 2.0f);
            var rampart = BuildEnemyPrefab("Rempart", A(AttackStyle.Shield), m, new[] { shield }, 2.15f);
            var howler = BuildEnemyPrefab("Hurleur", A(AttackStyle.Fist), m, new PartVisual[0], 1.75f);
            var boss = BuildEnemyPrefab("GrandFicele", A(AttackStyle.Blade), m, new[] { sword, shield }, 2.05f);
            var boss2 = BuildEnemyPrefab("Marionnettiste", A(AttackStyle.Crossbow), m, new[] { sniper, sword }, 2.05f);

            PDPaths.EnsureFolder(PDPaths.Enemies);
            var enemies = new List<EnemyDefinition>
            {
                Enemy("Errant", "Pantin errant", wanderer, EnemyBehaviour.Chaser, 34, 9, 0, 3.4f, 1.7f, 1.3f, 0.5f, 0, 1.4f, 0),
                Enemy("Guetteur", "Guetteur corrompu", scout, EnemyBehaviour.Shooter, 24, 7, 0, 3.0f, 1.5f, 2.0f, 0.6f, 6.5f, 1f, 0, projectileSpeed: 9f),
                Enemy("Rempart", "Rempart sans visage", rampart, EnemyBehaviour.Charger, 58, 12, 2, 2.6f, 1.6f, 2.6f, 0.8f, 0, 0.9f, 1, chargeSpeed: 12f),
                Enemy("Hurleur", "Hurleur de copeaux", howler, EnemyBehaviour.Chaser, 18, 6, 0, 4.8f, 1.4f, 0.9f, 0.32f, 0, 1f, 2),
            };
            var bosses = new List<EnemyDefinition>
            {
                Enemy("Boss_GrandFicele", "Le Grand Ficelé", boss, EnemyBehaviour.Boss, 420, 16, 3, 2.7f, 2.4f, 1.6f, 0.7f, 0, 1f, 0, projectileSpeed: 8f, boss: true, scale: 2.2f,
                    title: "Le Grand Ficelé, gardien des bobines"),
                Enemy("Boss_Marionnettiste", "La Marionnettiste déchue", boss2, EnemyBehaviour.Boss, 520, 18, 4, 3.2f, 2.4f, 1.4f, 0.6f, 0, 1f, 3, projectileSpeed: 9.5f, boss: true, scale: 2.0f,
                    title: "La Marionnettiste déchue, qui tirait jadis nos fils"),
            };
            return (enemies, bosses);
        }

        private static EnemyDefinition Enemy(string id, string name, EnemyController prefab, EnemyBehaviour b, float hp, float dmg, float armor, float speed,
            float range, float cooldown, float windup, float preferred, float weight, int minDungeon,
            float projectileSpeed = 9f, float chargeSpeed = 11f, bool boss = false, float scale = 1f, string title = "")
        {
            var anims = prefab != null && prefab.rig != null ? prefab.rig.animationSet : null;
            string path = PDPaths.Enemies + "/PD_Enemy_" + id + ".asset";
            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<EnemyDefinition>();
                def.displayName = name;
                def.behaviour = b;
                def.maxHealth = hp;
                def.damage = dmg;
                def.armor = armor;
                def.moveSpeed = speed;
                def.attackRange = range;
                def.attackCooldown = cooldown;
                def.windup = windup;
                def.preferredDistance = preferred;
                def.spawnWeight = weight;
                def.minDungeon = minDungeon;
                def.projectileSpeed = projectileSpeed;
                def.chargeSpeed = chargeSpeed;
                def.isBoss = boss;
                def.visualScale = scale;
                def.lootRarityBonus = boss ? 1 : 0;
                def.bossTitle = title;
                AssetDatabase.CreateAsset(def, path);
            }
            def.prefab = prefab;
            if (def.animations == null) def.animations = anims;
            EditorUtility.SetDirty(def);
            return def;
        }

        // ------------------------------------------------------------------ Thèmes

        public static List<DungeonTheme> BuildThemes(PDAssets.MaterialSet m, out DungeonTheme village)
        {
            PDPaths.EnsureFolder(PDPaths.Themes);
            var list = new List<DungeonTheme>
            {
                Theme("Combles", new Color(0.62f, 0.47f, 0.33f), new Color(0.55f, 0.41f, 0.29f), new Color(0.38f, 0.28f, 0.22f), new Color(0.72f, 0.58f, 0.42f), new Color(0.92f, 0.56f, 0.24f), new Color(1f, 0.86f, 0.68f), 1.25f, new Color(1f, 0.62f, 0.3f), new Color(0.34f, 0.28f, 0.3f), m),
                Theme("Atelier", new Color(0.46f, 0.5f, 0.38f), new Color(0.4f, 0.44f, 0.33f), new Color(0.28f, 0.32f, 0.26f), new Color(0.58f, 0.62f, 0.46f), new Color(0.6f, 0.85f, 0.4f), new Color(0.92f, 1f, 0.82f), 1.1f, new Color(0.85f, 1f, 0.55f), new Color(0.26f, 0.3f, 0.26f), m),
                Theme("Cave", new Color(0.4f, 0.44f, 0.52f), new Color(0.34f, 0.38f, 0.46f), new Color(0.22f, 0.24f, 0.32f), new Color(0.5f, 0.55f, 0.64f), new Color(0.35f, 0.65f, 0.95f), new Color(0.78f, 0.86f, 1f), 1.0f, new Color(0.5f, 0.75f, 1f), new Color(0.2f, 0.23f, 0.32f), m),
                Theme("Grenier", new Color(0.5f, 0.4f, 0.5f), new Color(0.44f, 0.34f, 0.44f), new Color(0.3f, 0.22f, 0.3f), new Color(0.62f, 0.5f, 0.6f), new Color(0.85f, 0.45f, 0.85f), new Color(1f, 0.82f, 0.9f), 1.05f, new Color(1f, 0.5f, 0.8f), new Color(0.28f, 0.22f, 0.3f), m),
                Theme("Coulisses", new Color(0.55f, 0.3f, 0.28f), new Color(0.47f, 0.25f, 0.24f), new Color(0.3f, 0.14f, 0.14f), new Color(0.7f, 0.45f, 0.35f), new Color(0.95f, 0.75f, 0.3f), new Color(1f, 0.8f, 0.62f), 1.15f, new Color(1f, 0.7f, 0.35f), new Color(0.3f, 0.18f, 0.18f), m),
            };
            village = Theme("Village", new Color(0.62f, 0.56f, 0.42f), new Color(0.56f, 0.5f, 0.37f), new Color(0.5f, 0.4f, 0.3f), new Color(0.78f, 0.68f, 0.5f), new Color(0.95f, 0.5f, 0.35f), new Color(1f, 0.9f, 0.75f), 1.35f, new Color(1f, 0.75f, 0.45f), new Color(0.42f, 0.38f, 0.4f), m);
            return list;
        }

        private static DungeonTheme Theme(string id, Color a, Color b, Color wall, Color top, Color accent, Color light, float intensity, Color lantern, Color ambient, PDAssets.MaterialSet m)
        {
            string path = PDPaths.Themes + "/PD_Theme_" + id + ".asset";
            var t = AssetDatabase.LoadAssetAtPath<DungeonTheme>(path);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<DungeonTheme>();
                t.floorA = a; t.floorB = b; t.wall = wall; t.wallTop = top; t.accent = accent;
                t.lightColor = light; t.lightIntensity = intensity; t.lanternColor = lantern; t.ambient = ambient;
                AssetDatabase.CreateAsset(t, path);
            }
            t.floorMaterial = m.floor;
            t.wallMaterial = m.wall;
            t.wallTopMaterial = m.wallTop;
            t.propMaterial = m.prop;
            EditorUtility.SetDirty(t);
            return t;
        }

        // ------------------------------------------------------------------ Audio

        public static AudioLibrary BuildAudio()
        {
            var lib = PDPaths.LoadOrCreate<AudioLibrary>(PDPaths.Data + "/PD_AudioLibrary.asset");
            var music = PDPaths.Find<AudioClip>(PDPaths.Music);
            var hit = PDPaths.Find<AudioClip>(PDPaths.SfxHit);
            var click = PDPaths.Find<AudioClip>(PDPaths.SfxClick);

            if (lib.musicDungeon == null) lib.musicDungeon = music;
            if (lib.musicVillage == null) lib.musicVillage = music;
            if (lib.musicMenu == null) lib.musicMenu = music;

            // Deux sons fournis, déclinés par la hauteur et le volume pour couvrir tous les besoins.
            Fill(lib.hit, hit, 0.85f, 0.9f, 1.15f);
            Fill(lib.crit, hit, 1f, 1.25f, 1.4f);
            Fill(lib.playerHurt, hit, 1f, 0.6f, 0.75f);
            Fill(lib.enemyDeath, hit, 1f, 0.45f, 0.6f);
            Fill(lib.doorSlam, hit, 1f, 0.4f, 0.5f);
            Fill(lib.alert, hit, 0.9f, 0.7f, 0.8f);
            Fill(lib.swing, click, 0.6f, 0.55f, 0.75f);
            Fill(lib.shoot, click, 0.7f, 1.4f, 1.6f);
            Fill(lib.dash, click, 0.6f, 0.5f, 0.6f);
            Fill(lib.footstep, click, 0.3f, 1.1f, 1.4f);
            Fill(lib.doorOpen, click, 0.7f, 0.7f, 0.8f);
            Fill(lib.roomCleared, click, 0.9f, 1.5f, 1.7f);
            Fill(lib.lootDrop, click, 0.8f, 1.6f, 1.8f);
            Fill(lib.lootPickup, click, 0.9f, 1.8f, 2.0f);
            Fill(lib.legendaryDrop, hit, 1f, 1.5f, 1.6f);
            Fill(lib.uiClick, click, 0.7f, 0.95f, 1.05f);
            Fill(lib.uiHover, click, 0.25f, 1.6f, 1.8f);
            Fill(lib.equip, click, 0.9f, 0.8f, 0.9f);
            Fill(lib.unequip, click, 0.8f, 1.2f, 1.3f);
            Fill(lib.offering, hit, 0.8f, 1.2f, 1.4f);
            EditorUtility.SetDirty(lib);
            return lib;
        }

        private static void Fill(SoundCue cue, AudioClip clip, float volume, float pitchMin, float pitchMax)
        {
            if (cue == null || clip == null || cue.IsValid) return;
            cue.clips = new[] { clip };
            cue.volume = volume;
            cue.pitch = new Vector2(pitchMin, pitchMax);
        }

        // ------------------------------------------------------------------ Crédits

        /// <summary>Catégories d'exemple (gros titres) avec, dans chacune, une personne et son rôle précis à remplacer.</summary>
        public static List<CreditsData.Section> DefaultTeamSections()
        {
            CreditsData.Section S(string title) => new CreditsData.Section
            {
                title = title,
                people = new List<CreditsData.Person> { new CreditsData.Person { name = "(Prénom Nom)", role = "(rôle précis)" } },
                lines = new List<string>(),
            };
            return new List<CreditsData.Section>
            {
                S("Direction & game design"),
                S("Programmation"),
                S("Modélisation, rig & animation"),
                S("Interface & illustrations"),
                S("Musique & sons"),
            };
        }

        /// <summary>Crédits par défaut (à compléter dans Content/PD_Credits.asset — conservés ensuite).</summary>
        public static CreditsData BuildCredits()
        {
            var c = AssetDatabase.LoadAssetAtPath<CreditsData>(PDPaths.Credits);
            if (c != null) return c;
            c = PDPaths.LoadOrCreate<CreditsData>(PDPaths.Credits);
            c.gameTitle = GameInfo.Title;
            c.subtitle = "Volcano Game Jam 3";
            c.sections = DefaultTeamSections();
            c.sections.AddRange(new List<CreditsData.Section>
            {
                new CreditsData.Section { title = "Outils", lines = new List<string> { "Unity 6 • Universal Render Pipeline • Input System • TextMeshPro", "Assistance au développement : Claude (Anthropic)" } },
                new CreditsData.Section { title = "Remerciements", lines = new List<string> { "Toute l'équipe de la Volcano Game Jam", "Et vous, qui tirez encore nos fils." } },
            });
            c.closing = "Merci d'avoir joué !";
            EditorUtility.SetDirty(c);
            return c;
        }

        // ------------------------------------------------------------------ Base de pièces + configuration

        public static PartDatabase RefreshDatabase()
        {
            var db = PDPaths.LoadOrCreate<PartDatabase>(PDPaths.Content + "/PD_PartDatabase.asset");
            db.parts = AssetDatabase.FindAssets("t:PartDefinition")
                .Select(g => AssetDatabase.LoadAssetAtPath<PartDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null).OrderBy(p => p.slot).ThenBy(p => p.displayName).ToList();
            EditorUtility.SetDirty(db);
            return db;
        }

        public static GameConfig BuildConfig(PartDatabase db, List<EnemyDefinition> enemies, List<EnemyDefinition> bosses, GameObject player, GameObject villager,
            LootPickup loot, Projectile bolt, Projectile orb, GameObject uiRoot, VfxLibrary vfx, AudioLibrary audio, List<DungeonTheme> themes,
            PuppetAnimationSet defaultAnims, Dictionary<AttackStyle, PuppetAnimationSet> styleAnims, CreditsData credits)
        {
            var cfg = PDPaths.LoadOrCreate<GameConfig>(PDPaths.Resources + "/" + GameConfig.ResourcePath + ".asset");
            // Salles plus compactes (personnages plus lisibles, façon Link's Awakening)
            if (cfg.roomSizeX.x == 13 && cfg.roomSizeX.y == 19) cfg.roomSizeX = new Vector2Int(11, 15);
            if (cfg.roomSizeZ.x == 9 && cfg.roomSizeZ.y == 13) cfg.roomSizeZ = new Vector2Int(8, 11);
            cfg.partDatabase = db;
            cfg.enemies = enemies;
            cfg.bosses = bosses;
            cfg.playerPrefab = player;
            cfg.villagerPrefab = villager;
            cfg.lootPickupPrefab = loot;
            cfg.playerBoltPrefab = bolt;
            cfg.enemyOrbPrefab = orb;
            cfg.uiRootPrefab = uiRoot;
            cfg.vfx = vfx;
            cfg.audio = audio;
            cfg.themes = themes;
            if (cfg.defaultAnimations == null) cfg.defaultAnimations = defaultAnims;
            if (cfg.styleAnimations == null) cfg.styleAnimations = new List<StyleAnimations>();
            if (styleAnims != null)
                foreach (var kv in styleAnims)
                {
                    var entry = cfg.styleAnimations.FirstOrDefault(x => x != null && x.style == kv.Key);
                    if (entry == null) cfg.styleAnimations.Add(new StyleAnimations { style = kv.Key, set = kv.Value });
                    else if (entry.set == null) entry.set = kv.Value;
                }
            if (cfg.credits == null) cfg.credits = credits;
            EditorUtility.SetDirty(cfg);
            return cfg;
        }
    }
}
