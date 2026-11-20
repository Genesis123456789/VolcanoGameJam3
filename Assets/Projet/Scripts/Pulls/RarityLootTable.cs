using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "RarityLootTable", menuName = "Items/Rarity Loot Table")]
public class RarityLootTable : ScriptableObject
{
    public List<RarityRewardItem> items;

    [System.NonSerialized] private bool isInitialized = false;

    public float totalWeight;

    public void Initialize()
    {
        if (!isInitialized)
        {
            totalWeight = items.Sum(i => i._Weight);

            isInitialized = true;
        }
    }


    /*public RarityRewardItem GetRarityRandomItem()
    {
        Initialize();

        float diceRoll = Random.Range(0f, totalWeight);

        foreach (var item in items)
        {
            if (item._Weight >= diceRoll)
            {
                return GetRandomItem(item.rarityLootTable);
            }

            diceRoll -= item._Weight;
        }

        throw new System.Exception("Rarity generation failed!");
    }*/

    public RarityRewardItem GetRandomItem(RarityLootTable rewardLootTable)
    {
        rewardLootTable.Initialize();

        List<RarityRewardItem> rewardItems = rewardLootTable.items;
        float rewardsTotalWeight = rewardLootTable.totalWeight;

        float diceRoll = Random.Range(0f, rewardsTotalWeight);

        foreach (var item in rewardItems)
        {
            if (item._Weight >= diceRoll)
            {
                return item;
            }

            diceRoll -= item._Weight;
        }

        throw new System.Exception("Card generation failed!");
    }
}
