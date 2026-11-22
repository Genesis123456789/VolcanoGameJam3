using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PuppetDungeon
{
    /// <summary>
    /// Pantin joueur : déplacement (CharacterController), ruée, visée, entrées (Input System),
    /// application des stats calculées à partir des pièces équipées.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Références")]
        public PuppetRig rig;
        public SquashStretch squash;
        public Transform visualRoot;

        [Header("Déplacement")]
        public float acceleration = 60f;
        public float turnSpeed = 900f;
        public float dashSpeed = 17f;
        public float dashDuration = 0.17f;
        public float gravity = 25f;

        public CharacterController Controller { get; private set; }
        public Health Health { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public StatBlock Stats { get; private set; }
        public Vector3 Facing { get; private set; } = Vector3.forward;
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public bool IsDashing { get; private set; }
        public bool ControlLocked => lockCount > 0;
        public bool IsDead => Health != null && Health.IsDead;
        public float DashReady01 => Stats == null ? 1f : Mathf.Clamp01(1f - (nextDashTime - Time.time) / Mathf.Max(0.01f, Stats[StatType.DashCooldown]));

        private PlayerInput playerInput;
        private InputAction moveAction, attackAction, dashAction, interactAction, inventoryAction, pauseAction;
        private Vector3 velocity;
        private float verticalVelocity;
        private int lockCount;
        private float nextDashTime;
        private float momentumUntil;
        private float momentumBonus;
        private Vector2 lastMousePos;
        private float mouseAimUntil;
        private float footstepTimer;

        private void Awake()
        {
            Instance = this;
            Controller = GetComponent<CharacterController>();
            Health = GetComponent<Health>();
            Combat = GetComponent<PlayerCombat>();
            if (Combat == null) Combat = gameObject.AddComponent<PlayerCombat>();
            playerInput = GetComponent<PlayerInput>();
            if (rig == null) rig = GetComponentInChildren<PuppetRig>();
            if (squash == null) squash = GetComponentInChildren<SquashStretch>();
            var animator = rig != null ? rig.Animator : GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
            gameObject.layer = Layers.Player;
            Layers.ConfigureCollisions();

            Health.invulnerabilityAfterHit = 0.6f;
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
            Health.DamageFilter = FilterDamage;
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.StatsDirty += RefreshFromRun;
            EnemyController.AnyKilled += OnEnemyKilled;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.StatsDirty -= RefreshFromRun;
            EnemyController.AnyKilled -= OnEnemyKilled;
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Les actions sont résolues après l'activation de PlayerInput (qui peut dupliquer l'asset) :
        /// on récupère donc toujours les actions réellement activées.
        /// </summary>
        private void CacheActions()
        {
            var actions = playerInput != null ? playerInput.actions : null;
            if (actions == null) return;
            moveAction = actions.FindAction("Move");
            attackAction = actions.FindAction("Attack");
            dashAction = actions.FindAction("Dash");
            interactAction = actions.FindAction("Interact");
            inventoryAction = actions.FindAction("Inventory");
            pauseAction = actions.FindAction("Pause");
            if (!actions.enabled) actions.Enable();
        }

        private void Start()
        {
            CacheActions();
            GameManager.Instance.EnsureRun();
            RefreshFromRun();
            var run = GameManager.Instance.Run;
            Health.Init(Stats[StatType.MaxHealth], run.currentHealth);
        }

        /// <summary>Recalcule stats + visuel à partir de l'inventaire.</summary>
        public void RefreshFromRun()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            Stats = gm.ComputePlayerStats();
            if (Health != null)
            {
                bool first = Health.maxHealth <= 0f || Health.Current <= 0f;
                Health.SetMax(Stats[StatType.MaxHealth], !first);
                Health.armor = Stats[StatType.Armor];
            }
            if (rig != null)
            {
                rig.ApplyLoadout(gm.Run.inventory.equipped, gm.Config);
                RefreshAnimations();
                rig.SetWoodTint(gm.Run.woodTint);
            }
        }

        public void Lock(bool on)
        {
            lockCount = Mathf.Max(0, lockCount + (on ? 1 : -1));
            if (ControlLocked) velocity = Vector3.zero;
        }

        public void ResetLocks() => lockCount = 0;

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Run != null) gm.Run.currentHealth = Health.IsDead ? -1f : Health.Current;

            HandleMenuInputs();
            if (IsDead || gm == null || gm.IsPaused) { UpdateAnimator(0f); return; }

            Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            if (ControlLocked) move = Vector2.zero;
            Vector3 input = new Vector3(move.x, 0f, move.y);
            input = Vector3.ClampMagnitude(input, 1f);

            UpdateAim(input);

            if (!ControlLocked && !IsDashing)
            {
                if (dashAction != null && dashAction.WasPressedThisFrame() && Time.time >= nextDashTime)
                    StartCoroutine(DashRoutine(input.sqrMagnitude > 0.01f ? input.normalized : Facing));
                if (attackAction != null && (attackAction.WasPressedThisFrame() || (attackAction.IsPressed() && Combat.WantsAutoRepeat)))
                    Combat.TryAttack(AimDirection);
                if (interactAction != null && interactAction.WasPressedThisFrame())
                    Interactable.TryInteractNearest(transform.position);
            }

            if (!Controller.enabled) return; // déplacement scripté en cours (il pilote l'animation)
            if (!IsDashing) Move(input);
            UpdateEasterEgg(input);
            UpdateAnimator(IsDashing ? 1f : velocity.magnitude / Mathf.Max(0.1f, MoveSpeed));
        }

        private void HandleMenuInputs()
        {
            var ui = UIRoot.Instance;
            if (ui == null) return;
            if (pauseAction != null && pauseAction.WasPressedThisFrame()) ui.OnPausePressed();
            if (inventoryAction != null && inventoryAction.WasPressedThisFrame() && !IsDead) ui.OnInventoryPressed();
        }

        public float MoveSpeed
        {
            get
            {
                float s = Stats != null ? Stats[StatType.MoveSpeed] : 5.5f;
                if (Time.time < momentumUntil) s *= 1f + momentumBonus;
                if (Combat != null && Combat.IsAttacking) s *= Combat.MoveSpeedWhileAttacking;
                return s;
            }
        }

        private void Move(Vector3 input)
        {
            // Pendant une traversée de porte, le CharacterController est désactivé (déplacement scripté).
            if (!Controller.enabled) return;
            Vector3 target = input * MoveSpeed;
            velocity = Vector3.MoveTowards(velocity, target, acceleration * Time.deltaTime);
            if (Controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity -= gravity * Time.deltaTime;
            Controller.Move((velocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (input.sqrMagnitude > 0.01f && (Combat == null || !Combat.IsAttacking))
                Facing = input.normalized;
            Vector3 look = Combat != null && Combat.IsAttacking ? AimDirection : Facing;
            if (look.sqrMagnitude > 0.001f)
            {
                var targetRot = Quaternion.LookRotation(look, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            // Pas
            if (velocity.magnitude > 1f && Controller.isGrounded)
            {
                footstepTimer -= Time.deltaTime * velocity.magnitude;
                if (footstepTimer <= 0f)
                {
                    footstepTimer = 1.7f;
                    AudioManager.Sfx(l => l.footstep, 0.35f, Random.Range(1.2f, 1.45f));
                    if (Random.value < 0.35f)
                        Juice.Spawn(v => v.dust, transform.position, Quaternion.identity, null, 0.5f);
                }
            }
        }

        private void UpdateAim(Vector3 moveInput)
        {
            // Manette : le stick droit vise (prioritaire tant qu'il est incliné).
            var pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 rs = pad.rightStick.ReadValue();
                if (rs.sqrMagnitude > 0.2f)
                {
                    AimDirection = new Vector3(rs.x, 0f, rs.y).normalized;
                    mouseAimUntil = 0f;
                    return;
                }
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 mp = mouse.position.ReadValue();
                if ((mp - lastMousePos).sqrMagnitude > 4f || mouse.leftButton.wasPressedThisFrame)
                    mouseAimUntil = Time.time + 2.5f;
                lastMousePos = mp;
            }
            bool usingMouse = mouse != null && Time.time < mouseAimUntil && Camera.main != null;
            if (usingMouse)
            {
                var ray = Camera.main.ScreenPointToRay(lastMousePos);
                var plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y + 0.8f, 0f));
                if (plane.Raycast(ray, out float d))
                {
                    Vector3 p = ray.GetPoint(d) - transform.position;
                    p.y = 0f;
                    if (p.sqrMagnitude > 0.04f) { AimDirection = p.normalized; return; }
                }
            }
            AimDirection = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : Facing;
        }

        // ------------------------------------------------------------------ Easter egg « 67 »

        private float sixPressedAt = -10f;
        private float nextEmote;

        /// <summary>
        /// À l'arrêt : 6 puis 7 au clavier (rangée du haut ou pavé numérique), ou les deux sticks enfoncés
        /// ensemble (L3 + R3) à la manette → le pantin se tourne vers la caméra et joue l'animation « 67 ».
        /// </summary>
        private void UpdateEasterEgg(Vector3 input)
        {
            bool idle = input.sqrMagnitude < 0.01f && Speed01 < 0.05f && !IsDashing && !ControlLocked && (Combat == null || !Combat.IsAttacking);
            bool trigger = false;
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.digit6Key.wasPressedThisFrame || k.numpad6Key.wasPressedThisFrame) sixPressedAt = Time.unscaledTime;
                if ((k.digit7Key.wasPressedThisFrame || k.numpad7Key.wasPressedThisFrame) && Time.unscaledTime - sixPressedAt < 0.8f) trigger = true;
            }
            var g = Gamepad.current;
            if (g != null && g.leftStickButton.isPressed && g.rightStickButton.isPressed
                && (g.leftStickButton.wasPressedThisFrame || g.rightStickButton.wasPressedThisFrame))
                trigger = true;
            if (!trigger || !idle || Time.time < nextEmote || rig == null) return;
            nextEmote = Time.time + 1.5f;
            sixPressedAt = -10f;
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 toCam = cam.transform.position - transform.position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.01f) FaceTowards(toCam);
            }
            rig.Celebrate();
            if (squash != null) squash.Punch(new Vector3(0.08f, 0.12f, 0.08f));
        }

        private IEnumerator DashRoutine(Vector3 dir)
        {
            IsDashing = true;
            nextDashTime = Time.time + (Stats != null ? Stats[StatType.DashCooldown] : 1f);
            Health.GrantInvulnerability(dashDuration + 0.05f);
            Facing = dir;
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            if (squash != null) squash.Punch(new Vector3(-0.25f, -0.15f, 0.45f));
            AudioManager.Sfx(l => l.dash, 0.7f);
            Juice.Spawn(v => v.dust, transform.position, Quaternion.LookRotation(-dir), null, 1f);
            Combat.BeginDash();
            for (float t = 0f; t < dashDuration; t += Time.deltaTime)
            {
                if (!Controller.enabled) break;
                Controller.Move(dir * dashSpeed * Time.deltaTime + Vector3.down * 2f * Time.deltaTime);
                Combat.DashTick(transform.position);
                yield return null;
            }
            velocity = dir * MoveSpeed * 0.6f;
            if (squash != null) squash.Punch(new Vector3(0.2f, -0.2f, -0.2f));
            IsDashing = false;
        }

        /// <summary>Vitesse actuelle normalisée (0 → 1), utilisée pour choisir l'attaque mélangée ou en corps entier.</summary>
        public float Speed01 { get; private set; }

        private void UpdateAnimator(float speed01)
        {
            Speed01 = Mathf.Clamp01(speed01);
            if (rig == null) return;
            // Marche à ~0.35 dans le blend tree, course au-delà selon la vitesse réelle.
            rig.SetSpeed(Speed01 < 0.05f ? 0f : Mathf.Lerp(0.3f, 1f, Mathf.InverseLerp(0.2f, 1f, Speed01)));
            // Pas de garde automatique : à l'arrêt, le joueur joue l'idle.
            rig.SetGuard(false);
        }

        /// <summary>Applique le jeu d'animations de l'arme portée au bras droit.</summary>
        public void RefreshAnimations()
        {
            var gm = GameManager.Instance;
            if (rig == null || gm == null || gm.Config == null) return;
            rig.UseAnimations(gm.Config.AnimationsFor(gm.CurrentAttackStyle()));
        }

        /// <summary>Déplacement scripté (traversée de porte, cinématiques).</summary>
        public IEnumerator ScriptedMove(Vector3 target, float duration)
        {
            Vector3 start = transform.position;
            target.y = start.y;
            Vector3 dir = target - start;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Facing = dir.normalized;
                transform.rotation = Quaternion.LookRotation(Facing, Vector3.up);
            }
            Controller.enabled = false;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                transform.position = Vector3.Lerp(start, target, t / duration);
                UpdateAnimator(0.8f);
                yield return null;
            }
            transform.position = target;
            Controller.enabled = true;
            velocity = Vector3.zero;
        }

        public void Teleport(Vector3 pos)
        {
            Controller.enabled = false;
            transform.position = pos;
            Controller.enabled = true;
            velocity = Vector3.zero;
            Physics.SyncTransforms();
        }

        public void FaceTowards(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            Facing = dir.normalized;
            transform.rotation = Quaternion.LookRotation(Facing, Vector3.up);
        }

        // ------------------------------------------------------------------ Dégâts

        private bool varnishUsed;
        public void OnRoomEntered() => varnishUsed = false;

        private float FilterDamage(DamageInfo info, float dmg)
        {
            float varnish = GameManager.Instance.PassiveTotal(PassiveKind.Varnish);
            if (varnish > 0f && !varnishUsed)
            {
                varnishUsed = true;
                float absorb = varnish * Health.maxHealth;
                Juice.Text(transform.position, "Vernis !", new Color(1f, 0.85f, 0.4f), 0.9f);
                dmg = Mathf.Max(0f, dmg - absorb);
            }
            if (Combat != null && Combat.IsAttacking) dmg *= Combat.DamageTakenWhileAttacking;
            return dmg;
        }

        private void OnDamaged(DamageInfo info, float dmg)
        {
            if (rig != null) rig.PlayHit();
            Juice.Shake(0.45f);
            Juice.Kick(info.direction, 0.25f);
            GameManager.Instance.HitStop(0.06f);
            var flash = GetComponent<HitFlash>();
            if (flash != null) flash.Flash(new Color(1f, 0.25f, 0.2f), 0.2f);
            if (squash != null) squash.Punch(new Vector3(0.2f, -0.25f, 0.2f));
            Juice.DamageNumber(transform.position, dmg, false, new Color(1f, 0.35f, 0.3f));
            Juice.Spawn(v => v.woodChips, transform.position + Vector3.up, Quaternion.identity, null, 0.6f);
            AudioManager.Sfx(l => l.playerHurt);
            PostFX.Pulse(PostFX.PulseKind.Hurt);
            // Recul
            if (info.knockback > 0f && !info.isReflect)
            {
                Vector3 kb = info.direction; kb.y = 0f;
                if (kb.sqrMagnitude > 0.001f) velocity = kb.normalized * info.knockback;
            }
            // Ronces
            float thorns = GameManager.Instance.PassiveTotal(PassiveKind.Thorns);
            if (thorns > 0f && info.source != null && !info.isReflect)
            {
                var h = info.source.GetComponentInParent<Health>();
                if (h != null && h != Health)
                    h.TakeDamage(new DamageInfo { amount = dmg * thorns, point = h.transform.position, direction = -info.direction, isReflect = true, ignoreArmor = true, source = gameObject });
            }
        }

        private void OnDied(DamageInfo info)
        {
            StopAllCoroutines();
            IsDashing = false;
            Controller.enabled = false;
            GameManager.Instance.SlowMo(0.25f, 1.2f);
            Juice.Shake(1f);
            AudioManager.Sfx(l => l.enemyDeath, 1f, 0.7f);
            Juice.Spawn(v => v.woodChips, transform.position + Vector3.up, Quaternion.identity, null, 1.6f);
            if (rig != null) rig.Shatter(transform.position + Vector3.up * 0.5f, 6f);
            var run = GameManager.Instance.Run;
            if (run != null) run.currentHealth = -1f;
            if (UIRoot.Instance != null) UIRoot.Instance.ShowGameOver(2.2f);
        }

        private void OnEnemyKilled(EnemyController enemy, DamageInfo info)
        {
            if (IsDead) return;
            if (Combat != null) Combat.SplinterBurst(enemy.transform.position);
            float momentum = GameManager.Instance.PassiveTotal(PassiveKind.Momentum);
            if (momentum > 0f)
            {
                momentumBonus = momentum;
                momentumUntil = Time.time + 3f;
            }
        }

        public void HealPercent(float pct)
        {
            if (pct <= 0f) return;
            Health.Heal(Health.maxHealth * pct);
            Juice.Spawn(v => v.healPuff, transform.position + Vector3.up, Quaternion.identity);
        }
    }
}
