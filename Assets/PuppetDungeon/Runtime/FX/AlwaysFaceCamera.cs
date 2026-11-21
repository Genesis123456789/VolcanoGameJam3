using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Oriente l'objet vers la caméra (marqueur « ! », textes).</summary>
    public class AlwaysFaceCamera : MonoBehaviour
    {
        public float bobAmplitude = 0.1f;
        public float bobSpeed = 8f;
        private Vector3 basePos;

        private void Start() => basePos = transform.localPosition;

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
            transform.localPosition = basePos + Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * bobSpeed)) * bobAmplitude;
        }
    }
}
