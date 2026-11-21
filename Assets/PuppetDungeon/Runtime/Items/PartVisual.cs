using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Visuel d'une pièce. Contient un ou plusieurs groupes de maillages, chacun destiné à un os
    /// humanoïde. Les poses locales ont été calculées par l'outil d'éditeur à partir de la pose de
    /// liaison du rig : il suffit de parenter chaque groupe à son os avec une transformation identité.
    /// </summary>
    public class PartVisual : MonoBehaviour
    {
        [Serializable]
        public class AttachGroup
        {
            public HumanBodyBones bone;
            public Transform root;
        }

        public List<AttachGroup> groups = new List<AttachGroup>();
        [Tooltip("Os dont les segments du pantin de base sont cachés quand la pièce est portée.")]
        public List<HumanBodyBones> coveredBones = new List<HumanBodyBones>();

        private readonly List<Renderer> renderers = new List<Renderer>();
        private bool attached;

        public IReadOnlyList<Renderer> Renderers
        {
            get
            {
                if (renderers.Count == 0) CollectRenderers();
                return renderers;
            }
        }

        private void CollectRenderers()
        {
            renderers.Clear();
            foreach (var g in groups)
                if (g != null && g.root != null)
                    renderers.AddRange(g.root.GetComponentsInChildren<Renderer>(true));
        }

        /// <summary>Parente chaque groupe à l'os correspondant de l'Animator (humanoïde).</summary>
        public bool AttachTo(Animator animator)
        {
            if (animator == null || !animator.isHuman) return false;
            CollectRenderers();
            foreach (var g in groups)
            {
                if (g == null || g.root == null) continue;
                Transform bone = animator.GetBoneTransform(g.bone);
                if (bone == null) bone = FallbackBone(animator, g.bone);
                if (bone == null) continue;
                g.root.SetParent(bone, false);
                g.root.localPosition = Vector3.zero;
                g.root.localRotation = Quaternion.identity;
                g.root.localScale = Vector3.one;
                SetLayerRecursive(g.root, animator.gameObject.layer);
            }
            attached = true;
            return true;
        }

        private static Transform FallbackBone(Animator a, HumanBodyBones b)
        {
            // Si un os optionnel (Chest, Neck…) n'est pas mappé, on remonte vers un parent fiable.
            switch (b)
            {
                case HumanBodyBones.UpperChest:
                case HumanBodyBones.Chest: return a.GetBoneTransform(HumanBodyBones.Spine);
                case HumanBodyBones.Neck: return a.GetBoneTransform(HumanBodyBones.Head);
                default: return a.GetBoneTransform(HumanBodyBones.Hips);
            }
        }

        /// <summary>Détruit le visuel (les groupes peuvent être parentés ailleurs).</summary>
        public void DestroyVisual()
        {
            foreach (var g in groups)
                if (g != null && g.root != null) Destroy(g.root.gameObject);
            Destroy(gameObject);
        }

        /// <summary>Détache tous les maillages et les transforme en débris physiques (mort / sacrifice).</summary>
        public void Shatter(Vector3 origin, float force)
        {
            foreach (var r in Renderers)
            {
                if (r == null) continue;
                Juice.MakeDebris(r.gameObject, origin, force);
            }
        }

        public static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayerRecursive(t.GetChild(i), layer);
        }

        private void OnDestroy()
        {
            if (!attached) return;
            foreach (var g in groups)
                if (g != null && g.root != null && g.root.parent != transform)
                    Destroy(g.root.gameObject);
        }
    }
}
