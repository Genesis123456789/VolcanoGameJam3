using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PuppetDungeon
{
    /// <summary>
    /// IA des pantins corrompus. Machine à états simple :
    /// Endormi → Alerte → Poursuite → Armement (télégraphe) → Attaque → Récupération.
    /// Les ennemis restent endormis tant que le joueur est dans la zone de sûreté.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        public enum State { Dormant, Alerting, Chase, Windup, Attacking, Recover, Stunned, Dead }

        public static event Action<EnemyController, DamageInfo> AnyKilled;
        internal static void ResetStatics() => AnyKilled = null;

        [Header("Références")]
        public PuppetRig rig;
        public SquashStretch squash;
        public Transform alertAnchor;
        public Transform muzzle;

        [Header("Réglages")]
        public float gravity = 25f;
        public float separationRadius = 1.3f;
        public float turnSpeed = 540f;

        public EnemyDefinition Definition { get; private set; }
        public Health Health { get; private set; }
        public State CurrentState => state;
        public float Radius => controller != null ? controller.radius * transform.localScale.x : 0.5f;
        public bool IsBoss => Definition != null && Definition.isBoss;
        public float Damage { get; private set; }
        public int DungeonIndex { get; private set; }
        public RoomController Room { get; set; }

        private CharacterController controller;
        private HitFlash flash;
        private Animator animator;
        private State state = State.Dormant;
        private float stateTimer;
        private Vector3 knockVelocity;
        private float verticalVelocity;
        private float nextAttackTime;
        private Vector3 chargeDir;
        private GameObject telegraph;
        private int bossPattern;
        private bool enraged;
        private float idleSway;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            Health = GetComponent<Health>();
            flash = GetComponent<HitFlash>();
            if (rig == null) rig = GetComponentInChildren<PuppetRig>();
            if (squash == null) squash = GetComponentInChildren<SquashStretch>();
            animator = rig != null ? rig.Animator : GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
            gameObject.layer = Layers.Enemy;
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
            idleSway = Random.Range(0f, 10f);
        }

        private float tempo = 1f;

        public void Init(EnemyDefinition def, int dungeonIndex, GameConfig cfg)
        {
            Definition = def;
            DungeonIndex = dungeonIndex;
            float hp = def.maxHealth * cfg.EnemyHealthScale(dungeonIndex);
            Damage = def.damage * cfg.EnemyDamageScale(dungeonIndex);
            tempo = cfg.EnemyTempo(dungeonIndex);
            Health.Init(hp);
            Health.armor = def.armor * cfg.EnemyDamageScale(dungeonIndex);
            transform.localScale = Vector3.one * def.visualScale;
            nextAttackTime = Time.time + Random.Range(0.4f, 1.2f);
            SetState(State.Dormant);
            if (rig != null) rig.UseAnimations(def.animations != null ? def.animations : cfg.defaultAnimations);
        }

        // ------------------------------------------------------------------ Alerte

        public void Alert(float delay)
        {
            if (state != State.Dormant) return;
            StartCoroutine(AlertRoutine(delay));
        }

        private IEnumerator AlertRoutine(float delay)
        {
            SetState(State.Alerting);
            yield return new WaitForSeconds(delay);
            if (state == State.Dead) yield break;
            var anchor = alertAnchor != null ? alertAnchor : transform;
            var mark = Juice.SpawnObject(v => v.alertMark, anchor.position + Vector3.up * (1.9f * transform.localScale.y), Quaternion.identity, transform);
            if (mark != null) Destroy(mark, 0.9f);
            if (squash != null) squash.Punch(new Vector3(-0.15f, 0.35f, -0.15f));
            yield return new WaitForSeconds(0.45f);
            if (state == State.Alerting) SetState(State.Chase);
        }

        // ------------------------------------------------------------------ Boucle

        private void Update()
        {
            if (state == State.Dead) return;
            var player = PlayerController.Instance;
            Vector3 move = Vector3.zero;
            stateTimer -= Time.deltaTime;

            bool playerOk = player != null && !player.IsDead;
            Vector3 toPlayer = playerOk ? player.transform.position - transform.position : Vector3.zero;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            switch (state)
            {
                case State.Dormant:
                case State.Alerting:
                    // Léger balancement « suspendu à ses fils »
                    if (rig != null)
                        rig.transform.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.3f + idleSway) * 3f, 0f, Mathf.Sin(Time.time * 0.9f + idleSway) * 4f);
                    if (state == State.Alerting && playerOk) Face(toPlayer);
                    break;

                case State.Chase:
                    if (rig != null) rig.transform.localRotation = Quaternion.identity;
                    if (!playerOk) break;
                    move = ChaseMove(toPlayer, dist);
                    if (Time.time >= nextAttackTime && WantsToAttack(dist)) BeginWindup(toPlayer);
                    break;

                case State.Windup:
                    if (playerOk && Definition.behaviour != EnemyBehaviour.Charger) Face(toPlayer);
                    if (stateTimer <= 0f) StartCoroutine(AttackRoutine());
                    break;

                case State.Attacking:
                    if (Definition.behaviour == EnemyBehaviour.Charger || (IsBoss && bossPattern == 2))
                        move = chargeDir * Definition.chargeSpeed * (enraged ? 1.2f : 1f);
                    break;

                case State.Recover:
                case State.Stunned:
                    if (stateTimer <= 0f) SetState(State.Chase);
                    break;
            }

            // Séparation entre ennemis
            if (state == State.Chase) move += Separation() * 2f;

            knockVelocity = Vector3.MoveTowards(knockVelocity, Vector3.zero, 30f * Time.deltaTime);
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity -= gravity * Time.deltaTime;
            if (controller.enabled)
            {
                var flags = controller.Move((move + knockVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
                if (state == State.Attacking && (flags & CollisionFlags.Sides) != 0 && (Definition.behaviour == EnemyBehaviour.Charger || (IsBoss && bossPattern == 2)))
                    OnChargeWallHit();
            }

            if (rig != null)
            {
                // 0.35 = marche dans le blend tree ; les ennemis courent quand ils sont enragés ou chargent.
                float speed01 = move.magnitude / Mathf.Max(0.1f, Definition != null ? Definition.moveSpeed : 3f);
                rig.SetSpeed(state == State.Attacking ? 0f : Mathf.Clamp01(speed01 * (enraged ? 0.9f : 0.55f)), 0.1f);
            }
        }

        private Vector3 ChaseMove(Vector3 toPlayer, float dist)
        {
            float speed = Definition.moveSpeed * (enraged ? 1.35f : 1f);
            Vector3 dir = dist > 0.001f ? toPlayer / dist : transform.forward;
            Face(toPlayer);
            switch (Definition.behaviour)
            {
                case EnemyBehaviour.Shooter:
                    float pref = Definition.preferredDistance > 0f ? Definition.preferredDistance : 6f;
                    if (dist < pref - 1.2f) return -dir * speed * 0.8f;
                    if (dist > pref + 1.2f) return dir * speed;
                    // Tourne autour du joueur
                    return Vector3.Cross(Vector3.up, dir) * speed * 0.45f * Mathf.Sign(Mathf.Sin(Time.time * 0.7f + idleSway));
                default:
                    if (dist <= Definition.attackRange * 0.85f) return Vector3.zero;
                    return dir * speed;
            }
        }

        private bool WantsToAttack(float dist)
        {
            switch (Definition.behaviour)
            {
                case EnemyBehaviour.Shooter: return dist < 13f;
                case EnemyBehaviour.Charger: return dist < 8f && dist > 2f || dist <= Definition.attackRange;
                case EnemyBehaviour.Boss: return dist < 14f;
                default: return dist <= Definition.attackRange;
            }
        }

        private void BeginWindup(Vector3 toPlayer)
        {
            float windup = Definition.windup * tempo * (enraged ? 0.75f : 1f);
            if (IsBoss)
            {
                bossPattern = PickBossPattern(toPlayer.magnitude);
                windup *= bossPattern == 0 ? 1.4f : 1f;
            }
            SetState(State.Windup, windup);
            // Posture de garde / visée pendant la préparation (bouclier levé, arbalète épaulée…)
            if (rig != null) rig.SetGuard(Definition.behaviour != EnemyBehaviour.Chaser);
            chargeDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;
            if (flash != null) flash.Flash(new Color(1f, 0.85f, 0.3f), windup);
            if (squash != null) squash.Punch(new Vector3(0.15f, -0.2f, 0.15f));

            // Télégraphe au sol
            bool showCircle = Definition.behaviour == EnemyBehaviour.Chaser || (IsBoss && bossPattern == 0);
            if (showCircle)
            {
                float r = IsBoss ? 3.4f : Definition.attackRange;
                Vector3 pos = IsBoss ? transform.position : transform.position + chargeDir * r * 0.5f;
                telegraph = Juice.SpawnObject(v => v.telegraphCircle, new Vector3(pos.x, 0.03f, pos.z), Quaternion.identity);
                if (telegraph != null)
                {
                    telegraph.transform.localScale = new Vector3(r * 2f, 1f, r * 2f);
                    var tc = telegraph.GetComponent<TelegraphCircle>();
                    if (tc != null) tc.Play(windup);
                }
            }
        }

        private int PickBossPattern(float dist)
        {
            if (dist < 3.5f) return 0;           // claque au sol
            int p = Random.Range(0, 3);
            return p;                            // 0 claque, 1 anneau d'orbes, 2 charge
        }

        private IEnumerator AttackRoutine()
        {
            SetState(State.Attacking, 5f);
            if (telegraph != null) { Destroy(telegraph); telegraph = null; }
            if (rig != null)
            {
                rig.SetGuard(false);
                rig.PlayAttack(false, 0.6f);
            }
            if (squash != null) squash.Punch(new Vector3(-0.1f, 0.15f, 0.3f));

            var behaviour = IsBoss ? (bossPattern == 0 ? EnemyBehaviour.Chaser : bossPattern == 1 ? EnemyBehaviour.Shooter : EnemyBehaviour.Charger) : Definition.behaviour;
            switch (behaviour)
            {
                case EnemyBehaviour.Chaser:
                    MeleeStrike(IsBoss ? 3.4f : Definition.attackRange + 0.3f, IsBoss ? 360f : 110f);
                    yield return new WaitForSeconds(0.25f);
                    break;
                case EnemyBehaviour.Shooter:
                    Shoot();
                    yield return new WaitForSeconds(0.2f);
                    break;
                case EnemyBehaviour.Charger:
                    AudioManager.Sfx(l => l.dash, 0.8f, 0.7f);
                    float t = 0f;
                    while (state == State.Attacking && t < 0.75f)
                    {
                        t += Time.deltaTime;
                        TryChargeHit();
                        yield return null;
                    }
                    break;
            }
            if (state == State.Attacking || state == State.Windup)
            {
                SetState(State.Recover, IsBoss ? 0.6f : 0.55f);
                nextAttackTime = Time.time + Definition.attackCooldown * tempo * (enraged ? 0.65f : 1f) * Random.Range(0.85f, 1.2f);
            }
        }

        private void MeleeStrike(float range, float arc)
        {
            AudioManager.Sfx(l => l.swing, 0.7f, IsBoss ? 0.6f : 0.85f);
            if (IsBoss)
            {
                Juice.Shake(0.6f);
                Juice.Spawn(v => v.doorSlam, transform.position, Quaternion.identity, null, 2f);
            }
            var player = PlayerController.Instance;
            if (player == null || player.IsDead) return;
            Vector3 to = player.transform.position - transform.position; to.y = 0f;
            if (to.magnitude > range + 0.4f) return;
            if (arc < 360f && to.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, to) > arc * 0.5f) return;
            player.Health.TakeDamage(new DamageInfo { amount = Damage, point = player.transform.position, direction = to.normalized, knockback = IsBoss ? 12f : 7f, source = gameObject });
        }

        private void Shoot()
        {
            var cfg = GameManager.Instance.Config;
            if (cfg.enemyOrbPrefab == null) return;
            var player = PlayerController.Instance;
            Vector3 origin = muzzle != null ? muzzle.position : transform.position + transform.forward * 0.6f;
            origin.y = transform.position.y + 1.0f; // hauteur du torse du joueur, même pour les géants
            AudioManager.Sfx(l => l.shoot, 0.6f, IsBoss ? 0.7f : 1.15f);
            if (IsBoss)
            {
                int n = enraged ? 18 : 12;
                float offset = Random.Range(0f, 360f);
                for (int i = 0; i < n; i++)
                {
                    Vector3 d = Quaternion.Euler(0f, offset + i * 360f / n, 0f) * Vector3.forward;
                    var orb = Instantiate(cfg.enemyOrbPrefab, origin + d * 0.6f, Quaternion.LookRotation(d));
                    orb.LaunchFromEnemy(gameObject, d, Definition.projectileSpeed * 0.75f, Damage * 0.7f);
                }
                return;
            }
            if (player == null) return;
            Vector3 aim = player.transform.position + Vector3.up * 0.9f - origin;
            aim.y = 0f;
            int count = Mathf.Max(1, Definition.projectilesPerShot);
            for (int i = 0; i < count; i++)
            {
                float a = count == 1 ? 0f : Mathf.Lerp(-Definition.projectileSpread, Definition.projectileSpread, i / (float)(count - 1));
                Vector3 d = Quaternion.Euler(0f, a, 0f) * aim.normalized;
                var orb = Instantiate(cfg.enemyOrbPrefab, origin, Quaternion.LookRotation(d));
                orb.LaunchFromEnemy(gameObject, d, Definition.projectileSpeed, Damage);
            }
        }

        private void TryChargeHit()
        {
            var player = PlayerController.Instance;
            if (player == null || player.IsDead) return;
            Vector3 to = player.transform.position - transform.position; to.y = 0f;
            if (to.magnitude < Radius + 0.8f)
            {
                player.Health.TakeDamage(new DamageInfo { amount = Damage * 1.2f, point = player.transform.position, direction = chargeDir, knockback = 10f, source = gameObject });
                SetState(State.Recover, 0.6f);
            }
        }

        private void OnChargeWallHit()
        {
            Juice.Shake(IsBoss ? 0.6f : 0.3f);
            Juice.Spawn(v => v.doorSlam, transform.position + chargeDir * Radius, Quaternion.identity, null, IsBoss ? 1.5f : 0.8f);
            AudioManager.Sfx(l => l.hit, 0.8f, 0.6f);
            SetState(State.Stunned, IsBoss ? 1.2f : 0.9f);
            if (flash != null) flash.Flash(Color.white, 0.15f);
        }

        private Vector3 Separation()
        {
            Vector3 push = Vector3.zero;
            var overlap = Physics.OverlapSphere(transform.position, separationRadius * transform.localScale.x, Layers.EnemyMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlap.Length; i++)
            {
                if (overlap[i].transform == transform || overlap[i].transform.IsChildOf(transform)) continue;
                Vector3 d = transform.position - overlap[i].transform.position; d.y = 0f;
                float m = d.magnitude;
                if (m > 0.001f) push += d / m * (1f - m / (separationRadius * transform.localScale.x));
            }
            return push;
        }

        private void Face(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir, Vector3.up), turnSpeed * Time.deltaTime);
        }

        private void SetState(State s, float timer = 0f)
        {
            state = s;
            stateTimer = timer;
        }

        // ------------------------------------------------------------------ Dégâts

        /// <summary>Point d'entrée des coups du joueur. Réveille l'ennemi s'il dormait.</summary>
        public float ReceiveHit(DamageInfo info, float stun)
        {
            if (state == State.Dead) return 0f;
            if (state == State.Dormant && Room != null) Room.TriggerAlert();
            float dealt = Health.TakeDamage(info);
            if (dealt > 0f && !Health.IsDead && !IsBoss && stun > 0f && state != State.Attacking)
                SetState(State.Stunned, stun);
            return dealt;
        }

        private void OnDamaged(DamageInfo info, float dmg)
        {
            if (flash != null) flash.Flash(Color.white, 0.1f);
            if (squash != null) squash.Punch(new Vector3(0.25f, -0.3f, 0.25f));
            if (rig != null && state != State.Attacking) rig.PlayHit();
            float kbResist = IsBoss ? 0.15f : 1f;
            Vector3 kb = info.direction; kb.y = 0f;
            if (kb.sqrMagnitude > 0.001f) knockVelocity = kb.normalized * info.knockback * kbResist;
            Juice.DamageNumber(transform.position + Vector3.up * (transform.localScale.y - 1f), dmg, info.crit, Color.white);
            Juice.Spawn(v => v.hitSpark, info.point, Quaternion.LookRotation(info.direction.sqrMagnitude > 0.001f ? info.direction : Vector3.forward), info.crit ? new Color(1f, 0.85f, 0.2f) : (Color?)null);
            AudioManager.Sfx(l => l.hit, 0.9f, IsBoss ? 0.75f : 1f);
            if (IsBoss && !enraged && Health.Normalized < 0.5f)
            {
                enraged = true;
                Juice.Text(transform.position + Vector3.up * 2f, "Ses fils se tendent !", new Color(1f, 0.3f, 0.25f), 1.3f);
                Juice.Shake(0.7f);
                if (flash != null) flash.SetGlobalTint(new Color(1f, 0.6f, 0.6f));
            }
        }

        private void OnDied(DamageInfo info)
        {
            SetState(State.Dead);
            StopAllCoroutines();
            if (telegraph != null) Destroy(telegraph);
            controller.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

            Juice.Spawn(v => v.woodChips, transform.position + Vector3.up, Quaternion.identity, null, IsBoss ? 2.5f : 1f);
            AudioManager.Sfx(l => l.enemyDeath, 1f, IsBoss ? 0.6f : 1f);
            Juice.Shake(IsBoss ? 1f : 0.35f);
            if (rig != null) rig.Shatter(transform.position + Vector3.up * 0.4f - info.direction * 0.5f, IsBoss ? 9f : 5.5f);

            var gm = GameManager.Instance;
            if (gm != null && gm.Run != null)
            {
                gm.Run.enemiesKilled++;
                if (IsBoss) gm.Run.bossesKilled++;
                DropLoot(gm);
            }
            AnyKilled?.Invoke(this, info);
            Destroy(gameObject, 0.05f);
        }

        private void DropLoot(GameManager gm)
        {
            var cfg = gm.Config;
            float luck = gm.PassiveTotal(PassiveKind.LuckyCharm);
            int drops = 0;
            if (IsBoss) drops = Mathf.Max(1, cfg.bossLootCount);
            else if (Random.value < cfg.enemyLootChance * Definition.lootChanceMultiplier * (1f + luck)) drops = 1;
            // Premier pantin vaincu de la run : une pièce garantie pour découvrir l'inventaire.
            if (gm.Run.dungeonIndex == 0 && gm.Run.enemiesKilled == 1 && gm.Run.generation == 1) drops = Mathf.Max(drops, 1);
            for (int i = 0; i < drops; i++)
            {
                var part = LootGenerator.Roll(cfg, gm.Run.dungeonIndex, Definition.lootRarityBonus, luck, IsBoss ? Rarity.Rare : (Rarity?)null);
                if (part == null) break;
                Vector3 dir = Quaternion.Euler(0f, i * 360f / Mathf.Max(1, drops) + Random.Range(-30f, 30f), 0f) * Vector3.forward;
                LootPickup.Drop(part, transform.position, dir);
            }
        }
    }
}
