using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PuppetDungeon
{
    /// <summary>
    /// Réglages globaux du jeu (équilibrage, références de prefabs).
    /// L'outil de setup crée l'asset dans Assets/PuppetDungeon/Resources/PD_GameConfig.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "PD_GameConfig", menuName = "Puppet Dungeon/Game Config", order = 10)]
    public class GameConfig : ScriptableObject
    {
        public const string ResourcePath = "PD_GameConfig";

        [Header("Boucle de run")]
        [Tooltip("Nombre de donjons entre deux villages de marionnettes.")]
        [Min(1)] public int dungeonsPerVillage = 5;
        [Tooltip("Les courbes de probabilité sont décalées d'autant de salles par donjon terminé (donjons plus longs).")]
        public float extraRoomsPerDungeon = 0.35f;
        [Tooltip("Taille des salles en X (min, max inclus).")]
        public Vector2Int roomSizeX = new Vector2Int(11, 15);
        [Tooltip("Taille des salles en Z (min, max inclus).")]
        public Vector2Int roomSizeZ = new Vector2Int(8, 11);
        [Header("Génération : probabilités selon les salles parcourues")]
        [Tooltip("Chance (0 → 1) que la prochaine NOUVELLE salle soit la sortie, en fonction du nombre de salles déjà parcourues dans le donjon (axe X).")]
        public AnimationCurve exitChanceByRooms = Linear(new Vector2(0, 0), new Vector2(2, 0), new Vector2(3, 0.05f), new Vector2(5, 0.2f), new Vector2(8, 0.5f), new Vector2(10, 0.75f), new Vector2(12, 1f));
        [Tooltip("Chance (0 → 1) qu'une nouvelle salle soit une salle de boss, en fonction des salles parcourues (peu de salles = peu de chance).")]
        public AnimationCurve bossChanceByRooms = Linear(new Vector2(0, 0), new Vector2(2, 0), new Vector2(3, 0.04f), new Vector2(6, 0.12f), new Vector2(10, 0.2f), new Vector2(16, 0.28f));
        [Tooltip("Filet de sécurité : au-delà de ce nombre de salles parcourues sans sortie, la sortie est garantie.")]
        [Min(2)] public int maxRoomsBeforeExit = 14;
        [Tooltip("Une nouvelle sortie peut réapparaître si la précédente a été laissée derrière depuis autant de salles.")]
        [Min(1)] public int roomsBeforeAnotherExit = 4;
        [Tooltip("Nombre maximum de salles de boss par donjon (hors gardien de la sortie).")]
        [Min(0)] public int maxBossRoomsPerDungeon = 2;
        [Range(0f, 1f)] public float exitGuardianChance = 0.5f;
        [Tooltip("Rayon de la zone de sûreté devant la porte d'entrée.")]
        public float safeZoneRadius = 2.6f;

        [Header("Ennemis")]
        public List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        public List<EnemyDefinition> bosses = new List<EnemyDefinition>();
        public Vector2Int enemiesPerRoom = new Vector2Int(2, 4);
        public float extraEnemiesPerDungeon = 0.4f;
        [Tooltip("Multiplicateur de PV ennemis par donjon (exponentiel).")]
        public float enemyHealthGrowth = 1.17f;
        [Tooltip("Multiplicateur de dégâts ennemis par donjon (exponentiel).")]
        public float enemyDamageGrowth = 1.13f;
        [Tooltip("Multiplicateur additionnel à chaque cycle de village (la difficulté grimpe drastiquement).")]
        public float cycleDifficultyMultiplier = 1.6f;

        [Header("Prise en main (premiers donjons plus doux)")]
        [Tooltip("Nombre de donjons sur lesquels la difficulté remonte progressivement jusqu'à la normale (0 = désactivé).")]
        [Min(0)] public int earlyRampDungeons = 3;
        [Tooltip("PV des ennemis au donjon 1 (×), remonte jusqu'à ×1.")]
        [Range(0.3f, 1f)] public float earlyEnemyHealth = 0.7f;
        [Tooltip("Dégâts des ennemis au donjon 1 (×), remonte jusqu'à ×1.")]
        [Range(0.3f, 1f)] public float earlyEnemyDamage = 0.65f;
        [Tooltip("Préparation et temps de recharge des attaques ennemies au donjon 1 (× : plus lent = plus lisible).")]
        [Range(1f, 2f)] public float earlyEnemySlowness = 1.3f;
        [Tooltip("Donjon 1 : aucune salle de boss avant ce nombre de salles parcourues.")]
        [Min(0)] public int firstDungeonBossFreeRooms = 6;
        [Tooltip("Donjon 1 : la chance de salle de boss est multipliée par cette valeur.")]
        [Range(0f, 1f)] public float firstDungeonBossMultiplier = 0.35f;
        [Tooltip("Donjon 1 : chance que la sortie soit gardée par un boss (remonte jusqu'à « Exit Guardian Chance »).")]
        [Range(0f, 1f)] public float firstDungeonExitGuardianChance = 0.15f;
        [HideInInspector] public int balanceVersion;

        [Header("Butin")]
        public PartDatabase partDatabase;
        [Range(0f, 1f)] public float enemyLootChance = 0.28f;
        public int bossLootCount = 2;
        [Tooltip("Poids de rareté (Commun → Légendaire) au donjon 0.")]
        public float[] rarityWeights = { 60f, 26f, 10f, 3.5f, 0.8f };
        [Tooltip("Décalage des poids vers les raretés hautes, par donjon.")]
        public float rarityShiftPerDungeon = 0.08f;
        [Tooltip("Multiplicateur de stats par rareté.")]
        public float[] rarityStatMultiplier = { 1f, 1.3f, 1.65f, 2.1f, 2.8f };
        [Tooltip("Stats supplémentaires tirées par rareté.")]
        public int[] rarityExtraStats = { 0, 1, 1, 2, 3 };
        [Tooltip("Nombre de passifs par rareté.")]
        public int[] rarityPassiveCount = { 0, 0, 1, 1, 2 };
        [Tooltip("Croissance de la puissance des pièces par donjon (exponentielle).")]
        public float itemPowerGrowth = 1.15f;
        public Color[] rarityColors =
        {
            new Color(0.82f, 0.78f, 0.72f),
            new Color(0.45f, 0.85f, 0.45f),
            new Color(0.35f, 0.62f, 1f),
            new Color(0.75f, 0.42f, 1f),
            new Color(1f, 0.66f, 0.18f)
        };

        [Header("Pantin")]
        public BaseStats playerBase = new BaseStats();
        [Tooltip("Multiplicateur des PV/dégâts de base à chaque nouveau pantin (village).")]
        public float rebirthBaseGrowth = 1.35f;
        [Min(4)] public int inventorySize = 24;
        [Tooltip("Le nouveau pantin garde-t-il le sac de l'ancien ?")]
        public bool rebirthKeepsInventory = false;
        [Range(0f, 1f)] public float multiOfferingChance = 0.15f;
        [Min(2)] public int multiOfferingMax = 3;
        [Tooltip("Pièces équipées sur le tout premier pantin (optionnel).")]
        public List<PartDefinition> starterParts = new List<PartDefinition>();

        [Header("Gacha du village")]
        [Tooltip("Nom de la monnaie obtenue en recyclant les pièces non offertes.")]
        public string currencyName = "Bobines";
        [Tooltip("Bobines rendues par une pièce recyclée, par rareté (Commun → Légendaire).")]
        public int[] rarityCurrencyValue = { 4, 10, 24, 60, 150 };
        [Tooltip("Bobines supplémentaires par niveau d'objet de la pièce recyclée.")]
        public float currencyPerItemLevel = 0.5f;
        [Tooltip("Prix d'un tirage au premier village.")]
        [Min(1)] public int gachaCost = 20;
        [Tooltip("Multiplicateur du prix à chaque nouveau cycle de village.")]
        public float gachaCostGrowthPerCycle = 1.25f;
        [Tooltip("Nombre de tirages du tirage multiple.")]
        [Min(2)] public int gachaMultiCount = 5;
        [Tooltip("Remise sur le tirage multiple (0.1 = 10 %).")]
        [Range(0f, 0.5f)] public float gachaMultiDiscount = 0.1f;
        [Tooltip("Poids des raretés du gacha (Commun → Légendaire).")]
        public float[] gachaRarityWeights = { 45f, 32f, 16f, 6f, 1f };
        [Tooltip("Garantie : après autant de tirages sans pièce Épique ou mieux, la suivante l'est (0 = désactivé).")]
        [Min(0)] public int gachaPity = 15;
        [Tooltip("Part du prix rendue si le sac est plein (la pièce est recyclée).")]
        [Range(0f, 1f)] public float gachaFullBagRefund = 0.5f;

        [Header("Animations")]
        [Tooltip("Jeu d'animations par défaut (poings, villageois, menus).")]
        public PuppetAnimationSet defaultAnimations;
        [Tooltip("Jeu d'animations selon le style d'arme du bras droit.")]
        public List<StyleAnimations> styleAnimations = new List<StyleAnimations>();

        [Header("Crédits")]
        public CreditsData credits;

        [Header("Prefabs")]
        public GameObject playerPrefab;
        public GameObject villagerPrefab;
        public LootPickup lootPickupPrefab;
        public Projectile playerBoltPrefab;
        public Projectile enemyOrbPrefab;
        public GameObject uiRootPrefab;
        public VfxLibrary vfx;
        public AudioLibrary audio;
        public List<DungeonTheme> themes = new List<DungeonTheme>();

        [Header("Textes")]
        public string[] dungeonNames =
        {
            "Les Combles Poussiéreux", "L'Atelier Oublié", "La Cave aux Bobines", "Le Grenier des Ficelles",
            "Les Coulisses Muettes", "La Fosse d'Orchestre", "Le Théâtre Englouti", "Les Ateliers de Cire",
            "La Remise des Masques", "Les Cintres Effondrés"
        };
        public string[] puppetNames =
        {
            "Brindille", "Copeau", "Bobine", "Ficelle", "Noisette", "Grelot", "Pampille", "Tilleul",
            "Mirliton", "Sciure", "Babiole", "Clochette", "Bouleau", "Galoche", "Pantoufle", "Rotin"
        };

        public Color RarityColor(Rarity r)
        {
            int i = Mathf.Clamp((int)r, 0, rarityColors.Length - 1);
            return rarityColors.Length > 0 ? rarityColors[i] : Color.white;
        }

        public string RarityHex(Rarity r) => "#" + ColorUtility.ToHtmlStringRGB(RarityColor(r));

        public int CycleOf(int dungeonIndex) => dungeonIndex / Mathf.Max(1, dungeonsPerVillage);

        /// <summary>0 au premier donjon → 1 une fois la prise en main terminée.</summary>
        public float EarlyRamp(int dungeonIndex)
            => earlyRampDungeons <= 0 ? 1f : Mathf.Clamp01(Mathf.Max(0, dungeonIndex) / (float)earlyRampDungeons);

        public float EnemyHealthScale(int dungeonIndex)
            => Mathf.Pow(enemyHealthGrowth, dungeonIndex) * Mathf.Pow(cycleDifficultyMultiplier, CycleOf(dungeonIndex))
               * Mathf.Lerp(earlyEnemyHealth, 1f, EarlyRamp(dungeonIndex));

        public float EnemyDamageScale(int dungeonIndex)
            => Mathf.Pow(enemyDamageGrowth, dungeonIndex) * Mathf.Pow(Mathf.Sqrt(cycleDifficultyMultiplier), CycleOf(dungeonIndex))
               * Mathf.Lerp(earlyEnemyDamage, 1f, EarlyRamp(dungeonIndex));

        /// <summary>Multiplicateur des temps de préparation / recharge ennemis (> 1 au début).</summary>
        public float EnemyTempo(int dungeonIndex) => Mathf.Lerp(Mathf.Max(1f, earlyEnemySlowness), 1f, EarlyRamp(dungeonIndex));

        /// <summary>Chance que la sortie soit gardée par un boss.</summary>
        public float ExitGuardianChance(int dungeonIndex) => Mathf.Lerp(Mathf.Min(firstDungeonExitGuardianChance, exitGuardianChance), exitGuardianChance, EarlyRamp(dungeonIndex));

        public float ItemPowerScale(int itemLevel)
            => Mathf.Pow(itemPowerGrowth, Mathf.Max(0, itemLevel - 1)) * Mathf.Pow(Mathf.Sqrt(cycleDifficultyMultiplier), CycleOf(itemLevel - 1));

        /// <summary>Salles parcourues « effectives » (les donjons avancés sont plus longs).</summary>
        public float EffectiveRooms(int roomsVisited, int dungeonIndex) => roomsVisited - dungeonIndex * extraRoomsPerDungeon;

        /// <summary>Probabilité que la prochaine nouvelle salle soit la sortie.</summary>
        public float ExitChance(int roomsVisited, int dungeonIndex)
        {
            if (roomsVisited >= maxRoomsBeforeExit + Mathf.FloorToInt(dungeonIndex * extraRoomsPerDungeon)) return 1f;
            return exitChanceByRooms != null ? Mathf.Clamp01(exitChanceByRooms.Evaluate(EffectiveRooms(roomsVisited, dungeonIndex))) : 0.2f;
        }

        /// <summary>Probabilité qu'une nouvelle salle (non-sortie) soit une salle de boss.</summary>
        public float BossChance(int roomsVisited, int dungeonIndex)
        {
            float c = bossChanceByRooms != null ? Mathf.Clamp01(bossChanceByRooms.Evaluate(EffectiveRooms(roomsVisited, dungeonIndex))) : 0.1f;
            if (dungeonIndex <= 0)
            {
                if (roomsVisited < firstDungeonBossFreeRooms) return 0f;
                c *= firstDungeonBossMultiplier;
            }
            return c;
        }

        public int CurrencyValue(PartInstance p)
        {
            if (p == null) return 0;
            int r = Mathf.Clamp((int)p.rarity, 0, rarityCurrencyValue.Length - 1);
            int baseValue = rarityCurrencyValue.Length > 0 ? rarityCurrencyValue[r] : 5;
            return Mathf.Max(1, Mathf.RoundToInt(baseValue + p.itemLevel * currencyPerItemLevel));
        }

        public int GachaCost(int dungeonIndex) => Mathf.Max(1, Mathf.RoundToInt(gachaCost * Mathf.Pow(gachaCostGrowthPerCycle, Mathf.Max(0, CycleOf(dungeonIndex) - 1))));
        public int GachaMultiCost(int dungeonIndex) => Mathf.Max(1, Mathf.RoundToInt(GachaCost(dungeonIndex) * gachaMultiCount * (1f - gachaMultiDiscount)));

        public PuppetAnimationSet AnimationsFor(AttackStyle style)
        {
            if (styleAnimations != null)
                foreach (var s in styleAnimations)
                    if (s != null && s.style == style && s.set != null) return s.set;
            return defaultAnimations;
        }

        private static AnimationCurve Linear(params Vector2[] pts)
        {
            var keys = new Keyframe[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                float inT = i > 0 ? (pts[i].y - pts[i - 1].y) / Mathf.Max(0.0001f, pts[i].x - pts[i - 1].x) : 0f;
                float outT = i < pts.Length - 1 ? (pts[i + 1].y - pts[i].y) / Mathf.Max(0.0001f, pts[i + 1].x - pts[i].x) : 0f;
                keys[i] = new Keyframe(pts[i].x, pts[i].y, inT, outT);
            }
            return new AnimationCurve(keys);
        }

        public int EnemiesForRoom(int dungeonIndex)
        {
            // Donjon 1 : une salle compte au plus un ennemi de moins.
            int max = enemiesPerRoom.y - (dungeonIndex <= 0 && enemiesPerRoom.y > enemiesPerRoom.x ? 1 : 0);
            int e = Random.Range(Mathf.Max(1, enemiesPerRoom.x), Mathf.Max(1, max) + 1);
            return e + Mathf.FloorToInt(dungeonIndex * extraEnemiesPerDungeon);
        }

        public DungeonTheme ThemeFor(int dungeonIndex)
        {
            if (themes == null || themes.Count == 0) return null;
            return themes[dungeonIndex % themes.Count];
        }

        public string DungeonName(int dungeonIndex)
        {
            if (dungeonNames == null || dungeonNames.Length == 0) return "Donjon " + (dungeonIndex + 1);
            return dungeonNames[dungeonIndex % dungeonNames.Length];
        }

        private static GameConfig cached;
        public static GameConfig Load()
        {
            if (cached == null) cached = Resources.Load<GameConfig>(ResourcePath);
            return cached;
        }
    }

    [Serializable]
    public class StyleAnimations
    {
        public AttackStyle style;
        public PuppetAnimationSet set;
    }
}
