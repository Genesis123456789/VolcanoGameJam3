using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Scintillement de flamme (bruit de Perlin).</summary>
    [RequireComponent(typeof(Light))]
    public class LanternFlicker : MonoBehaviour
    {
        public float amount = 0.25f;
        public float speed = 3f;
        private Light l;
        private float baseIntensity;
        private float seed;

        private void Awake()
        {
            l = GetComponent<Light>();
            seed = Random.Range(0f, 100f);
        }

        private void Start() => baseIntensity = l.intensity;

        private void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed);
            l.intensity = baseIntensity * (1f - amount + n * amount * 2f);
        }
    }
}
