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
        [Tooltip("Jeu d'animations par défaut (remplacé selon l'arme équipée).")]
        public PuppetAnimationSet animationSet;
        [Tooltip("Segments du corps de base, avec l'os qui les porte (calculé par l'outil d'éditeur).")]
        public List<BaseSegment> baseSegments = new List<BaseSegment>();
        [Tooltip("Visuels décoratifs accrochés au démarrage (ex : arbalète d'un ennemi).")]
        public List<PartVisual> decorations = new List<PartVisual>();

        private readonly Dictionary<PuppetSlot, PartVisual> equippedVisuals = new Dictionary<PuppetSlot, PartVisual>();
        private readonly List<PartVisual> extraVisuals = new List<PartVisual>();
        private HitFlash flash;

        public Animator Animator => animator;
        public PuppetAnimationSet CurrentAnimations { get; private set; }

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int AttackStillId = Animator.StringToHash("AttackStill");
        private static readonly int AttackSpeedId = Animator.StringToHash("AttackSpeed");
        private static readonly int GuardId = Animator.StringToHash("Guard");
        private static readonly int CelebrateId = Animator.StringToHash("Celebrate");
        private static readonly int HitId = Animator.StringToHash("Hit");

        private float busyUntil;
        private static readonly int EmptyStateId = Animator.StringToHash("Vide");

        /// <summary>
        /// Calque « haut du corps » : il ne pèse que lorsqu'il joue quelque chose (attaque en marchant, garde,
        /// réaction, célébration). Le reste du temps son poids retombe à 0, pour que l'idle et l'attaque sur
        /// place (calque de base, corps entier) gardent leurs bras.
        /// </summary>
        private void Update()
        {
            if (!CanAnimate || animator.layerCount < 2) return;
            bool inTransition = animator.IsInTransition(1);
            bool resting = inTransition
                ? animator.GetNextAnimatorStateInfo(1).shortNameHash == EmptyStateId
                : animator.GetCurrentAnimatorStateInfo(1).shortNameHash == EmptyStateId;
            float w = animator.GetLayerWeight(1);
            float target = resting ? 0f : 1f;
            if (!Mathf.Approximately(w, target))
                animator.SetLayerWeight(1, Mathf.MoveTowards(w, target, Time.deltaTime / (resting ? 0.15f : 0.05f)));
        }

        private bool CanAnimate => animator != null && animator.enabled && animator.runtimeAnimatorController != null && animator.isActiveAndEnabled;

        /// <summary>Change de jeu d'animations (ex : nouvelle arme). Sans effet si c'est déjà le bon.</summary>
        public void UseAnimations(PuppetAnimationSet set)
        {
            if (set == null || animator == null) return;
            if (CurrentAnimations == set && animator.runtimeAnimatorController == set.controller) return;
            CurrentAnimations = set;
            if (set.controller != null && animator.runtimeAnimatorController != set.controller)
                animator.runtimeAnimatorController = set.controller;
        }

        /// <summary>Vitesse de locomotion normalisée (0 = idle, ~0.35 = marche, 1 = course).</summary>
        public void SetSpeed(float speed01, float damp = 0.08f)
        {
            if (!CanAnimate) return;
            if (damp > 0f) animator.SetFloat(SpeedId, Mathf.Clamp01(speed01), damp, Time.deltaTime);
            else animator.SetFloat(SpeedId, Mathf.Clamp01(speed01));
        }

        /// <summary>
        /// Lance l'animation d'attaque. moving : haut du corps seulement (la marche continue dessous),
        /// sinon attaque en corps entier. duration : temps de jeu visé (la vitesse de lecture s'adapte).
        /// </summary>
        public void PlayAttack(bool moving, float duration)
        {
            if (!CanAnimate) return;
            var set = CurrentAnimations != null ? CurrentAnimations : animationSet;
            float length = set != null ? set.AttackLength(moving) : 0.8f;
            float playback = set != null ? set.attackPlaybackSpeed : 1f;
            float speed = Mathf.Clamp((duration > 0.01f ? length / duration : 1f) * playback, 0.8f, 2.5f);
            animator.SetFloat(AttackSpeedId, speed);
            animator.ResetTrigger(moving ? AttackStillId : AttackId);
            animator.SetTrigger(moving ? AttackId : AttackStillId);
            // La garde ne doit pas recouvrir l'attaque tant que son animation n'est pas terminée.
            busyUntil = Time.time + length / speed * 0.9f;
            animator.SetBool(GuardId, false);
        }

        /// <summary>Garde / visée tenue (haut du corps).</summary>
        public void SetGuard(bool on)
        {
            if (!CanAnimate) return;
            if (on && Time.time < busyUntil) on = false;
            animator.SetBool(GuardId, on);
        }

        /// <summary>Réaction au coup reçu (haut du corps : les jambes continuent de marcher).</summary>
        public void PlayHit()
        {
            if (!CanAnimate || Time.time < busyUntil) return;
            animator.SetTrigger(HitId);
        }

        public void Celebrate()
        {
            if (!CanAnimate) return;
            animator.SetTrigger(CelebrateId);
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animationSet != null) CurrentAnimations = animationSet;
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
