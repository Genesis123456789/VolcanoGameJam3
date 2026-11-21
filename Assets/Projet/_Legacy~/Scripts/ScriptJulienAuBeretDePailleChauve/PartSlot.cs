using UnityEngine;

public class PartSlot : MonoBehaviour
{
    public PuppetPartEnum partSlot;
    public bool isEquiped;

    public void EquipPart(Part part)
    {
        if (isEquiped) return;
        isEquiped = true;
        part.GetComponent<RectTransform>().position = transform.position;
    }

    public void UnEquipPart(Part part)
    {
        PlayerManager.instance.AddPartToInventory(part);
        isEquiped = false;
    }
}
