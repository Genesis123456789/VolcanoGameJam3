using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PuppetDungeon
{
    [Serializable]
    public struct PassiveRoll
    {
        public PassiveKind kind;
        public float magnitude;

        public PassiveRoll(PassiveKind kind, float magnitude)
        {
            this.kind = kind;
            this.magnitude = magnitude;
        }

        public string Describe() => PassiveCatalog.Describe(kind, magnitude);
    }

    /// <summary>Une pièce concrète (tirée) : définition + rareté + niveau + stats et passifs tirés.</summary>
    [Serializable]
    public class PartInstance
    {
        public string uid;
        public PartDefinition definition;
        public Rarity rarity;
        public int itemLevel = 1;
        public List<StatModifier> stats = new List<StatModifier>();
        public List<PassiveRoll> passives = new List<PassiveRoll>();
        [Tooltip("Génération de pantin qui l'a trouvée (affichage lore).")]
        public int foundByGeneration;

        public PuppetSlot Slot => definition != null ? definition.slot : PuppetSlot.Head;
        public AttackStyle AttackStyle => definition != null ? definition.attackStyle : AttackStyle.Fist;

        public static string RarityPrefix(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return "Verni";
                case Rarity.Rare: return "Ciselé";
                case Rarity.Epic: return "Enchanté";
                case Rarity.Legendary: return "Ancestral";
                default: return "";
            }
        }

        public string DisplayName
        {
            get
            {
                string n = definition != null ? definition.displayName : "Pièce inconnue";
                string p = RarityPrefix(rarity);
                return string.IsNullOrEmpty(p) ? n : $"{n} {p.ToLowerInvariant()}";
            }
        }

        /// <summary>Puissance globale approximative (sert au tri et aux comparaisons rapides).</summary>
        public float PowerScore
        {
            get
            {
                float s = itemLevel * 2f + (int)rarity * 6f + passives.Count * 5f;
                foreach (var m in stats) s += Mathf.Abs(m.value) * (m.mode == ModifierMode.Percent ? 40f : 1f);
                return s;
            }
        }

        public string BuildTooltipBody(bool includeLore = true)
        {
            var sb = new StringBuilder();
            sb.Append("<color=#C9B79C>").Append(EnumUtil.SlotLabel(Slot));
            if (Slot == PuppetSlot.RightArm && AttackStyle != AttackStyle.Fist)
                sb.Append(" • ").Append(EnumUtil.AttackStyleLabel(AttackStyle));
            sb.Append(" • Niv. ").Append(itemLevel).Append("</color>\n");
            foreach (var m in stats)
                sb.Append(m.value >= 0 || m.stat == StatType.DashCooldown ? "<color=#9BE37A>" : "<color=#E36B5B>")
                  .Append(m.Describe()).Append("</color>\n");
            foreach (var p in passives)
                sb.Append("<color=#F2C14E>» ").Append(p.Describe()).Append("</color>\n");
            if (includeLore && definition != null && !string.IsNullOrWhiteSpace(definition.lore))
                sb.Append("\n<i><color=#A89A86>").Append(definition.lore.Trim()).Append("</color></i>");
            return sb.ToString();
        }
    }

    /// <summary>Valeurs et textes des passifs (code unique, pas un script par passif).</summary>
    public static class PassiveCatalog
    {
        /// <summary>Magnitude de base à la rareté Rare, niveau 1.</summary>
        public static float BaseMagnitude(PassiveKind k)
        {
            switch (k)
            {
                case PassiveKind.Splinters: return 0.6f;
                case PassiveKind.Sap: return 0.04f;
                case PassiveKind.Thorns: return 0.35f;
                case PassiveKind.SecondWind: return 0.12f;
                case PassiveKind.Frenzy: return 0.4f;
                case PassiveKind.Momentum: return 0.25f;
                case PassiveKind.Executioner: return 0.25f;
                case PassiveKind.Varnish: return 0.2f;
                case PassiveKind.StringDash: return 0.9f;
                case PassiveKind.LuckyCharm: return 0.15f;
                default: return 0f;
            }
        }

        public static string Describe(PassiveKind k, float m)
        {
            string pct = $"{m * 100f:0}%";
            switch (k)
            {
                case PassiveKind.Splinters: return $"Échardes : un ennemi vaincu explose et inflige {pct} de vos dégâts autour de lui.";
                case PassiveKind.Sap: return $"Sève : +{pct} de vol de vie.";
                case PassiveKind.Thorns: return $"Ronces : renvoie {pct} des dégâts subis à l'attaquant.";
                case PassiveKind.SecondWind: return $"Second souffle : rend {pct} des PV max à chaque salle nettoyée.";
                case PassiveKind.Frenzy: return $"Frénésie : +{pct} de dégâts sous 35 % de PV.";
                case PassiveKind.Momentum: return $"Élan : +{pct} de vitesse pendant 3 s après un ennemi vaincu.";
                case PassiveKind.Executioner: return $"Bourreau : +{pct} de chance de critique contre les ennemis blessés.";
                case PassiveKind.Varnish: return $"Vernis : absorbe le premier coup de chaque salle (jusqu'à {pct} des PV max).";
                case PassiveKind.StringDash: return $"Fil tranchant : la ruée inflige {pct} de vos dégâts.";
                case PassiveKind.LuckyCharm: return $"Porte-bonheur : +{pct} de chance de butin et de rareté.";
                default: return "";
            }
        }
    }
}
