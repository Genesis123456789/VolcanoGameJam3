using System.Collections;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Marionnette du village (PNJ) : marche, salue, se balance au bout de ses fils.</summary>
    public class VillagerNPC : MonoBehaviour
    {
        public PuppetRig rig;
        public string villagerName = "Marionnette";
        public bool sway = true;

        private float seed;

        private void Awake()
        {
            if (rig == null) rig = GetComponentInChildren<PuppetRig>();
            seed = Random.Range(0f, 10f);
        }

        private void Update()
        {
            if (!sway || rig == null) return;
            rig.transform.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.1f + seed) * 2f, 0f, Mathf.Sin(Time.time * 0.8f + seed) * 3f);
        }

        public IEnumerator WalkTo(Vector3 target, float speed)
        {
            target.y = transform.position.y;
            while (true)
            {
                Vector3 d = target - transform.position;
                d.y = 0f;
                if (d.magnitude < 0.05f) break;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d.normalized, Vector3.up), 540f * Time.deltaTime);
                transform.position += d.normalized * Mathf.Min(d.magnitude, speed * Time.deltaTime);
                if (rig != null) rig.SetSpeed(0.35f, 0.1f);
                yield return null;
            }
            if (rig != null) rig.SetSpeed(0f, 0f);
        }

        public void Face(Vector3 worldPos)
        {
            Vector3 d = worldPos - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }
    }
}
