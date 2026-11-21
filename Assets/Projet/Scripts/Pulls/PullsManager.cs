using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PullsManager : MonoBehaviour
{
    [SerializeField] protected float _cardDropSpeed;
    [SerializeField] private RarityLootTable lootTable;

    private bool isActive = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DoPull()
    {
        RarityRewardItem chosenCard = lootTable.GetRandomItem(lootTable);

        Debug.Log(chosenCard._Capacity.CapacityName);
    }

    private IEnumerator DoRarityPull(RarityLootTable lootTable, int pullNumber = 1)
    {
        if (isActive) yield break;

        isActive = true;

        for (int pull = 0; pull < pullNumber; pull++)
        {
            RarityRewardItem chosenCard = lootTable.GetRandomItem(lootTable);

            Debug.Log(chosenCard._Capacity.CapacityName);
            
            yield return new WaitForSeconds(_cardDropSpeed);
        }

        yield return new WaitForSeconds(_cardDropSpeed);
    }
}
