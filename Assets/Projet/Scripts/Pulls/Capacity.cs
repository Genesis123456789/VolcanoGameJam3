using UnityEngine;

[CreateAssetMenu(fileName = "Capacity", menuName = "Items/NewCapacity")]
public class Capacity : ScriptableObject
{
    [SerializeField] private string capacityName;

    public string CapacityName { get => capacityName; set => capacityName = value; }
}
