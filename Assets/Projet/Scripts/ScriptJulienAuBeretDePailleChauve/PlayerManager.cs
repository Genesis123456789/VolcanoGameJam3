using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance;

    public int inventoryLimit = 20;
    [SerializeField] int currentParts = 0;




    public CanvasGroup canvasInventory;
    public RectTransform yes;

    public InventoryPart pfbPartInventory;

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
        canvasInventory.alpha = 0f;
    }

    public void SetInventory()
    {
        bool value;

        if (canvasInventory.alpha == 0)
        {
            value = true;
        }
        else
        {
            value = false;
        }


        SetInventory(value);
       // canvasInventory.al.SetActive(!canvasInventory.isActiveAndEnabled);
    }
    public void SetInventory(bool value)
    {
        canvasInventory.alpha = value ? 1f : 0f;
    }

    public void AddPartToInventory()
    {
        if (currentParts >= inventoryLimit)
        {
            currentParts = inventoryLimit;
            return;
        }
        Instantiate(pfbPartInventory, yes.transform);
        currentParts++;
    }

    public void RemovePartFromInventory()
    {
        if (currentParts == 0) return;

        currentParts--;
    }
}
