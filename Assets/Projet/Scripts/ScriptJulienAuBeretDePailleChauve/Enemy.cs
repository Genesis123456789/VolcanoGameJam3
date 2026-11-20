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



    void Start()
    {

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
        yield return new WaitForSeconds(stunLockTime);
        canAttack = true;
        canMove = true;
    }

    public void Death()
    {
        Debug.Log("Chuis mort !", this);
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
