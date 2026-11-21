using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Ambiance visuelle d'un donjon (couleurs, lumière).</summary>
    [CreateAssetMenu(fileName = "PD_Theme_New", menuName = "Puppet Dungeon/Dungeon Theme", order = 2)]
    public class DungeonTheme : ScriptableObject
    {
        public Color floorA = new Color(0.55f, 0.42f, 0.30f);
        public Color floorB = new Color(0.48f, 0.36f, 0.25f);
        public Color wall = new Color(0.36f, 0.27f, 0.22f);
        public Color wallTop = new Color(0.62f, 0.50f, 0.38f);
        public Color accent = new Color(0.85f, 0.55f, 0.25f);
        public Color lightColor = new Color(1f, 0.86f, 0.68f);
        public float lightIntensity = 1.2f;
        public Color lanternColor = new Color(1f, 0.62f, 0.3f);
        public Color ambient = new Color(0.32f, 0.27f, 0.30f);
        public Material floorMaterial;
        public Material wallMaterial;
        public Material wallTopMaterial;
        public Material propMaterial;
    }
}
