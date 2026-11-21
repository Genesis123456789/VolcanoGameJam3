using UnityEngine;

public class PlayerManager : MonoBehaviour
{

    public static PlayerManager instance;

    public Canvas canvasInventory;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        canvasInventory?.gameObject.SetActive(false);
    }

    public void SetInventory()
    {
        canvasInventory.gameObject.SetActive(!canvasInventory.isActiveAndEnabled);
    }
    public void SetInventory(bool value)
    {
        canvasInventory.gameObject.SetActive(value);
    }
}
