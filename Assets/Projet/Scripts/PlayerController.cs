using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerController top-down façon Zelda (2D).
/// Requiert : Rigidbody2D + PlayerInput (avec PlayerControls.inputactions assigné).
/// Animator optionnel (paramètres : MoveX, MoveY, Speed, Attack).
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Déplacement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Attaque")]
    [SerializeField] private float attackDuration = 0.3f;   // durée pendant laquelle le joueur est bloqué
    [SerializeField] private float attackCooldown = 0.1f;
    [SerializeField] private float attackRange = 0.8f;      // distance du centre de la hitbox
    [SerializeField] private float attackRadius = 0.5f;     // taille de la hitbox
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private LayerMask hittableLayers;

    [Header("Références (optionnel)")]
    [SerializeField] private Animator animator;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction attackAction;

    private Vector2 moveInput;
    private Vector2 facing = Vector2.down; // direction regardée (pour l'attaque et l'idle)
    private bool isAttacking;
    private float nextAttackTime;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        //rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        attackAction = playerInput.actions["Attack"];

        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        // Mémorise la dernière direction (sur 4 axes, style Zelda)
        if (moveInput.sqrMagnitude > 0.01f && !isAttacking)
            facing = ToFourDirections(moveInput);

        if (attackAction.WasPressedThisFrame() && !isAttacking && Time.time >= nextAttackTime)
            StartCoroutine(AttackRoutine());

        //UpdateAnimator();
    }

    private void FixedUpdate()
    {

        Vector3 _moveInput = new Vector3(moveInput.x,0, moveInput.y);
        Vector3 velocity = isAttacking ? Vector3.zero : Vector3.ClampMagnitude(_moveInput, 1f) * moveSpeed;
        rb.linearVelocity = velocity; // Unity 6. Sur Unity 2022 ou avant : rb.velocity
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        if (animator) animator.SetTrigger(AttackHash);

        // Hitbox devant le joueur
        Vector2 hitPoint = (Vector2)transform.position + facing * attackRange;
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitPoint, attackRadius, hittableLayers);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            // Tout script ennemi avec une méthode "TakeDamage(int)" recevra l'appel
            hit.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }

        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    /*private void UpdateAnimator()
    {
        if (!animator) return;
        animator.SetFloat(MoveXHash, facing.x);
        animator.SetFloat(MoveYHash, facing.y);
        animator.SetFloat(SpeedHash, isAttacking ? 0f : moveInput.magnitude);
    }*/

    private static Vector2 ToFourDirections(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x > 0 ? Vector3.right : Vector3.left;
        return dir.y > 0 ? Vector3.forward : Vector3.back;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 dir = Application.isPlaying ? facing : Vector2.down;
        Gizmos.DrawWireSphere((Vector2)transform.position + dir * attackRange, attackRadius);
    }
}
