using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Emplacement d'une pièce sur le pantin (repris du prototype : PuppetPartEnum).</summary>
    public enum PuppetSlot
    {
        Head = 0,
        Torso = 1,
        LeftArm = 2,
        RightArm = 3,
        LeftLeg = 4,
        RightLeg = 5
    }

    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    /// <summary>Statistiques du pantin. Toutes les pièces modifient ces valeurs.</summary>
    public enum StatType
    {
        MaxHealth = 0,      // points de vie
        Damage = 1,         // dégâts de base par coup
        Armor = 2,          // réduction plate des dégâts reçus
        MoveSpeed = 3,      // m/s
        AttackSpeed = 4,    // multiplicateur (1 = normal)
        CritChance = 5,     // 0..1
        CritDamage = 6,     // multiplicateur des critiques (1.5 = +50%)
        LifeSteal = 7,      // 0..1 des dégâts infligés rendus en PV
        DashCooldown = 8,   // secondes (les pièces le réduisent avec des valeurs négatives)
        Knockback = 9       // force de recul infligée
    }

    public enum ModifierMode
    {
        Flat = 0,       // + valeur
        Percent = 1     // + valeur % de la base (0.1 = +10 %)
    }

    /// <summary>Style d'attaque donné par le bras droit (main d'arme).</summary>
    public enum AttackStyle
    {
        Fist = 0,       // pantin nu : petit coup de poing
        Blade = 1,      // arc de lame large
        Crossbow = 2,   // carreau à distance
        Shield = 3      // coup de bouclier : recul énorme + réduction des dégâts pendant l'attaque
    }

    /// <summary>Passifs génériques. Leur puissance dépend de la rareté et du niveau d'objet.</summary>
    public enum PassiveKind
    {
        None = 0,
        Splinters = 1,      // à la mort d'un ennemi : explosion d'échardes autour de lui
        Sap = 2,            // vol de vie supplémentaire
        Thorns = 3,         // renvoie une partie des dégâts reçus
        SecondWind = 4,     // soigne à la fin d'une salle
        Frenzy = 5,         // + dégâts quand les PV sont bas
        Momentum = 6,       // + vitesse pendant quelques secondes après un kill
        Executioner = 7,    // + critique contre les ennemis blessés
        Varnish = 8,        // bouclier qui absorbe le premier coup de chaque salle
        StringDash = 9,     // la ruée inflige des dégâts
        LuckyCharm = 10     // meilleure chance de butin
    }

    public enum EnemyBehaviour
    {
        Chaser = 0,     // fonce et frappe au corps à corps
        Shooter = 1,    // garde ses distances et tire
        Charger = 2,    // charge en ligne droite après un temps d'armement
        Boss = 3        // mélange des trois + attaques de zone
    }

    public enum RoomKind
    {
        Start = 0,
        Normal = 1,
        Boss = 2,
        Exit = 3
    }

    public enum WallSide
    {
        North = 0,  // +Z
        South = 1,  // -Z
        East = 2,   // +X
        West = 3    // -X
    }

    public static class EnumUtil
    {
        public static WallSide Opposite(WallSide s)
        {
            switch (s)
            {
                case WallSide.North: return WallSide.South;
                case WallSide.South: return WallSide.North;
                case WallSide.East: return WallSide.West;
                default: return WallSide.East;
            }
        }

        /// <summary>Direction qui part du mur vers l'intérieur de la salle.</summary>
        public static Vector3 Inward(WallSide s)
        {
            switch (s)
            {
                case WallSide.North: return Vector3.back;
                case WallSide.South: return Vector3.forward;
                case WallSide.East: return Vector3.left;
                default: return Vector3.right;
            }
        }

        public static bool IsLeft(PuppetSlot s) => s == PuppetSlot.LeftArm || s == PuppetSlot.LeftLeg;
        public static bool IsArm(PuppetSlot s) => s == PuppetSlot.LeftArm || s == PuppetSlot.RightArm;
        public static bool IsLeg(PuppetSlot s) => s == PuppetSlot.LeftLeg || s == PuppetSlot.RightLeg;

        public static string SlotLabel(PuppetSlot s)
        {
            switch (s)
            {
                case PuppetSlot.Head: return "Tête";
                case PuppetSlot.Torso: return "Torse";
                case PuppetSlot.LeftArm: return "Bras gauche";
                case PuppetSlot.RightArm: return "Bras droit";
                case PuppetSlot.LeftLeg: return "Jambe gauche";
                default: return "Jambe droite";
            }
        }

        public static string RarityLabel(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return "Commun";
                case Rarity.Uncommon: return "Peu commun";
                case Rarity.Rare: return "Rare";
                case Rarity.Epic: return "Épique";
                default: return "Légendaire";
            }
        }

        public static string StatLabel(StatType t)
        {
            switch (t)
            {
                case StatType.MaxHealth: return "Points de vie";
                case StatType.Damage: return "Dégâts";
                case StatType.Armor: return "Armure";
                case StatType.MoveSpeed: return "Vitesse";
                case StatType.AttackSpeed: return "Vitesse d'attaque";
                case StatType.CritChance: return "Chance de critique";
                case StatType.CritDamage: return "Dégâts critiques";
                case StatType.LifeSteal: return "Vol de vie";
                case StatType.DashCooldown: return "Recharge de ruée";
                default: return "Recul";
            }
        }

        public static string AttackStyleLabel(AttackStyle a)
        {
            switch (a)
            {
                case AttackStyle.Blade: return "Lame";
                case AttackStyle.Crossbow: return "Arbalète";
                case AttackStyle.Shield: return "Bouclier";
                default: return "Poing";
            }
        }

        /// <summary>Vrai si la stat s'affiche en pourcentage (0.1 → 10 %).</summary>
        public static bool IsRatioStat(StatType t)
        {
            return t == StatType.CritChance || t == StatType.LifeSteal || t == StatType.AttackSpeed || t == StatType.CritDamage;
        }
    }
}
