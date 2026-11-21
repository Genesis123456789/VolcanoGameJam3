using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage = 1;

    public float projectileSpeed = 3f;

    private void FixedUpdate()
    {
        transform.position += transform.forward * projectileSpeed * Time.fixedDeltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerController>().TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
