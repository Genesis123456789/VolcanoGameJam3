using System.Collections.Generic;
using UnityEngine;

public class SafeZoneTest : MonoBehaviour
{
    public static SafeZoneTest instance;

    public static List<Enemy> Enemies = new List<Enemy>();

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(this);
    }

    void Start()
    {

    }

    void Update()
    {

    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("SafeZone"))
        {
            Debug.Log(other.gameObject, this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SafeZone"))
        {
            Debug.Log("Enemies Attack !", this);
            foreach (Enemy enemy in Enemies)
            {
                enemy.SetAttackMode();
            }
        }
    }

    public void AddEnemy(Enemy enemy) => Enemies.Add(enemy);
    public void RemoveEnemy(Enemy enemy) => Enemies.Remove(enemy);
}
