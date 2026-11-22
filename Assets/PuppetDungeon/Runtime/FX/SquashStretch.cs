using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Ressort d'échelle : un « punch » écrase/étire puis revient à la normale.</summary>
    public class SquashStretch : MonoBehaviour
    {
        public float stiffness = 180f;
        public float damping = 14f;

        private Vector3 baseScale = Vector3.one;
        private Vector3 offset;
        private Vector3 velocity;

        private void Awake() => baseScale = transform.localScale;

        /// <summary>xz : écart horizontal, y : écart vertical. Ex : (0.25, -0.25, 0.25) pour écraser.</summary>
        public void Punch(Vector3 delta)
        {
            // Les punchs successifs ne s'additionnent pas sans fin (ex : plusieurs pièces équipées d'affilée).
            offset = Vector3.ClampMagnitude(offset + delta, 0.35f);
        }

        /// <summary>Revient immédiatement à l'échelle de repos.</summary>
        public void ResetNow()
        {
            offset = Vector3.zero;
            velocity = Vector3.zero;
            transform.localScale = baseScale;
        }

        private void OnDisable() => ResetNow();

        private void LateUpdate()
        {
            // Temps réel : le ressort revient au repos même jeu en pause (aperçu du pantin dans l'inventaire).
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            Vector3 force = -stiffness * offset - damping * velocity;
            velocity += force * dt;
            offset += velocity * dt;
            transform.localScale = Vector3.Scale(baseScale, Vector3.one + offset);
        }
    }
}
