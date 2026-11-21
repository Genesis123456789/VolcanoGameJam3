using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerController 3D (déplacement sur le plan XZ).
/// Requiert : Rigidbody + PlayerInput (avec PlayerControls.inputactions assigné).
/// Animator optionnel (paramètres : Speed [float], Attack [trigger]).
/// Marionnette optionnelle : ses stats (Hp, Damages, Resistance, Speed) sont appliquées au joueur.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Stats (Temporaire)")]
    [SerializeField] private float hp;

    [Header("Déplacement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f;     // degrés par seconde
    [SerializeField] private float speedPerPoint = 0.1f;     // bonus de vitesse par point de stat Speed

    [Header("Attaque")]
    [SerializeField] private float attackDuration = 0.3f;   // durée pendant laquelle le joueur est bloqué
    [SerializeField] private float attackCooldown = 0.1f;
    [SerializeField] private float attackRange = 0.8f;      // distance du centre de la hitbox
    [SerializeField] private float attackRadius = 0.5f;     // taille de la hitbox
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private LayerMask hittableLayers;      // À RÉGLER dans l'inspecteur (sinon rien n'est touché)

    [Header("Références (optionnel)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SimpleAttackZone AnimAttackZone;
    [SerializeField] private Marionnette marionnette;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private InputAction inventoryAction;

    private Vector2 moveInput;
    private Vector3 facing = Vector3.forward; // direction regardée (pour l'attaque et la rotation)
    private bool isAttacking;
    private float nextAttackTime;
    private bool isDead;
    private bool deathApplied;
    private bool hasSpeedParam;
    private bool hasAttackParam;

    private readonly HashSet<Enemy> alreadyHit = new HashSet<Enemy>();

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    public bool Isdead { get => isDead; set => isDead = value; }

    // Stats effectives (valeurs de base + bonus de la Marionnette)
    private float CurrentMoveSpeed => moveSpeed + (marionnette != null ? marionnette.Speed * speedPerPoint : 0f);
    private int CurrentDamage => attackDamage + (marionnette != null ? marionnette.Damages : 0);
    private int CurrentResistance => marionnette != null ? marionnette.Resistance : 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        attackAction = playerInput.actions["Attack"];
        interactAction = playerInput.actions["Interact"];
        inventoryAction = playerInput.actions["Inventory"];

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (marionnette == null) marionnette = GetComponentInChildren<Marionnette>();
        if (marionnette == null) marionnette = GetComponentInParent<Marionnette>();

        // Le déplacement est piloté par le Rigidbody : l'animation ne doit pas déplacer le modèle
        if (animator != null) animator.applyRootMotion = false;
    }

    private void Start()
    {
        // Marionnette.Awake a déjà calculé les stats : pas de problème d'ordre d'exécution
        if (marionnette != null && marionnette.Hp > 0)
            hp = marionnette.Hp;

        hasSpeedParam = HasParameter(SpeedHash, AnimatorControllerParameterType.Float);
        hasAttackParam = HasParameter(AttackHash, AnimatorControllerParameterType.Trigger);
    }

    private bool HasParameter(int hash, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.nameHash == hash && p.type == type) return true;
        }
        return false;
    }

    private void Update()
    {
        if (isDead) return;

        moveInput = moveAction.ReadValue<Vector2>();

        // Mémorise la dernière direction regardée (sert à l'attaque et à la rotation)
        if (moveInput.sqrMagnitude > 0.01f && !isAttacking)
            facing = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (attackAction.WasPressedThisFrame() && !isAttacking && Time.time >= nextAttackTime)
            StartCoroutine(AttackRoutine());

        if (interactAction.WasPressedThisFrame())
            Interact();

        if (inventoryAction.WasPressedThisFrame())
            OpenInventory();

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 planar = isAttacking ? Vector3.zero : Vector3.ClampMagnitude(input, 1f) * CurrentMoveSpeed;

        // On garde la vitesse verticale : sinon la gravité est écrasée à chaque frame physique
        Vector3 v = rb.linearVelocity; // Unity 6. Sur Unity 2022 ou avant : rb.velocity
        rb.linearVelocity = new Vector3(planar.x, v.y, planar.z);

        // Le personnage se tourne vers sa direction de déplacement
        if (!isAttacking && input.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(facing, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target, rotationSpeed * Time.fixedDeltaTime));
        }
    }

    private void UpdateAnimator()
    {
        if (!hasSpeedParam) return;
        float speed = isAttacking ? 0f : Mathf.Clamp01(moveInput.magnitude);
        animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
    }

    private void OpenInventory()
    {
        if (PlayerManager.instance != null)
            PlayerManager.instance.SetInventory();
    }

    private void Interact()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, 2f);

        for (int i = 0; i < cols.Length; i++)
        {
            if (!cols[i].CompareTag("Loot")) continue;

            if (cols[i].TryGetComponent<Loot>(out var loot))
            {
                loot.LootLoot();
                Debug.Log("Loot " + cols[i].gameObject.name, this);
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        if (hasAttackParam) animator.SetTrigger(AttackHash);
        if (AnimAttackZone) AnimAttackZone.PlayAnimationAttackZone();

        // Hitbox devant le joueur
        Vector3 hitPoint = transform.position + facing * attackRange;
        Collider[] hits = Physics.OverlapSphere(hitPoint, attackRadius, hittableLayers);

        alreadyHit.Clear();
        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; // ignore le joueur et ses pièces

            // Le collider peut être sur un enfant de l'ennemi
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null) continue;

            // Un ennemi avec plusieurs colliders ne prend les dégâts qu'une fois
            if (!alreadyHit.Add(enemy)) continue;

            Debug.Log($"Enemy {enemy.name} took {CurrentDamage} !");
            enemy.TakeDamage(CurrentDamage);
        }

        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        int finalDamage = Mathf.Max(1, damage - CurrentResistance);

        if (hp <= finalDamage)
        {
            hp = 0;
            Death();
            return;
        }
        hp -= finalDamage;
    }

    public void Death()
    {
        isDead = true;
        isAttacking = false;
        StopAllCoroutines();

        // Coupe la physique : sinon le joueur continue de glisser avec sa dernière vitesse
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.isKinematic = true;

        if (hasSpeedParam) animator.SetFloat(SpeedHash, 0f);

        if (deathApplied) return;
        deathApplied = true;

        transform.Rotate(90f, 0f, 0f);

        // Pose le corps allongé sur le sol
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            float radius = TryGetComponent(out CapsuleCollider capsule) ? capsule.radius : 0.5f;
            Vector3 pos = transform.position;
            transform.position = new Vector3(pos.x, hit.point.y + radius, pos.z);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 dir = Application.isPlaying ? facing : Vector3.forward;
        Gizmos.DrawWireSphere(transform.position + dir * attackRange, attackRadius);
    }
}