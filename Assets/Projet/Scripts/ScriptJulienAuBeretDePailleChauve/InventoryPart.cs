using UnityEngine;
using UnityEngine.UI;

public class InventoryPart : MonoBehaviour
{
    private Button button;

    void Start()
    {
        
    }

    public void DoSomething()
    {
        PlayerManager.instance.RemovePartFromInventory();
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
