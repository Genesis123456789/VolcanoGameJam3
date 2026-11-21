using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Anneau au sol (zone de sûreté) : rotation lente + pulsation.</summary>
    public class GroundRing : MonoBehaviour
    {
        public float spin = 12f;
        public float pulse = 0.04f;
        public float pulseSpeed = 2.2f;
        private Vector3 baseScale;

        private void Start() => baseScale = transform.localScale;

        private void Update()
        {
            transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
            float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulse;
            transform.localScale = new Vector3(baseScale.x * s, baseScale.y, baseScale.z * s);
        }
    }
}
