using System.Collections;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Fait disparaître un débris après quelques secondes.</summary>
    public class DebrisLife : MonoBehaviour
    {
        public float life = 5f;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(life + Random.Range(0f, 1.5f));
            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            Vector3 s = transform.localScale;
            for (float t = 0; t < 0.6f; t += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(s, Vector3.zero, t / 0.6f);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
