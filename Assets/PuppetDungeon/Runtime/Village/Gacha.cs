using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Gacha du village : les pièces non offertes sont recyclées en monnaie (selon leur rareté),
    /// la Tisseuse échange cette monnaie contre des tirages de pièces. Tous les réglages sont dans
    /// PD_GameConfig (section « Gacha du village »).
    /// </summary>
    public static class Gacha
    {
        public struct Result
        {
            public PartInstance part;
            public bool recycled;   // sac plein : la pièce a été rendue en monnaie
            public int refund;
            public bool pity;       // obtenue grâce à la garantie
        }

        private static GameConfig Cfg => GameManager.Instance.Config;
        private static RunState Run => GameManager.Instance.Run;

        public static int Currency => GameManager.Instance != null && GameManager.Instance.Run != null ? GameManager.Instance.Run.currency : 0;

        public static int ValueOf(IEnumerable<PartInstance> parts)
        {
            int total = 0;
            if (parts == null) return 0;
            foreach (var p in parts) total += Cfg.CurrencyValue(p);
            return total;
        }

        /// <summary>Recycle des pièces : ajoute leur valeur à la monnaie. Retourne le gain.</summary>
        public static int Recycle(IEnumerable<PartInstance> parts)
        {
            int gain = 0, count = 0;
            foreach (var p in parts)
            {
                if (p == null) continue;
                gain += Cfg.CurrencyValue(p);
                count++;
            }
            Run.currency += gain;
            Run.partsRecycled += count;
            return gain;
        }

        public static int Cost(bool multi) => multi ? Cfg.GachaMultiCost(Run.dungeonIndex) : Cfg.GachaCost(Run.dungeonIndex);
        public static int Count(bool multi) => multi ? Mathf.Max(2, Cfg.gachaMultiCount) : 1;
        public static bool CanAfford(bool multi) => Run.currency >= Cost(multi);

        /// <summary>Tirages restants avant la garantie Épique (0 si désactivée).</summary>
        public static int PullsBeforePity => Cfg.gachaPity <= 0 ? 0 : Mathf.Max(1, Cfg.gachaPity - Run.gachaSincePity);

        /// <summary>Probabilité (0-1) de chaque rareté, pour l'affichage.</summary>
        public static float[] Odds()
        {
            var w = Cfg.gachaRarityWeights;
            int n = System.Enum.GetValues(typeof(Rarity)).Length;
            var o = new float[n];
            float total = 0f;
            for (int i = 0; i < n; i++) total += i < w.Length ? Mathf.Max(0f, w[i]) : 0f;
            for (int i = 0; i < n; i++) o[i] = total > 0f && i < w.Length ? Mathf.Max(0f, w[i]) / total : 0f;
            return o;
        }

        /// <summary>Effectue un tirage (simple ou multiple). Retourne null si la monnaie manque.</summary>
        public static List<Result> Pull(bool multi)
        {
            int cost = Cost(multi);
            if (Run.currency < cost) return null;
            Run.currency -= cost;
            int count = Count(multi);
            var results = new List<Result>();
            var odds = Odds();
            int dungeon = Run.dungeonIndex;
            for (int i = 0; i < count; i++)
            {
                bool pity = Cfg.gachaPity > 0 && Run.gachaSincePity >= Cfg.gachaPity - 1;
                Rarity r = pity ? (Random.value < 0.2f ? Rarity.Legendary : Rarity.Epic) : RollRarity(odds);
                var def = LootGenerator.PickDefinition(Cfg, dungeon, r);
                if (def == null) continue;
                r = (Rarity)Mathf.Clamp((int)r, (int)def.minRarity, (int)def.maxRarity);
                var part = LootGenerator.Create(Cfg, def, r, dungeon + 1);
                Run.gachaPulls++;
                if (r >= Rarity.Epic) Run.gachaSincePity = 0; else Run.gachaSincePity++;

                var res = new Result { part = part, pity = pity };
                if (!Run.inventory.TryAdd(part))
                {
                    res.recycled = true;
                    res.refund = Mathf.RoundToInt(cost / (float)count * Cfg.gachaFullBagRefund);
                    Run.currency += res.refund;
                }
                results.Add(res);
            }
            return results;
        }

        private static Rarity RollRarity(float[] odds)
        {
            float roll = Random.value;
            for (int i = 0; i < odds.Length; i++)
            {
                if (roll < odds[i]) return (Rarity)i;
                roll -= odds[i];
            }
            return Rarity.Common;
        }
    }
}
