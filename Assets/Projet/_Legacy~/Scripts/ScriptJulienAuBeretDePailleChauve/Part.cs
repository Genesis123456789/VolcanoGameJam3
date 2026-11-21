using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Part : MonoBehaviour, IPointerDownHandler
{

    public PuppetPartEnum part;

    private TextMeshProUGUI text;

    public bool isEquiped;

    void Start()
    {
        part = (PuppetPartEnum)Random.Range(0, 6);

        text = GetComponentInChildren<TextMeshProUGUI>();
        text.text = part.ToString();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (isEquiped)
            {
                isEquiped = false;
                PlayerManager.instance.RemovePart(this);
            }
            else
            {
                PlayerManager.instance.RemovePartFromInventory();
                Destroy(gameObject);
            }
        }


        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (isEquiped || !PlayerManager.instance.CanEquip(this)) return;
            PlayerManager.instance.WearPart(this);
            Debug.Log("équiper: " + part.ToString());
            isEquiped = true;
        }
    }
}
