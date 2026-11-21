using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attache les pièces (Head, Body, Arm, Leg) aux bones d'un rig Humanoid.
/// Les slots (un par PuppetPartEnum) sont remplis dans l'inspecteur : bone + offset de placement.
/// </summary>
public class BodyAssembleur : MonoBehaviour
{
    public Animator animator;
    public BodyPartSlot[] slots;   // rempli dans l'inspecteur (6 entrées, une par PuppetPartEnum)

    [Tooltip("Supprime les colliders des pièces instanciées (évite qu'ils perturbent le Rigidbody du joueur).")]
    [SerializeField] private bool removePartColliders = false;

    private readonly Dictionary<PuppetPartEnum, BodyPartSlot> slotLookup = new Dictionary<PuppetPartEnum, BodyPartSlot>();
    private readonly Dictionary<PuppetPartEnum, GameObject> equipped = new Dictionary<PuppetPartEnum, GameObject>();

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        BuildLookup();
    }

    private void BuildLookup()
    {
        slotLookup.Clear();
        if (slots == null) return;

        foreach (var s in slots)
        {
            if (s == null) continue;

            if (slotLookup.ContainsKey(s.puppetpart))
                Debug.LogWarning($"Slot {s.puppetpart} défini plusieurs fois dans BodyAssembleur : vérifie l'enum de chaque entrée.", this);

            slotLookup[s.puppetpart] = s;
        }
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

    /// <summary>Équipe (ou remplace) la pièce d'un slot. Une pièce null retire simplement l'ancienne.</summary>
    public void Equip(PuppetPartEnum slot, PuppetPart part)
    {
        if (slotLookup.Count == 0) BuildLookup();

        if (!slotLookup.TryGetValue(slot, out var def))
        {
            Debug.LogError($"Aucun slot configuré pour {slot}", this);
            return;
        }

        // Retire l'ancienne pièce
        if (equipped.TryGetValue(slot, out var old) && old != null)
        {
            old.SetActive(false);   // disparaît tout de suite (Destroy ne s'applique qu'en fin de frame)
            Destroy(old);
        }
        equipped.Remove(slot);

        if (part == null) return;

        if (animator == null || !animator.isHuman)
        {
            Debug.LogError("BodyAssembleur : l'Animator est manquant ou n'a pas d'Avatar Humanoid valide.", this);
            return;
        }

        Transform bone = animator.GetBoneTransform(def.bone);
        if (bone == null)
        {
            Debug.LogError($"BodyAssembleur : le bone {def.bone} (slot {slot}) n'est pas mappé dans l'Avatar.", this);
            return;
        }

        GameObject go = Instantiate(part.gameObject, bone);
        go.transform.SetLocalPositionAndRotation(def.localPosition, Quaternion.Euler(def.localEuler));
        go.transform.localScale = def.localScale;

        if (removePartColliders)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>())
                Destroy(col);
        }

        equipped[slot] = go;
    }
}