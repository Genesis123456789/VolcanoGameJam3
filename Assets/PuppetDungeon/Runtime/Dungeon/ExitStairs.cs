using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Trappe de sortie du donjon. S'ouvre quand la salle est nettoyée.</summary>
    [RequireComponent(typeof(Interactable))]
    public class ExitStairs : MonoBehaviour
    {
        public GameObject lockedVisual;
        public GameObject openVisual;
        public Light glow;

        private Interactable interactable;
        private bool active;

        private void Awake()
        {
            interactable = GetComponent<Interactable>();
            interactable.Interacted += Use;
            interactable.interactable = false;
            Configure(lockedVisual, openVisual, glow);
        }

        public void Configure(GameObject locked, GameObject open, Light light)
        {
            lockedVisual = locked;
            openVisual = open;
            glow = light;
            if (active) return;
            if (lockedVisual != null) lockedVisual.SetActive(true);
            if (openVisual != null) openVisual.SetActive(false);
            if (glow != null) glow.enabled = false;
        }

        public void Activate()
        {
            if (active) return;
            active = true;
            interactable.interactable = true;
            int next = GameManager.Instance.Run.dungeonIndex + 1;
            int per = Mathf.Max(1, GameManager.Instance.Config.dungeonsPerVillage);
            interactable.prompt = next % per == 0 ? "Descendre vers le village" : "Descendre plus bas";
            if (lockedVisual != null) lockedVisual.SetActive(false);
            if (openVisual != null) openVisual.SetActive(true);
            if (glow != null) glow.enabled = true;
            Juice.Spawn(v => v.lootBurst, transform.position + Vector3.up * 0.3f, Quaternion.identity, new Color(1f, 0.8f, 0.4f), 1.5f);
            if (UIRoot.Instance != null) UIRoot.Instance.Toast("Une trappe s'est ouverte…", new Color(1f, 0.85f, 0.5f));
        }

        private void Use()
        {
            if (!active) return;
            interactable.interactable = false;
            var player = PlayerController.Instance;
            if (player != null) player.Lock(true);
            GameManager.Instance.CompleteDungeon();
        }

        private void Update()
        {
            if (glow != null && active) glow.intensity = 2.5f + Mathf.Sin(Time.time * 3f) * 0.6f;
        }
    }
}
