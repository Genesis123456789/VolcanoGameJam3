using UnityEngine;

public class PorteScript : MonoBehaviour
{
    public ElGenerator generator;
    public GameObject visual;
    public WallSide wall;
    public PorteScript linkedDoor;
    public Vector3 doorCenter; // centre de la porte (au sol) en coordonnées monde
    public Vector3 arrivalPoint; // où le joueur apparaît quand il arrive par cette porte// null = pas encore liée
    public bool linkToRoom => linkedDoor != null;

    private void Start()
    {
        if (visual != null) visual.SetActive(false);
    }

    // Le collider de la porte doit être en "Is Trigger", et le joueur taggé "Player"
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        generator.UseDoor(this);
    }
}
