using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PuppetDungeon
{
    /// <summary>Objet utilisable avec la touche Interagir (escalier, villageois, coffre…).</summary>
    public class Interactable : MonoBehaviour
    {
        private static readonly List<Interactable> all = new List<Interactable>();
        internal static void ResetStatics() => all.Clear();

        public string prompt = "Interagir";
        public float radius = 2f;
        public bool interactable = true;
        public UnityEvent onInteract = new UnityEvent();

        public System.Action Interacted;

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        public static Interactable Nearest(Vector3 pos)
        {
            Interactable best = null;
            float bestD = float.MaxValue;
            foreach (var i in all)
            {
                if (i == null || !i.interactable || !i.isActiveAndEnabled) continue;
                Vector3 d = i.transform.position - pos; d.y = 0f;
                float m = d.magnitude;
                if (m <= i.radius && m < bestD) { best = i; bestD = m; }
            }
            return best;
        }

        public static bool TryInteractNearest(Vector3 pos)
        {
            var i = Nearest(pos);
            if (i == null) return false;
            i.Interact();
            return true;
        }

        public void Interact()
        {
            if (!interactable) return;
            AudioManager.Sfx(l => l.uiClick, 0.6f);
            Interacted?.Invoke();
            onInteract.Invoke();
        }
    }
}
