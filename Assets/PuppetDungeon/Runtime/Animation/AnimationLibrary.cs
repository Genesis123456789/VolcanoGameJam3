using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Liste des FBX d'animation à cuire en clips Humanoid (Generated/Animation/Baked).
    /// Déposez un FBX dans Assets/Projet/Animation : il est ajouté automatiquement à la prochaine cuisson.
    /// </summary>
    [CreateAssetMenu(fileName = "PD_AnimationLibrary", menuName = "Puppet Dungeon/Animation/Bibliothèque d'animations", order = 32)]
    public class AnimationLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("FBX source (contrôleurs ou articulations animés).")]
            public GameObject source;
            [Tooltip("Nom du clip généré (PD_<nom>).")]
            public string clipName;
            public bool loop;
            [Tooltip("Décocher pour ignorer ce fichier (ex : fichier de pose de repos).")]
            public bool bake = true;
            [Tooltip("Profil de rig (vide = profil par défaut de la bibliothèque).")]
            public AnimationRigProfile profile;
            [Header("Résultat")]
            public AnimationClip result;
        }

        public AnimationRigProfile defaultProfile;
        public List<Entry> entries = new List<Entry>();

        public AnimationClip Get(string clipName)
        {
            foreach (var e in entries)
                if (e != null && e.result != null && string.Equals(e.clipName, clipName, StringComparison.OrdinalIgnoreCase)) return e.result;
            return null;
        }
    }
}
