using System;
using UnityEngine;

namespace PuppetDungeon
{
    public struct DamageInfo
    {
        public float amount;
        public bool crit;
        public Vector3 point;
        public Vector3 direction;
        public float knockback;
        public GameObject source;
        public bool ignoreArmor;
        public bool isReflect;
    }

    /// <summary>Points de vie communs au joueur et aux ennemis.</summary>
    public class Health : MonoBehaviour
    {
        public float maxHealth = 30f;
        public float armor = 0f;
        [Tooltip("Durée d'invulnérabilité après un coup (joueur).")]
        public float invulnerabilityAfterHit = 0f;

        public float Current { get; private set; }
        public bool IsDead { get; private set; }
        public float Normalized => maxHealth > 0f ? Current / maxHealth : 0f;
        public bool Invulnerable => Time.time < invulnerableUntil || forceInvulnerable;

        public event Action<DamageInfo, float> Damaged;   // info, dégâts réellement subis
        public event Action<float> Healed;
        public event Action<DamageInfo> Died;
        public event Action Changed;

        /// <summary>Intercepteur optionnel (ex : vernis, bouclier) qui peut réduire les dégâts.</summary>
        public Func<DamageInfo, float, float> DamageFilter;

        private float invulnerableUntil;
        private bool forceInvulnerable;

        private void Awake()
        {
            if (Current <= 0f && !IsDead) Current = maxHealth;
        }

        public void Init(float max, float current = -1f)
        {
            maxHealth = Mathf.Max(1f, max);
            Current = current < 0f ? maxHealth : Mathf.Clamp(current, 1f, maxHealth);
            IsDead = false;
            Changed?.Invoke();
        }

        public void SetMax(float max, bool keepRatio)
        {
            float ratio = Normalized;
            maxHealth = Mathf.Max(1f, max);
            Current = keepRatio ? Mathf.Max(1f, ratio * maxHealth) : Mathf.Min(Current, maxHealth);
            Changed?.Invoke();
        }

        public void SetInvulnerable(bool on) => forceInvulnerable = on;
        public void GrantInvulnerability(float duration) => invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration);

        /// <summary>Applique des dégâts. Retourne les dégâts effectivement subis.</summary>
        public float TakeDamage(DamageInfo info)
        {
            if (IsDead || Invulnerable) return 0f;
            float dmg = info.ignoreArmor ? info.amount : Mathf.Max(1f, info.amount - armor);
            if (DamageFilter != null) dmg = DamageFilter(info, dmg);
            if (dmg <= 0f) return 0f;
            Current = Mathf.Max(0f, Current - dmg);
            if (invulnerabilityAfterHit > 0f) GrantInvulnerability(invulnerabilityAfterHit);
            Damaged?.Invoke(info, dmg);
            Changed?.Invoke();
            if (Current <= 0f)
            {
                IsDead = true;
                Died?.Invoke(info);
            }
            return dmg;
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            float before = Current;
            Current = Mathf.Min(maxHealth, Current + amount);
            if (Current > before)
            {
                Healed?.Invoke(Current - before);
                Changed?.Invoke();
            }
        }

        public void Kill()
        {
            if (IsDead) return;
            Current = 0f;
            IsDead = true;
            Changed?.Invoke();
            Died?.Invoke(new DamageInfo { amount = maxHealth, point = transform.position });
        }
    }
}
