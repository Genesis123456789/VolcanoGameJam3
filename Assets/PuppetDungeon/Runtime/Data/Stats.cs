using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Un bonus de stat déjà tiré (valeur fixe), porté par une pièce.</summary>
    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public ModifierMode mode;
        public float value;

        public StatModifier(StatType stat, ModifierMode mode, float value)
        {
            this.stat = stat;
            this.mode = mode;
            this.value = value;
        }

        public string Describe()
        {
            string sign = value >= 0 ? "+" : "-";
            float abs = Mathf.Abs(value);
            if (mode == ModifierMode.Percent)
                return $"{sign}{abs * 100f:0}% {EnumUtil.StatLabel(stat)}";
            if (stat == StatType.DashCooldown)
                return $"{sign}{abs:0.00}s {EnumUtil.StatLabel(stat)}";
            if (EnumUtil.IsRatioStat(stat))
                return $"{sign}{abs * 100f:0}% {EnumUtil.StatLabel(stat)}";
            if (stat == StatType.MoveSpeed)
                return $"{sign}{abs:0.0} {EnumUtil.StatLabel(stat)}";
            return $"{sign}{abs:0} {EnumUtil.StatLabel(stat)}";
        }
    }

    /// <summary>Plage de valeurs d'une stat dans une définition de pièce (avant tirage).</summary>
    [Serializable]
    public struct StatRange
    {
        public StatType stat;
        public ModifierMode mode;
        [Tooltip("Valeur minimale au niveau d'objet 1, rareté Commune.")]
        public float min;
        [Tooltip("Valeur maximale au niveau d'objet 1, rareté Commune.")]
        public float max;

        public StatRange(StatType stat, ModifierMode mode, float min, float max)
        {
            this.stat = stat;
            this.mode = mode;
            this.min = min;
            this.max = max;
        }
    }

    /// <summary>Statistiques finales calculées du pantin.</summary>
    [Serializable]
    public class StatBlock
    {
        private readonly float[] values = new float[Enum.GetValues(typeof(StatType)).Length];

        public float this[StatType t]
        {
            get => values[(int)t];
            set => values[(int)t] = value;
        }

        public StatBlock Clone()
        {
            var b = new StatBlock();
            Array.Copy(values, b.values, values.Length);
            return b;
        }

        /// <summary>Calcule base + bonus plats, puis applique les pourcentages sur le résultat.</summary>
        public static StatBlock Compute(StatBlock baseStats, IEnumerable<StatModifier> mods)
        {
            var flat = baseStats.Clone();
            int n = flat.values.Length;
            var pct = new float[n];
            if (mods != null)
            {
                foreach (var m in mods)
                {
                    if (m.mode == ModifierMode.Flat) flat.values[(int)m.stat] += m.value;
                    else pct[(int)m.stat] += m.value;
                }
            }
            for (int i = 0; i < n; i++) flat.values[i] *= 1f + pct[i];

            // Bornes de sécurité
            flat[StatType.MaxHealth] = Mathf.Max(1f, flat[StatType.MaxHealth]);
            flat[StatType.Damage] = Mathf.Max(1f, flat[StatType.Damage]);
            flat[StatType.Armor] = Mathf.Max(0f, flat[StatType.Armor]);
            flat[StatType.MoveSpeed] = Mathf.Clamp(flat[StatType.MoveSpeed], 2f, 14f);
            flat[StatType.AttackSpeed] = Mathf.Clamp(flat[StatType.AttackSpeed], 0.4f, 3.5f);
            flat[StatType.CritChance] = Mathf.Clamp01(flat[StatType.CritChance]);
            flat[StatType.CritDamage] = Mathf.Max(1f, flat[StatType.CritDamage]);
            flat[StatType.LifeSteal] = Mathf.Clamp(flat[StatType.LifeSteal], 0f, 0.5f);
            flat[StatType.DashCooldown] = Mathf.Clamp(flat[StatType.DashCooldown], 0.25f, 5f);
            flat[StatType.Knockback] = Mathf.Max(0f, flat[StatType.Knockback]);
            return flat;
        }
    }

    /// <summary>Valeurs de base d'un pantin nu (éditables dans GameConfig).</summary>
    [Serializable]
    public class BaseStats
    {
        public float maxHealth = 75f;
        public float damage = 8f;
        public float armor = 0f;
        public float moveSpeed = 5.5f;
        public float attackSpeed = 1f;
        public float critChance = 0.05f;
        public float critDamage = 1.6f;
        public float lifeSteal = 0f;
        public float dashCooldown = 1.1f;
        public float knockback = 4f;

        public StatBlock ToBlock(float scale = 1f)
        {
            var b = new StatBlock();
            b[StatType.MaxHealth] = maxHealth * scale;
            b[StatType.Damage] = damage * scale;
            b[StatType.Armor] = armor * scale;
            b[StatType.MoveSpeed] = moveSpeed;
            b[StatType.AttackSpeed] = attackSpeed;
            b[StatType.CritChance] = critChance;
            b[StatType.CritDamage] = critDamage;
            b[StatType.LifeSteal] = lifeSteal;
            b[StatType.DashCooldown] = dashCooldown;
            b[StatType.Knockback] = knockback;
            return b;
        }
    }
}
