using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float hp;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float attackPower;
    [SerializeField] private float attackRange;
    [SerializeField] private float stunLockTime;


    [SerializeField] private bool canAttack;
    [SerializeField] private bool canMove;
    [SerializeField] private bool canAttackPlayer;

    [SerializeField] private Transform player;

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

        if (Vector3.Distance(transform.position, player.position) < attackRange && canAttack)
        {
            Attack();
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

    private void OnDrawGizmos()
    {
        if (canAttack)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
