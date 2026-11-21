using UnityEngine;

public class Loot : MonoBehaviour
{
    private void Update()
    {
        Transform tVisual = transform.GetChild(0);
        tVisual.Rotate(0, 30f * Time.deltaTime,0);
    }

    public void LootLoot()
    {
        Debug.Log("Loot Récolté !", this);
        Destroy(gameObject);
    }
}
