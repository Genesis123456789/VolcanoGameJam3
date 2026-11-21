using UnityEditor.Animations;
using UnityEngine;

public class SimpleAttackZone : MonoBehaviour
{
    Animator ac;

    private void Awake()
    {
        ac = GetComponent<Animator>();
    }

    void Start()
    {
        
    }

    void Update()
    {
        //transform.position = transform.parent.gameObject.transform.forward;
    }

    public void PlayAnimationAttackZone()
    {
        ac.SetTrigger("Attack");
    }
}
