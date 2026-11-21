using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Définition d'une pièce de pantin (contenu). Créez-en une avec
    /// Assets > Create > Puppet Dungeon > Part Definition, ou avec
    /// Tools > Puppet Dungeon > Créer une pièce depuis un modèle.
    /// Toute nouvelle définition est ajoutée automatiquement à la PartDatabase.
    /// </summary>
    [CreateAssetMenu(fileName = "PD_Part_New", menuName = "Puppet Dungeon/Part Definition", order = 0)]
    public class PartDefinition : ScriptableObject
    {
        [Header("Identité")]
        public string displayName = "Nouvelle pièce";
        public PuppetSlot slot = PuppetSlot.RightArm;
        [Tooltip("Icône d'inventaire. Laissez vide : une icône est rendue automatiquement depuis le visuel.")]
        public Sprite icon;

        [Header("Visuel")]
        [Tooltip("Prefab contenant un composant PartVisual (créé par l'outil 'Créer une pièce depuis un modèle').")]
        public PartVisual visualPrefab;
        [Tooltip("Cache les segments du pantin de base remplacés par cette pièce.")]
        public bool replaceBaseSegments = true;

        [Header("Gameplay")]
        [Tooltip("Uniquement pour le bras droit : style d'attaque accordé.")]
        public AttackStyle attackStyle = AttackStyle.Fist;
        [Tooltip("Stats de base (niveau d'objet 1, Commun). Mises à l'échelle par la rareté et la profondeur.")]
        public List<StatRange> baseStats = new List<StatRange>();
        [Tooltip("Passifs que cette pièce peut obtenir (tirés à partir de la rareté Rare).")]
        public List<PassiveKind> possiblePassives = new List<PassiveKind>();

        [Header("Butin")]
        public Rarity minRarity = Rarity.Common;
        public Rarity maxRarity = Rarity.Legendary;
        [Min(0f)] public float dropWeight = 1f;
        [Tooltip("Donjon minimum (0 = dès le début) où la pièce peut tomber.")]
        [Min(0)] public int minDungeon = 0;
        [Tooltip("Pièce de départ / de base : ne tombe pas en butin, sert à équiper le pantin initial.")]
        public bool starterOnly = false;

        [Header("Histoire")]
        [TextArea(4, 10)] public string lore = "";

        public string SlotLabel => EnumUtil.SlotLabel(slot);
    }
}
