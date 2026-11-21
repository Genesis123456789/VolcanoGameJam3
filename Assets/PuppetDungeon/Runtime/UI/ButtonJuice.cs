using UnityEngine;
using UnityEngine.EventSystems;

namespace PuppetDungeon
{
    /// <summary>Petit rebond + sons sur les boutons (survol, clic).</summary>
    public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public float hoverScale = 1.05f;
        public float pressScale = 0.94f;
        private float target = 1f;

        public void OnPointerEnter(PointerEventData e) { target = hoverScale; AudioManager.Sfx(l => l.uiHover, 0.3f, 1.2f); }
        public void OnPointerExit(PointerEventData e) => target = 1f;
        public void OnPointerDown(PointerEventData e) { target = pressScale; AudioManager.Sfx(l => l.uiClick, 0.8f); }
        public void OnPointerUp(PointerEventData e) => target = hoverScale;
        public void OnSelect(BaseEventData e) => target = hoverScale;
        public void OnDeselect(BaseEventData e) => target = 1f;

        private void OnDisable()
        {
            target = 1f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float s = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
