using System;
using System.ComponentModel.Design;
using UnityEditor.Search;
using UnityEngine;

public class Marionnette : MonoBehaviour
{
    [SerializeField] private BodyAssembleur assembleur;

    //Parties du corps 
    [SerializeField] private Head tete;
    [SerializeField] private Body corps;

    [SerializeField] private Arm bras_gauche;
    [SerializeField] private Arm bras_droit;

    [SerializeField] private Leg jambe_gauche;
    [SerializeField] private Leg jambe_droite;


    [Header("Puppet Stats")]
    [SerializeField] private int hp;
    [SerializeField] private int damages;

    [SerializeField] private int resistance;
    [SerializeField] private int speed;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        assembleur.Assemblement(tete,corps,bras_gauche,bras_droit,jambe_gauche,jambe_droite);
        CalculateStats();
    }



    private void CalculateStats()
    {
        AddPuppetPartStats(tete);
        AddPuppetPartStats(corps);
        AddPuppetPartStats(bras_gauche);
        AddPuppetPartStats(bras_droit);
        AddPuppetPartStats(jambe_gauche);
        AddPuppetPartStats(jambe_droite);

    }


    private void AddPuppetPartStats(PuppetPart part)
    {
        foreach (var item in part.puppetBoosts)
        {
            switch (item.Key)
            {
                case PuppetStatEnum.Hp:
                    hp += item.Value;
                    break;
                case PuppetStatEnum.Resistance:
                    resistance += item.Value;
                    break;
                case PuppetStatEnum.Damages:
                    damages += item.Value;
                    break;
                case PuppetStatEnum.Speed:
                    speed += item.Value;
                    break;
            }
        }
    }

    

}
