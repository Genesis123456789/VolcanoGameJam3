using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Relaie le déclencheur de passage (enfant) vers la porte.</summary>
    public class DoorTriggerRelay : MonoBehaviour
    {
        public Door door;

        private void OnTriggerEnter(Collider other)
        {
            if (door != null) door.OnPassage(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (door != null && Time.frameCount % 10 == 0) door.OnPassage(other);
        }
    }
}
