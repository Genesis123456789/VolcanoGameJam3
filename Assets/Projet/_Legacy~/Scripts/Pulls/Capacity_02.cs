using UnityEngine;

[CreateAssetMenu(fileName = "Capacity", menuName = "Items/NewCapacity02")]
public class Capacity_02 : ScriptableObject
{
    [SerializeField] private string capacityName;
    [SerializeField] private PuppetStatEnum puppetsStat;

    public string CapacityName { get => capacityName; set => capacityName = value; }
    public PuppetStatEnum PuppetsStat { get => puppetsStat; set => puppetsStat = value; }
}
