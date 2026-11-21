using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;

public class PullsManager : MonoBehaviour
{
    [SerializeField] protected float _cardDropSpeed;
    [SerializeField] private RarityLootTable lootTable;
    [SerializeField] private GameObject canvasPull;

    private bool isActive = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(canvasPull !=null)
        {
            canvasPull.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DoPull()
    {
        StartCoroutine(Do_Pull());

    }

    IEnumerator Do_Pull()
    {
        if (!isActive)
        {
            isActive = true;
            canvasPull.SetActive(true);
            TextMeshProUGUI[] children = canvasPull.gameObject.GetComponentsInChildren<TextMeshProUGUI>();
            RarityRewardItem chosenCard = lootTable.GetRandomItem(lootTable);
            children[0].text = chosenCard._Capacity.CapacityName;
            children[1].text = chosenCard._Capacity.Hp.ToString();
            children[2].text = chosenCard._Capacity.Speed.ToString();
            children[3].text = chosenCard._Capacity.Endurance.ToString();
            children[4].text = chosenCard._Capacity.Damage.ToString();

            yield return new WaitForSeconds(2);

            isActive = false;
            canvasPull.SetActive(false);
        }
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
