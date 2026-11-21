using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Compatibilité avec « Enter Play Mode Options » (rechargement du domaine désactivé) :
    /// les champs statiques survivent entre deux Play, on les remet à zéro au démarrage.
    /// </summary>
    internal static class StaticReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            EnemyController.ResetStatics();
            Interactable.ResetStatics();
            Layers.ResetStatics();
            MaterialFactory.ClearCache();
            PartIconRenderer.ResetStatics();
        }
    }
}
