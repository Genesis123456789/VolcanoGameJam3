using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Prefabs d'effets visuels (créés par l'outil de setup).</summary>
    [CreateAssetMenu(fileName = "PD_VfxLibrary", menuName = "Puppet Dungeon/VFX Library", order = 4)]
    public class VfxLibrary : ScriptableObject
    {
        public ParticleSystem hitSpark;
        public ParticleSystem woodChips;
        public ParticleSystem dust;
        public ParticleSystem doorSlam;
        public ParticleSystem lootBurst;
        public ParticleSystem healPuff;
        public ParticleSystem splinterNova;
        public ParticleSystem swingTrail;
        public GameObject telegraphCircle;
        public GameObject safeZoneRing;
        public GameObject alertMark;
        public DamageNumber damageNumber;
    }
}
