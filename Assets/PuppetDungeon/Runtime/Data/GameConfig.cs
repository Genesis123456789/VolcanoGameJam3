using System.Collections.Generic;
using UnityEngine;

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
        [Tooltip("Salles (min, max) avant la salle de sortie, au premier donjon.")]
        public Vector2Int roomsPerDungeon = new Vector2Int(4, 6);
        [Tooltip("Salles supplémentaires par donjon traversé.")]
        public float extraRoomsPerDungeon = 0.35f;
        [Tooltip("Taille des salles en X (min, max inclus).")]
        public Vector2Int roomSizeX = new Vector2Int(11, 15);
        [Tooltip("Taille des salles en Z (min, max inclus).")]
        public Vector2Int roomSizeZ = new Vector2Int(8, 11);
        [Range(0f, 1f)] public float bossRoomChance = 0.12f;
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

        public float EnemyHealthScale(int dungeonIndex)
            => Mathf.Pow(enemyHealthGrowth, dungeonIndex) * Mathf.Pow(cycleDifficultyMultiplier, CycleOf(dungeonIndex));

        public float EnemyDamageScale(int dungeonIndex)
            => Mathf.Pow(enemyDamageGrowth, dungeonIndex) * Mathf.Pow(Mathf.Sqrt(cycleDifficultyMultiplier), CycleOf(dungeonIndex));

        public float ItemPowerScale(int itemLevel)
            => Mathf.Pow(itemPowerGrowth, Mathf.Max(0, itemLevel - 1)) * Mathf.Pow(Mathf.Sqrt(cycleDifficultyMultiplier), CycleOf(itemLevel - 1));

        public int RoomsForDungeon(int dungeonIndex)
        {
            int r = Random.Range(roomsPerDungeon.x, roomsPerDungeon.y + 1);
            return r + Mathf.FloorToInt(dungeonIndex * extraRoomsPerDungeon);
        }

        public int EnemiesForRoom(int dungeonIndex)
        {
            int e = Random.Range(enemiesPerRoom.x, enemiesPerRoom.y + 1);
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
}
