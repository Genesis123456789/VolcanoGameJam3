using UnityEngine;

public class Marionnette : MonoBehaviour
{
    [SerializeField] private BodyAssembleur assembleur;

    // Parties du corps (prefabs)
    [SerializeField] private Head tete;
    [SerializeField] private Body corps;

    [SerializeField] private Arm bras_gauche;
    [SerializeField] private Arm bras_droit;

    [SerializeField] private Leg jambe_gauche;
    [SerializeField] private Leg jambe_droite;

    [Header("Puppet Stats (valeurs de base)")]
    [SerializeField] private int hp;
    [SerializeField] private int damages;
    [SerializeField] private int resistance;
    [SerializeField] private int speed;

    // Stats finales = base + bonus des pièces (lecture seule pour les autres scripts)
    public int Hp { get; private set; }
    public int Damages { get; private set; }
    public int Resistance { get; private set; }
    public int Speed { get; private set; }

    private void Awake()
    {
        if (assembleur == null) assembleur = GetComponentInChildren<BodyAssembleur>();
        CalculateStats();
    }

    private void Start()
    {
        if (assembleur == null)
        {
            Debug.LogError("Marionnette : aucun BodyAssembleur assigné.", this);
            return;
        }

        assembleur.Assemblement(tete, corps, bras_gauche, bras_droit, jambe_gauche, jambe_droite);
    }

    /// <summary>Recalcule les stats (utile après avoir changé une pièce).</summary>
    public void CalculateStats()
    {
        Hp = hp;
        Damages = damages;
        Resistance = resistance;
        Speed = speed;

        AddPuppetPartStats(tete);
        AddPuppetPartStats(corps);
        AddPuppetPartStats(bras_gauche);
        AddPuppetPartStats(bras_droit);
        AddPuppetPartStats(jambe_gauche);
        AddPuppetPartStats(jambe_droite);
    }

    private void AddPuppetPartStats(PuppetPart part)
    {
        if (part == null || part.puppetBoosts == null) return;

        foreach (var boost in part.puppetBoosts)
        {
            switch (boost.stat)
            {
                case PuppetStatEnum.Hp:
                    Hp += boost.value;
                    break;
                case PuppetStatEnum.Resistance:
                    Resistance += boost.value;
                    break;
                case PuppetStatEnum.Damages:
                    Damages += boost.value;
                    break;
                case PuppetStatEnum.Speed:
                    Speed += boost.value;
                    break;
            }
        }
    }
}