using UnityEngine;

[System.Serializable]
public class BodyPartSlot 
{
    public PuppetPartEnum puppetpart;

    public HumanBodyBones bone;
    public Vector3 localPosition;    // offset par rapport au bone
    public Vector3 localEuler;
    public Vector3 localScale = Vector3.one;
}
