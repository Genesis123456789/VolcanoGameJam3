using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Décrit un rig d'animation exporté sans « Bake Animation » : seuls les contrôleurs sont animés,
    /// les articulations suivent des contraintes Maya (perdues à l'export). L'outil de cuisson
    /// (Tools > Puppet Dungeon > Animations) rejoue ces contraintes pour reconstruire le mouvement
    /// des articulations, puis enregistre un clip Humanoid.
    /// Si vos FBX sont exportés avec « Bake Animation », ce profil n'est pas nécessaire : les articulations
    /// déjà animées sont reprises telles quelles.
    /// Important : à l'import, Unity place les contrôleurs animés sur la première image de la prise alors que
    /// les articulations restent dans la pose de l'export. Les décalages des contraintes doivent donc être
    /// mesurés dans Maya (ou dans le FBX) et saisis ici (« Stored »), pas mesurés dans Unity.
    /// </summary>
    [CreateAssetMenu(fileName = "PD_RigProfile_", menuName = "Puppet Dungeon/Animation/Profil de rig", order = 31)]
    public class AnimationRigProfile : ScriptableObject
    {
        [Serializable]
        public class BoneName
        {
            public HumanBodyBones bone;
            [Tooltip("Nom exact de l'articulation (sans l'espace de noms Maya « rig_0010: »).")]
            public string name;
        }

        [Serializable]
        public class ControlDriver
        {
            [Tooltip("Articulation pilotée.")]
            public HumanBodyBones bone;
            [Tooltip("Contrôleur : noms séparés par « / » (ex : OFFSET2_Hips/nurbsCircle24). Espaces de noms ignorés.")]
            public string control;
            [Tooltip("Contrainte « parent » (position + rotation). Sinon : orientation seule.")]
            public bool followPosition;
            [Tooltip("Utiliser le décalage mesuré dans Maya ci-dessous (recommandé). Sinon il est mesuré dans la pose importée, ce qui n'est fiable que si contrôleurs et articulations y sont cohérents.")]
            public bool useStoredOffset;
            [Tooltip("Rotation articulation / contrôleur (axes Maya, quaternion x y z w).")]
            public Quaternion storedRotation = Quaternion.identity;
            [Tooltip("Position de l'articulation dans le repère du contrôleur (axes et unités Maya).")]
            public Vector3 storedPosition;
        }

        [Serializable]
        public class IkLimb
        {
            public HumanBodyBones upper = HumanBodyBones.LeftUpperLeg;
            public HumanBodyBones lower = HumanBodyBones.LeftLowerLeg;
            public HumanBodyBones end = HumanBodyBones.LeftFoot;
            [Tooltip("Contrôleur cible de l'IK (pied).")]
            public string goal;
            [Tooltip("Contrôleur du « pole vector » (genou). Son pivot définit le plan de la jambe.")]
            public string pole;
            [Tooltip("Utiliser les valeurs mesurées dans Maya ci-dessous.")]
            public bool useStoredOffset;
            [Tooltip("Position du pied dans le repère du contrôleur cible (axes et unités Maya).")]
            public Vector3 storedGoal;
            [Tooltip("Pivot de rotation du contrôleur de genou (axes et unités Maya) : c'est ce point que vise le pole vector.")]
            public Vector3 polePivot;
        }

        [Tooltip("Modèle où le squelette est en pose de repos (sert à construire l'avatar humanoïde du rig). Vide = pose du fichier cuit.")]
        public GameObject restPose;
        [Tooltip("Noms d'articulations à forcer (sinon : convention Hips/Spine/LeftArm/LeftForeArm/LeftUpLeg…).")]
        public List<BoneName> boneNames = new List<BoneName>();
        public List<ControlDriver> drivers = new List<ControlDriver>();
        public List<IkLimb> ikLimbs = new List<IkLimb>();
        [Tooltip("Articulation de référence pour convertir les unités Maya → Unity (longueur de son déplacement local dans Maya).")]
        public HumanBodyBones unitReferenceBone = HumanBodyBones.Spine;
        public float unitReferenceLength;
        [HideInInspector] public int dataVersion;
        [Tooltip("Images par seconde des clips cuits.")]
        [Range(10, 60)] public int sampleRate = 30;

        [Header("Généré")]
        public Avatar avatar;
    }
}
