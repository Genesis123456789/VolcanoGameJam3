using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Attaques du pantin. Le style dépend de la pièce portée au bras droit :
    /// Poing, Lame (combo de 3), Arbalète (tir), Bouclier (gros recul + garde).
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [System.Serializable]
        public class StyleTuning
        {
            public float range = 1.4f;
            public float arc = 120f;
            public float damageMul = 1f;
            public float knockbackMul = 1f;
            public float duration = 0.32f;
            public float cooldown = 0.08f;
            public float hitDelay = 0.08f;
            public float moveSpeedWhileAttacking = 0.25f;
            public float damageTakenWhileAttacking = 1f;
            public float lunge = 3f;
        }

        public StyleTuning fist = new StyleTuning { range = 1.25f, arc = 100f, damageMul = 0.85f, duration = 0.26f, lunge = 2.5f };
        public StyleTuning blade = new StyleTuning { range = 2.0f, arc = 150f, damageMul = 1f, duration = 0.32f, lunge = 3.5f };
        public StyleTuning crossbow = new StyleTuning { range = 0f, arc = 0f, damageMul = 1.1f, duration = 0.3f, cooldown = 0.25f, moveSpeedWhileAttacking = 0.45f, lunge = 0f };
        public StyleTuning shield = new StyleTuning { range = 1.6f, arc = 130f, damageMul = 0.8f, knockbackMul = 3f, duration = 0.42f, damageTakenWhileAttacking = 0.35f, lunge = 5f };


        public bool IsAttacking { get; private set; }
        public bool WantsAutoRepeat => true;
        public float MoveSpeedWhileAttacking => current != null ? current.moveSpeedWhileAttacking : 1f;
        public float DamageTakenWhileAttacking => current != null ? current.damageTakenWhileAttacking : 1f;

        private PlayerController player;
        private StyleTuning current;
        private float nextAttackTime;
        private int comboStep;
        private float comboResetTime;
        private readonly HashSet<Health> dashHits = new HashSet<Health>();

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        private StatBlock Stats => player.Stats;

        private StyleTuning Tuning(AttackStyle s)
        {
            switch (s)
            {
                case AttackStyle.Blade: return blade;
                case AttackStyle.Crossbow: return crossbow;
                case AttackStyle.Shield: return shield;
                default: return fist;
            }
        }

        public void TryAttack(Vector3 aim)
        {
            if (IsAttacking || Time.time < nextAttackTime || Stats == null) return;
            var style = GameManager.Instance.CurrentAttackStyle();
            StartCoroutine(AttackRoutine(style, aim.sqrMagnitude > 0.001f ? aim.normalized : transform.forward));
        }

        private IEnumerator AttackRoutine(AttackStyle style, Vector3 aim)
        {
            current = Tuning(style);
            IsAttacking = true;
            float atkSpeed = Stats[StatType.AttackSpeed];
            float duration = current.duration / atkSpeed;

            if (Time.time > comboResetTime) comboStep = 0;
            bool finisher = style == AttackStyle.Blade && comboStep == 2;

            player.FaceTowards(aim);
            if (player.rig != null)
            {
                // En mouvement : attaque du haut du corps mélangée à la marche. À l'arrêt : corps entier.
                bool moving = player.Speed01 > 0.15f;
                player.rig.SetGuard(false);
                player.rig.PlayAttack(moving, duration * 1.6f);
            }
            if (player.squash != null) player.squash.Punch(new Vector3(0.1f, -0.12f, 0.25f) * (finisher ? 1.6f : 1f));
            AudioManager.Sfx(l => style == AttackStyle.Crossbow ? l.shoot : l.swing, 0.8f, finisher ? 0.85f : 1f);

            // Petite fente vers l'avant
            if (current.lunge > 0f)
                StartCoroutine(Lunge(aim, current.lunge * (finisher ? 1.4f : 1f), current.hitDelay / atkSpeed + 0.04f));

            yield return new WaitForSeconds(current.hitDelay / atkSpeed);

            if (style == AttackStyle.Crossbow) FireBolt(aim);
            else MeleeHit(style, aim, finisher);

            yield return new WaitForSeconds(Mathf.Max(0.02f, duration - current.hitDelay / atkSpeed));
            IsAttacking = false;
            nextAttackTime = Time.time + current.cooldown / atkSpeed;
            comboStep = (comboStep + 1) % 3;
            comboResetTime = Time.time + 0.6f;
        }

        private IEnumerator Lunge(Vector3 dir, float speed, float duration)
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                if (player.Controller.enabled) player.Controller.Move(dir * speed * (1f - t / duration) * Time.deltaTime);
                yield return null;
            }
        }

        private void MeleeHit(AttackStyle style, Vector3 aim, bool finisher)
        {
            float range = current.range * (finisher ? 1.25f : 1f);
            float arc = current.arc * (finisher ? 1.3f : 1f);
            Vector3 origin = transform.position + Vector3.up * 0.8f;

            var rot = Quaternion.LookRotation(aim, Vector3.up);
            Juice.Spawn(v => v.swingTrail, transform.position + Vector3.up * 0.9f + aim * range * 0.45f, rot,
                style == AttackStyle.Shield ? new Color(0.8f, 0.85f, 1f) : (Color?)null, range * 0.6f * (finisher ? 1.3f : 1f));

            var cols = Physics.OverlapSphere(origin + aim * range * 0.5f, range * 0.75f, ~0, QueryTriggerInteraction.Ignore);
            var done = new HashSet<Health>();
            int hits = 0;
            foreach (var c in cols)
            {
                var enemy = c.GetComponentInParent<EnemyController>();
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead) continue;
                if (!done.Add(enemy.Health)) continue;
                Vector3 to = enemy.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > range + enemy.Radius) continue;
                if (to.sqrMagnitude > 0.01f && Vector3.Angle(aim, to) > arc * 0.5f) continue;
                float mult = current.damageMul * (finisher ? 1.6f : 1f);
                float kb = current.knockbackMul * (finisher ? 1.8f : 1f);
                DealDamage(enemy, mult, kb, to.sqrMagnitude > 0.01f ? to.normalized : aim, style == AttackStyle.Shield ? 0.7f : 0.15f);
                hits++;
            }
            if (hits > 0)
            {
                Juice.Shake(finisher ? 0.35f : 0.2f);
                Juice.Kick(aim, finisher ? 0.3f : 0.15f);
            }
        }

        private void FireBolt(Vector3 aim)
        {
            var cfg = GameManager.Instance.Config;
            if (cfg.playerBoltPrefab == null) { MeleeHit(AttackStyle.Fist, aim, false); return; }
            var spawn = transform.position + Vector3.up * 1.0f + aim * 0.6f;
            var bolt = Instantiate(cfg.playerBoltPrefab, spawn, Quaternion.LookRotation(aim, Vector3.up));
            bolt.LaunchFromPlayer(this, aim, 24f);
            Juice.Kick(-aim, 0.1f);
        }

        /// <summary>Calcule critique/frénésie/bourreau puis applique les dégâts à un ennemi.</summary>
        public void DealDamage(EnemyController enemy, float multiplier, float knockbackMul, Vector3 dir, float stun)
        {
            var gm = GameManager.Instance;
            var s = Stats;
            float dmg = s[StatType.Damage] * multiplier * Random.Range(0.92f, 1.08f);
            if (player.Health.Normalized < 0.35f) dmg *= 1f + gm.PassiveTotal(PassiveKind.Frenzy);
            float critChance = s[StatType.CritChance];
            if (enemy.Health.Normalized < 0.5f) critChance += gm.PassiveTotal(PassiveKind.Executioner);
            bool crit = Random.value < critChance;
            if (crit) dmg *= s[StatType.CritDamage];

            var info = new DamageInfo
            {
                amount = dmg,
                crit = crit,
                point = enemy.transform.position + Vector3.up,
                direction = dir,
                knockback = s[StatType.Knockback] * knockbackMul,
                source = gameObject
            };
            float dealt = enemy.ReceiveHit(info, stun);
            if (dealt > 0f)
            {
                float steal = s[StatType.LifeSteal];
                if (steal > 0f) player.Health.Heal(dealt * steal);
                gm.HitStop(crit ? 0.09f : 0.045f);
                if (crit) AudioManager.Sfx(l => l.crit);
            }
        }

        // ------------------------------------------------------------------ Ruée tranchante

        public void BeginDash() => dashHits.Clear();

        public void DashTick(Vector3 pos)
        {
            float pct = GameManager.Instance.PassiveTotal(PassiveKind.StringDash);
            if (pct <= 0f) return;
            foreach (var c in Physics.OverlapSphere(pos + Vector3.up * 0.8f, 1.1f, ~0, QueryTriggerInteraction.Ignore))
            {
                var enemy = c.GetComponentInParent<EnemyController>();
                if (enemy == null || enemy.Health.IsDead || !dashHits.Add(enemy.Health)) continue;
                DealDamage(enemy, pct, 0.5f, player.Facing, 0.2f);
            }
        }

        /// <summary>Échardes : explosion à la mort d'un ennemi.</summary>
        public void SplinterBurst(Vector3 pos)
        {
            float pct = GameManager.Instance.PassiveTotal(PassiveKind.Splinters);
            if (pct <= 0f || Stats == null) return;
            Juice.Spawn(v => v.splinterNova, pos + Vector3.up * 0.5f, Quaternion.identity);
            foreach (var c in Physics.OverlapSphere(pos + Vector3.up * 0.5f, 2.6f, ~0, QueryTriggerInteraction.Ignore))
            {
                var enemy = c.GetComponentInParent<EnemyController>();
                if (enemy == null || enemy.Health.IsDead) continue;
                Vector3 d = enemy.transform.position - pos; d.y = 0f;
                enemy.ReceiveHit(new DamageInfo
                {
                    amount = Stats[StatType.Damage] * pct,
                    point = enemy.transform.position + Vector3.up,
                    direction = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward,
                    knockback = 4f,
                    source = gameObject
                }, 0.2f);
            }
        }
    }
}
