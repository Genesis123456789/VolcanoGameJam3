using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Liste des pièces disponibles en butin. Remplie automatiquement par l'éditeur.</summary>
    [CreateAssetMenu(fileName = "PD_PartDatabase", menuName = "Puppet Dungeon/Part Database", order = 3)]
    public class PartDatabase : ScriptableObject
    {
        public List<PartDefinition> parts = new List<PartDefinition>();

        public IEnumerable<PartDefinition> Droppable(int dungeonIndex, Rarity rarity)
        {
            foreach (var p in parts)
            {
                if (p == null || p.starterOnly) continue;
                if (p.minDungeon > dungeonIndex) continue;
                if (rarity < p.minRarity || rarity > p.maxRarity) continue;
                if (p.dropWeight <= 0f) continue;
                yield return p;
            }
        }
    }
}
