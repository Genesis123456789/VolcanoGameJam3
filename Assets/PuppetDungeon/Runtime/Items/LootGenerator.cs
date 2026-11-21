using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PuppetDungeon
{
    /// <summary>Tirage des pièces : rareté pondérée, stats mises à l'échelle, passifs.</summary>
    public static class LootGenerator
    {
        private static readonly StatType[] ExtraStatPool =
        {
            StatType.MaxHealth, StatType.Damage, StatType.Armor, StatType.MoveSpeed, StatType.AttackSpeed,
            StatType.CritChance, StatType.CritDamage, StatType.LifeSteal, StatType.Knockback
        };

        private static readonly PassiveKind[] GenericPassives =
        {
            PassiveKind.Splinters, PassiveKind.Sap, PassiveKind.Thorns, PassiveKind.SecondWind, PassiveKind.Frenzy,
            PassiveKind.Momentum, PassiveKind.Executioner, PassiveKind.Varnish, PassiveKind.StringDash, PassiveKind.LuckyCharm
        };

        public static Rarity RollRarity(GameConfig cfg, int dungeonIndex, int rarityBonus = 0, float luck = 0f)
        {
            int n = Enum.GetValues(typeof(Rarity)).Length;
            var w = new float[n];
            float shift = cfg.rarityShiftPerDungeon * dungeonIndex + luck;
            float total = 0f;
            for (int i = 0; i < n; i++)
            {
                float baseW = i < cfg.rarityWeights.Length ? cfg.rarityWeights[i] : 0.1f;
                // Les raretés hautes gagnent du poids avec la profondeur, les basses en perdent.
                float f = Mathf.Pow(1f + shift, i - 1.5f);
                w[i] = Mathf.Max(0f, baseW * f);
                total += w[i];
            }
            float roll = Random.value * total;
            int picked = 0;
            for (int i = 0; i < n; i++)
            {
                if (roll <= w[i]) { picked = i; break; }
                roll -= w[i];
                picked = i;
            }
            return (Rarity)Mathf.Clamp(picked + rarityBonus, 0, n - 1);
        }

        public static PartDefinition PickDefinition(GameConfig cfg, int dungeonIndex, Rarity rarity, PuppetSlot? slot = null)
        {
            if (cfg.partDatabase == null) return null;
            var pool = cfg.partDatabase.Droppable(dungeonIndex, rarity)
                .Where(p => !slot.HasValue || p.slot == slot.Value).ToList();
            if (pool.Count == 0)
                pool = cfg.partDatabase.parts.Where(p => p != null && !p.starterOnly && p.dropWeight > 0f).ToList();
            if (pool.Count == 0) return null;
            float total = pool.Sum(p => p.dropWeight);
            float roll = Random.value * total;
            foreach (var p in pool)
            {
                if (roll <= p.dropWeight) return p;
                roll -= p.dropWeight;
            }
            return pool[pool.Count - 1];
        }

        /// <summary>Tire une pièce complète, ou null si la base de pièces est vide.</summary>
        public static PartInstance Roll(GameConfig cfg, int dungeonIndex, int rarityBonus = 0, float luck = 0f, Rarity? minRarity = null)
        {
            Rarity r = RollRarity(cfg, dungeonIndex, rarityBonus, luck);
            if (minRarity.HasValue && r < minRarity.Value) r = minRarity.Value;
            var def = PickDefinition(cfg, dungeonIndex, r);
            if (def == null) return null;
            r = (Rarity)Mathf.Clamp((int)r, (int)def.minRarity, (int)def.maxRarity);
            return Create(cfg, def, r, dungeonIndex + 1);
        }

        public static PartInstance Create(GameConfig cfg, PartDefinition def, Rarity rarity, int itemLevel)
        {
            var inst = new PartInstance
            {
                uid = Guid.NewGuid().ToString("N"),
                definition = def,
                rarity = rarity,
                itemLevel = Mathf.Max(1, itemLevel),
                foundByGeneration = GameManager.Instance != null ? GameManager.Instance.Run.generation : 0
            };

            int ri = (int)rarity;
            float rarityMul = ri < cfg.rarityStatMultiplier.Length ? cfg.rarityStatMultiplier[ri] : 1f;
            float power = cfg.ItemPowerScale(inst.itemLevel) * rarityMul;

            // Stats de base de la définition
            foreach (var range in def.baseStats)
                inst.stats.Add(new StatModifier(range.stat, range.mode, ScaleValue(range.stat, range.mode, Random.Range(range.min, range.max), power)));

            // Stats supplémentaires selon la rareté
            int extra = ri < cfg.rarityExtraStats.Length ? cfg.rarityExtraStats[ri] : 0;
            var used = new HashSet<StatType>(def.baseStats.Select(s => s.stat));
            for (int i = 0; i < extra; i++)
            {
                var candidates = ExtraStatPool.Where(s => !used.Contains(s)).ToArray();
                if (candidates.Length == 0) break;
                var st = candidates[Random.Range(0, candidates.Length)];
                used.Add(st);
                inst.stats.Add(RollExtraStat(st, power));
            }

            // Passifs
            int passiveCount = ri < cfg.rarityPassiveCount.Length ? cfg.rarityPassiveCount[ri] : 0;
            var pool = (def.possiblePassives != null && def.possiblePassives.Count > 0)
                ? def.possiblePassives.Where(p => p != PassiveKind.None).Distinct().ToList()
                : GenericPassives.ToList();
            for (int i = 0; i < passiveCount && pool.Count > 0; i++)
            {
                var k = pool[Random.Range(0, pool.Count)];
                pool.Remove(k);
                float mag = PassiveCatalog.BaseMagnitude(k) * (1f + 0.25f * (ri - 2)) * Mathf.Lerp(1f, Mathf.Sqrt(power / rarityMul), 0.35f);
                inst.passives.Add(new PassiveRoll(k, Round(mag, 2)));
            }
            return inst;
        }

        private static float ScaleValue(StatType stat, ModifierMode mode, float v, float power)
        {
            // Les pourcentages et ratios ne doivent pas exploser : on atténue leur croissance.
            if (mode == ModifierMode.Percent || EnumUtil.IsRatioStat(stat) || stat == StatType.MoveSpeed || stat == StatType.DashCooldown)
                return Round(v * Mathf.Lerp(1f, power, 0.25f), stat == StatType.MoveSpeed || stat == StatType.DashCooldown ? 2 : 3);
            return Mathf.Round(v * power);
        }

        private static StatModifier RollExtraStat(StatType st, float power)
        {
            switch (st)
            {
                case StatType.MaxHealth: return new StatModifier(st, ModifierMode.Flat, Mathf.Round(Random.Range(6f, 14f) * power));
                case StatType.Damage: return new StatModifier(st, ModifierMode.Flat, Mathf.Round(Random.Range(1.5f, 4f) * power));
                case StatType.Armor: return new StatModifier(st, ModifierMode.Flat, Mathf.Round(Random.Range(1f, 3f) * power));
                case StatType.MoveSpeed: return new StatModifier(st, ModifierMode.Flat, Round(Random.Range(0.2f, 0.6f) * Mathf.Lerp(1f, power, 0.2f), 2));
                case StatType.AttackSpeed: return new StatModifier(st, ModifierMode.Flat, Round(Random.Range(0.04f, 0.1f) * Mathf.Lerp(1f, power, 0.25f), 3));
                case StatType.CritChance: return new StatModifier(st, ModifierMode.Flat, Round(Random.Range(0.02f, 0.06f) * Mathf.Lerp(1f, power, 0.25f), 3));
                case StatType.CritDamage: return new StatModifier(st, ModifierMode.Flat, Round(Random.Range(0.1f, 0.25f) * Mathf.Lerp(1f, power, 0.25f), 3));
                case StatType.LifeSteal: return new StatModifier(st, ModifierMode.Flat, Round(Random.Range(0.01f, 0.03f) * Mathf.Lerp(1f, power, 0.25f), 3));
                default: return new StatModifier(StatType.Knockback, ModifierMode.Flat, Round(Random.Range(0.8f, 2f) * Mathf.Lerp(1f, power, 0.3f), 1));
            }
        }

        private static float Round(float v, int digits)
        {
            float m = Mathf.Pow(10f, digits);
            return Mathf.Round(v * m) / m;
        }
    }
}
