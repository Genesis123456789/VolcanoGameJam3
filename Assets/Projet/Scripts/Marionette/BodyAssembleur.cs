using System.Collections.Generic;
using UnityEngine;

public class BodyAssembleur : MonoBehaviour
{
    public Animator animator;
    public BodyPartSlot[] slots;   // rempli dans l'inspecteur (6 entrées)

    readonly Dictionary<PuppetPartEnum, BodyPartSlot> slotLookup = new();
    readonly Dictionary<PuppetPartEnum, GameObject> equipped = new();

    void Awake()
    {
        foreach (var s in slots) slotLookup[s.puppetpart] = s;
    }

    public void Assemblement(Head head, Body body, Arm leftArm, Arm rightArm, Leg leftLeg, Leg rightLeg)
    {
        Equip(PuppetPartEnum.Head, head);
        Equip(PuppetPartEnum.Torso, body);
        Equip(PuppetPartEnum.LeftArm, leftArm);
        Equip(PuppetPartEnum.RightArm, rightArm);
        Equip(PuppetPartEnum.LeftLeg, leftLeg);
        Equip(PuppetPartEnum.RightLeg, rightLeg);
    }

    public void Equip(PuppetPartEnum slot, PuppetPart part)
    {
        if (!slotLookup.TryGetValue(slot, out var def))
        {
            Debug.LogError($"Aucun slot configuré pour {slot}", this);
            return;
        }

        if (equipped.TryGetValue(slot, out var old)) Destroy(old);

        var bone = animator.GetBoneTransform(def.bone);
        var go = Instantiate(part.gameObject, bone);
        go.transform.SetLocalPositionAndRotation(def.localPosition, Quaternion.Euler(def.localEuler));
        go.transform.localScale = def.localScale;

        equipped[slot] = go;
    }
}