using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Définition d'un type d'ennemi. Le prefab porte EnemyController + Health.</summary>
    [CreateAssetMenu(fileName = "PD_Enemy_New", menuName = "Puppet Dungeon/Enemy Definition", order = 1)]
    public class EnemyDefinition : ScriptableObject
    {
        public string displayName = "Pantin errant";
        public EnemyController prefab;
        [Tooltip("Jeu d'animations (attaques propres à son arme). Vide = jeu par défaut.")]
        public PuppetAnimationSet animations;
        public EnemyBehaviour behaviour = EnemyBehaviour.Chaser;

        [Header("Stats (donjon 0)")]
        public float maxHealth = 30f;
        public float damage = 8f;
        public float armor = 0f;
        public float moveSpeed = 3.2f;

        [Header("Attaque")]
        public float attackRange = 1.6f;
        public float attackCooldown = 1.4f;
        [Tooltip("Temps d'armement (télégraphe) avant que le coup parte.")]
        public float windup = 0.45f;
        public float preferredDistance = 0f;
        public float projectileSpeed = 9f;
        public int projectilesPerShot = 1;
        public float projectileSpread = 0f;
        public float chargeSpeed = 11f;

        [Header("Apparition")]
        [Min(0f)] public float spawnWeight = 1f;
        [Min(0)] public int minDungeon = 0;
        public bool isBoss = false;
        public float visualScale = 1f;
        [Tooltip("Bonus de rareté du butin (en nombre de crans).")]
        public int lootRarityBonus = 0;
        public float lootChanceMultiplier = 1f;

        [Header("Histoire")]
        [TextArea(2, 6)] public string bossTitle = "";
    }
}
