using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance;

    public int inventoryLimit = 20;
    [SerializeField] int currentParts = 0;

    public PartSlot head;
    public PartSlot chest;
    public PartSlot leftArm;
    public PartSlot rightArm;
    public PartSlot leftLeg;
    public PartSlot rightLeg;

    public CanvasGroup canvasInventory;
    public RectTransform lParts;
    public RectTransform wParts;

    public Part pfbPartInventory;


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
        Instantiate(pfbPartInventory, lParts.transform);
        currentParts++;
    }
    public void AddPartToInventory(Part part)
    {
        if (currentParts >= inventoryLimit)
        {
            currentParts = inventoryLimit;
            return;
        }
        part.transform.SetParent(lParts);
        currentParts++;
    }

    public void RemovePartFromInventory()
    {
        if (currentParts == 0) return;

        currentParts--;
    }
    public void RemovePartFromInventory(Part part)
    {
        if (currentParts == 0) return;
        part.transform.SetParent(wParts);
        currentParts--;
    }

    public void WearPart(Part part)
    {
        switch (part.part)
        {
            case PuppetPartEnum.Head:
                head.EquipPart(part);
                break;
            case PuppetPartEnum.Torso:
                chest.EquipPart(part);
                break;
            case PuppetPartEnum.LeftArm:
                leftArm.EquipPart(part);
                break;
            case PuppetPartEnum.RightArm:
                rightArm.EquipPart(part);
                break;
            case PuppetPartEnum.LeftLeg:
                leftLeg.EquipPart(part);
                break;
            case PuppetPartEnum.RightLeg:
                rightLeg.EquipPart(part);
                break;
            default:
                break;
        }

        RemovePartFromInventory(part);
    }

    public void RemovePart(Part part)
    {
        switch (part.part)
        {
            case PuppetPartEnum.Head:
                head.UnEquipPart(part);
                break;
            case PuppetPartEnum.Torso:
                chest.UnEquipPart(part);
                break;
            case PuppetPartEnum.LeftArm:
                leftArm.UnEquipPart(part);
                break;
            case PuppetPartEnum.RightArm:
                rightArm.UnEquipPart(part);
                break;
            case PuppetPartEnum.LeftLeg:
                leftLeg.UnEquipPart(part);
                break;
            case PuppetPartEnum.RightLeg:
                rightLeg.UnEquipPart(part);
                break;
            default:
                break;
        }
        
        
        AddPartToInventory(part);
    }

    public bool CanEquip(Part part)
    {
        switch (part.part)
        {
            case PuppetPartEnum.Head:
                return !head.isEquiped;
            case PuppetPartEnum.Torso:
                return !chest.isEquiped;
            case PuppetPartEnum.LeftArm:
                return !leftArm.isEquiped;
            case PuppetPartEnum.RightArm:
                return !rightArm.isEquiped;
            case PuppetPartEnum.LeftLeg:
                return !leftLeg.isEquiped;
            case PuppetPartEnum.RightLeg:
                return !rightLeg.isEquiped;
            default:
                break;
        }

        return false;
    }
}
