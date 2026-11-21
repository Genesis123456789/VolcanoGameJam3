using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager instance;

    


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









}
