using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    [System.Serializable]
    public class SoundCue
    {
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 0.8f;
        public Vector2 pitch = new Vector2(0.92f, 1.08f);

        public bool IsValid => clips != null && clips.Length > 0;
    }
    /// <summary>Sons et musiques. Les emplacements vides sont simplement ignorés.</summary>
    [CreateAssetMenu(fileName = "PD_AudioLibrary", menuName = "Puppet Dungeon/Audio Library", order = 5)]
    public class AudioLibrary : ScriptableObject
    {
        [Header("Musiques")]
        public AudioClip musicDungeon;
        public AudioClip musicCombat;
        public AudioClip musicBoss;
        public AudioClip musicVillage;
        public AudioClip musicMenu;
        [Range(0f, 1f)] public float musicVolume = 0.55f;

        [Header("Combat")]
        public SoundCue swing = new SoundCue();
        public SoundCue hit = new SoundCue();
        public SoundCue crit = new SoundCue();
        public SoundCue playerHurt = new SoundCue();
        public SoundCue enemyDeath = new SoundCue();
        public SoundCue shoot = new SoundCue();
        public SoundCue dash = new SoundCue();
        public SoundCue footstep = new SoundCue();

        [Header("Monde")]
        public SoundCue doorSlam = new SoundCue();
        public SoundCue doorOpen = new SoundCue();
        public SoundCue alert = new SoundCue();
        public SoundCue roomCleared = new SoundCue();
        public SoundCue lootDrop = new SoundCue();
        public SoundCue lootPickup = new SoundCue();
        public SoundCue legendaryDrop = new SoundCue();

        [Header("Interface")]
        public SoundCue uiClick = new SoundCue();
        public SoundCue uiHover = new SoundCue();
        public SoundCue equip = new SoundCue();
        public SoundCue unequip = new SoundCue();
        public SoundCue offering = new SoundCue();
    }
}
