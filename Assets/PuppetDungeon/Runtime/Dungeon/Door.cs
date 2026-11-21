using System.Collections;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Porte entre deux salles. Les barreaux tombent quand les ennemis repèrent le joueur
    /// et remontent quand la salle est nettoyée. Traverser une porte ouverte lance la transition.
    /// </summary>
    public class Door : MonoBehaviour
    {
        public RoomController room;
        public WallSide side;
        public Door linkedDoor;
        public Transform bars;
        public Collider blocker;
        public Light doorLight;
        public float barsOpenY = -2.6f;
        public float barsClosedY = 0f;

        /// <summary>Position (monde) où le joueur apparaît en arrivant par cette porte.</summary>
        public Vector3 ArrivalPoint => transform.position + EnumUtil.Inward(side) * 2.2f;
        /// <summary>Centre de l'ouverture au niveau du sol (monde).</summary>
        public Vector3 Center => transform.position;
        public bool IsLocked { get; private set; }
        public bool IsLinked => linkedDoor != null;

        private Coroutine anim;

        private void Start()
        {
            SetLocked(false, true);
        }

        public void SetLocked(bool locked, bool instant = false)
        {
            if (IsLocked == locked && !instant) return;
            IsLocked = locked;
            if (blocker != null) blocker.enabled = locked;
            if (doorLight != null) doorLight.color = locked ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.75f, 0.45f);
            if (bars == null) return;
            if (anim != null) StopCoroutine(anim);
            if (instant || !gameObject.activeInHierarchy)
            {
                var p = bars.localPosition;
                p.y = locked ? barsClosedY : barsOpenY;
                bars.localPosition = p;
                bars.gameObject.SetActive(locked);
            }
            else anim = StartCoroutine(Animate(locked));
        }

        private IEnumerator Animate(bool locked)
        {
            bars.gameObject.SetActive(true);
            Vector3 p = bars.localPosition;
            float from = p.y, to = locked ? barsClosedY : barsOpenY;
            float dur = locked ? 0.18f : 0.5f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                k = locked ? k * k : 1f - (1f - k) * (1f - k);
                p.y = Mathf.Lerp(from, to, k);
                bars.localPosition = p;
                yield return null;
            }
            p.y = to;
            bars.localPosition = p;
            if (locked)
            {
                Juice.Spawn(v => v.doorSlam, transform.position, Quaternion.identity);
                AudioManager.Sfx(l => l.doorSlam, 0.9f, 0.7f);
            }
            else
            {
                bars.gameObject.SetActive(false);
                AudioManager.Sfx(l => l.doorOpen, 0.7f, 1.2f);
            }
        }

        /// <summary>Appelé par le déclencheur « Passage » (DoorTriggerRelay).</summary>
        public void OnPassage(Collider other)
        {
            if (IsLocked) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.ControlLocked || player.IsDead) return;
            // On ne déclenche que si le joueur avance vers l'extérieur de la salle.
            Vector3 outward = -EnumUtil.Inward(side);
            Vector3 rel = player.transform.position - transform.position;
            if (Vector3.Dot(rel, outward) < -0.4f) return;
            if (DungeonManager.Instance != null) DungeonManager.Instance.RequestTravel(this);
        }
    }
}
