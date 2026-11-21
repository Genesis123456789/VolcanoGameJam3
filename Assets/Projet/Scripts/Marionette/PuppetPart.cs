using System.Collections.Generic;
using UnityEngine;

public abstract class PuppetPart : MonoBehaviour
{

    public PuppetPartEnum slot;
    public List<PuppetBoost> puppetBoosts = new List<PuppetBoost>();


}




[System.Serializable]
public struct PuppetBoost
{
    public PuppetStatEnum stat;
    public int value;
}
