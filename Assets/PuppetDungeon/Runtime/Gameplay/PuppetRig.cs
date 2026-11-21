using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Pantin de base (rig humanoïde). Sait cacher les segments remplacés par des pièces et
    /// accroche les visuels des pièces équipées sur les os. Utilisé par le joueur, les villageois
    /// et les ennemis.
    /// </summary>
    public class PuppetRig : MonoBehaviour
    {
        [Serializable]
        public class BaseSegment
        {
            public Renderer renderer;
            public HumanBodyBones bone;
        }

        public Animator animator;
        [Tooltip("Segments du corps de base, avec l'os qui les porte (calculé par l'outil d'éditeur).")]
        public List<BaseSegment> baseSegments = new List<BaseSegment>();
        [Tooltip("Visuels décoratifs accrochés au démarrage (ex : arbalète d'un ennemi).")]
        public List<PartVisual> decorations = new List<PartVisual>();

        private readonly Dictionary<PuppetSlot, PartVisual> equippedVisuals = new Dictionary<PuppetSlot, PartVisual>();
        private readonly List<PartVisual> extraVisuals = new List<PartVisual>();
        private HitFlash flash;

        public Animator Animator => animator;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            flash = GetComponentInParent<HitFlash>();
            foreach (var d in decorations)
            {
                var v = AddExtraVisual(d);
                if (v == null || !v.coveredBones.Any()) continue;
                foreach (var s in baseSegments)
                    if (s.renderer != null && v.coveredBones.Contains(s.bone) && IsReplaced(s.bone, v)) s.renderer.enabled = false;
            }
        }

        private static bool IsReplaced(HumanBodyBones bone, PartVisual v)
        {
            // Les décors d'ennemis remplacent les avant-bras / jambes qu'ils couvrent.
            return bone != HumanBodyBones.Hips && bone != HumanBodyBones.Spine && bone != HumanBodyBones.Chest && bone != HumanBodyBones.Head;
        }

        public void SetWoodTint(Color tint)
        {
            if (flash == null) flash = GetComponentInParent<HitFlash>();
            if (flash == null) return;
            var list = new List<Renderer>();
            foreach (var s in baseSegments) if (s.renderer != null) list.Add(s.renderer);
            flash.SetTint(list, tint, Color.black);
        }

        /// <summary>Reconstruit les visuels à partir des pièces équipées (6 emplacements).</summary>
        public void ApplyLoadout(PartInstance[] equipped, GameConfig cfg)
        {
            var covered = new HashSet<HumanBodyBones>();
            for (int i = 0; i < 6; i++)
            {
                var slot = (PuppetSlot)i;
                PartInstance part = equipped != null && i < equipped.Length ? equipped[i] : null;
                equippedVisuals.TryGetValue(slot, out var current);
                var wantedPrefab = part != null && part.definition != null ? part.definition.visualPrefab : null;

                if (current != null && (wantedPrefab == null || current.name != wantedPrefab.name + "_" + slot))
                {
                    current.DestroyVisual();
                    equippedVisuals.Remove(slot);
                    current = null;
                }
                if (current == null && wantedPrefab != null)
                {
                    current = Instantiate(wantedPrefab, transform);
                    current.name = wantedPrefab.name + "_" + slot;
                    current.AttachTo(animator);
                    equippedVisuals[slot] = current;
                }
                if (current != null && part.definition.replaceBaseSegments)
                    foreach (var b in current.coveredBones) covered.Add(b);
            }

            foreach (var s in baseSegments)
                if (s.renderer != null) s.renderer.enabled = !covered.Contains(s.bone);

            if (flash == null) flash = GetComponentInParent<HitFlash>();
            if (flash != null)
            {
                flash.Refresh();
                for (int i = 0; i < 6; i++)
                {
                    PartInstance part = equipped != null && i < equipped.Length ? equipped[i] : null;
                    if (part == null || !equippedVisuals.TryGetValue((PuppetSlot)i, out var vis)) continue;
                    Color glow = Color.black;
                    if (cfg != null && part.rarity >= Rarity.Epic)
                        glow = cfg.RarityColor(part.rarity) * (part.rarity == Rarity.Legendary ? 0.55f : 0.3f);
                    flash.SetTint(vis.Renderers, Color.white, glow);
                }
            }
        }

        /// <summary>Ajoute un visuel décoratif permanent (ennemis : arbalète, bouclier…).</summary>
        public PartVisual AddExtraVisual(PartVisual prefab)
        {
            if (prefab == null) return null;
            var v = Instantiate(prefab, transform);
            v.AttachTo(animator);
            extraVisuals.Add(v);
            if (flash == null) flash = GetComponentInParent<HitFlash>();
            if (flash != null) flash.Refresh();
            return v;
        }

        /// <summary>Le pantin se disloque : chaque segment devient un débris.</summary>
        public void Shatter(Vector3 origin, float force)
        {
            foreach (var v in equippedVisuals.Values) if (v != null) v.Shatter(origin, force);
            foreach (var v in extraVisuals) if (v != null) v.Shatter(origin, force);
            foreach (var s in baseSegments)
                if (s.renderer != null && s.renderer.enabled) Juice.MakeDebris(s.renderer.gameObject, origin, force);
            if (animator != null) animator.enabled = false;
        }

        public Transform Bone(HumanBodyBones b)
        {
            if (animator == null || !animator.isHuman) return transform;
            var t = animator.GetBoneTransform(b);
            return t != null ? t : transform;
        }
    }
}
