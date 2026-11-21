using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float hp;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float attackPower;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackShootThresold;
    [SerializeField] private float attackOutofRange;
    [SerializeField] private float stunLockTime;

    [Header("Bool")]
    [SerializeField] private bool canAttack;
    [SerializeField] private bool canMove;
    [SerializeField] private bool canAttackPlayer;
    [SerializeField] private bool canShoot;
    [SerializeField] private bool isShooting;
    [SerializeField] private bool isdead;

    [Header("Assign")]

    [SerializeField] private Transform player;
    [SerializeField] private GameObject pfbProjectile;
    [SerializeField] private Transform projectileSpawn;
    [SerializeField] private Loot pfbLoot;

    private void Awake()
    {

    }

    void Start()
    {
        SafeZoneTest.instance.AddEnemy(this);
    }

    void Update()
    {

    }

    private void FixedUpdate()
    {
        if (canMove)
        {
            Move(player);
        }

        if (canAttack)
        {
            float distPlayerEnemy = Vector3.Distance(transform.position, player.position);


            if (distPlayerEnemy <= attackOutofRange && distPlayerEnemy >= attackShootThresold && canShoot)
            {
                Shoot();
            }
            else if (distPlayerEnemy <= attackRange)
            {
                Attack();
            }
        }


    }

    public void Move(Transform player)
    {
        transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.fixedDeltaTime);
    }

    public void Attack()
    {
        StartCoroutine(AttackEnum());
    }

    IEnumerator AttackEnum()
    {
        canMove = false;
        canAttack = false;
        Debug.Log("Attack !", this);

        var _player = player.GetComponent<PlayerController>();
        if (_player && !_player.Isdead)
        {
            _player.TakeDamage((int)attackPower);
        }


        yield return new WaitForSeconds(stunLockTime);
        canAttack = true;
        canMove = true;
    }

    public void Death()
    {
        if (isdead) return;
        isdead = true;
        Debug.Log("Chuis mort !", this);
        canAttack = false;
        canMove = false;
        StopAllCoroutines();
        transform.Rotate(new Vector3(90, 0, 0));

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, float.MaxValue))
        {
            Vector3 pos = transform.position;
            float objectToFloor = hit.point.y + GetComponent<CapsuleCollider>().radius;
            transform.position = new Vector3(pos.x, objectToFloor, pos.z);
        }
        SafeZoneTest.instance.RemoveEnemy(this);

        if (pfbLoot) Instantiate(pfbLoot, transform.position + new Vector3(0,1.5f,0), Quaternion.identity);
    }

    public void SetAttackMode()
    {
        canMove = true;
        canAttack = true;
    }

    public void TakeDamage(int damage)
    {
        if (hp <= damage)
        {
            hp = 0;
            Death();
            return;
        }
        hp -= damage;
    }

    public void Shoot()
    {
        if (!projectileSpawn || !pfbProjectile)
        {
            Debug.LogWarning("Assgine prefabs project et projectile spawn !");
            return;
        }
        StartCoroutine(ShootEnum());
    }

    IEnumerator ShootEnum()
    {
        isShooting = true;
        canMove = false;
        canAttack = false;
        var p = Instantiate(pfbProjectile, projectileSpawn);
        Vector3 dir = player.position - projectileSpawn.position;
        p.transform.forward = dir.normalized;
        yield return new WaitForSeconds(stunLockTime);
        isShooting = false;
        canMove = true;
        canAttack = true;
    }

    private void OnDrawGizmos()
    {
        if (canAttack)
        {
            // Hors de porté
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, attackOutofRange);

            // Porté 
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackShootThresold);

            // Porté d'attaque
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
