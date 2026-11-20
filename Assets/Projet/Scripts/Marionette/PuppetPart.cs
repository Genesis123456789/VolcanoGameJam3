using System.Collections.Generic;
using UnityEngine;

public abstract class PuppetPart : MonoBehaviour
{

    public PuppetPartEnum slot;
    [SerializeField] public Dictionary<PuppetStatEnum, int> puppetBoosts;

    
}
