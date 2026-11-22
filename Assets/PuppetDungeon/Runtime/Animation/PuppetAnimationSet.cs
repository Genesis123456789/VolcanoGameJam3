using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Jeu d'animations d'un pantin (un par style d'arme, et un par défaut).
    /// L'outil de setup génère pour chaque jeu un AnimatorOverrideController basé sur PD_Puppet.controller :
    /// changer d'arme = changer de contrôleur, sans dupliquer la machine à états.
    /// Un emplacement vide reprend le clip du jeu par défaut.
    /// </summary>
    [CreateAssetMenu(fileName = "PD_Anim_", menuName = "Puppet Dungeon/Animation/Jeu d'animations", order = 30)]
    public class PuppetAnimationSet : ScriptableObject
    {
        [Tooltip("Nom lisible (affiché dans les journaux).")]
        public string label = "Défaut";

        [Header("Locomotion (corps entier, mélange sur « Speed »)")]
        public AnimationClip idle;
        public AnimationClip walk;
        public AnimationClip run;

        [Header("Attaques")]
        [Tooltip("Haut du corps : jouée en se déplaçant, mélangée avec la marche/course (masque d'avatar).")]
        public AnimationClip attack;
        [Tooltip("Corps entier : jouée à l'arrêt. Vide = même clip que l'attaque.")]
        public AnimationClip attackStill;
        [Tooltip("Vitesse de lecture de base des attaques (multipliée par la vitesse d'attaque du pantin).")]
        [Min(0.1f)] public float attackPlaybackSpeed = 1f;

        [Header("Postures (haut du corps, optionnelles)")]
        [Tooltip("Garde / visée tenue quand le pantin est immobile en combat.")]
        public AnimationClip guard;
        [Tooltip("Célébration quand une salle est nettoyée.")]
        public AnimationClip celebrate;
        [Tooltip("Réaction quand le pantin est touché (haut du corps).")]
        public AnimationClip hit;
        [HideInInspector] public int dataVersion;

        [Header("Généré par l'outil (ne pas modifier)")]
        public AnimatorOverrideController controller;

        public AnimationClip AttackClip(bool moving) => moving ? (attack != null ? attack : attackStill) : (attackStill != null ? attackStill : attack);
        public float AttackLength(bool moving) { var c = AttackClip(moving); return c != null ? c.length : 0.8f; }
    }
}
