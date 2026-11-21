using UnityEngine;

[CreateAssetMenu(fileName = "Capacity", menuName = "Items/NewCapacity")]
public class Capacity : ScriptableObject
{
    [SerializeField] private string capacityName;
    [SerializeField] private int hp;
    [SerializeField] private int speed;
    [SerializeField] private int endurance;
    [SerializeField] private int damage;

    public string CapacityName { get => capacityName; set => capacityName = value; }
    public int Hp { get => hp; set => hp = value; }
    public int Speed { get => speed; set => speed = value; }
    public int Endurance { get => endurance; set => endurance = value; }
    public int Damage { get => damage; set => damage = value; }
}
