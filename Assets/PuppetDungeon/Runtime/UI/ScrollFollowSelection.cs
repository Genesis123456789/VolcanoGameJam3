using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>
    /// Fait défiler une ScrollRect pour garder visible l'élément sélectionné (navigation à la manette).
    /// Ajouté automatiquement aux zones défilantes de l'interface.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollFollowSelection : MonoBehaviour
    {
        private ScrollRect scroll;
        private GameObject last;
        private readonly Vector3[] corners = new Vector3[4];

        private void Awake() => scroll = GetComponent<ScrollRect>();

        private void Update()
        {
            // Stick droit : défilement direct de la zone, quelle que soit la sélection.
            var g = UnityEngine.InputSystem.Gamepad.current;
            if (g == null || scroll == null || scroll.content == null || !scroll.vertical || !scroll.IsActive()) return;
            float y = g.rightStick.ReadValue().y;
            if (Mathf.Abs(y) < 0.2f) return;
            var vp = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            float range = scroll.content.rect.height - vp.rect.height;
            if (range <= 1f) return;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + y * Time.unscaledDeltaTime * 900f / range);
        }

        private void LateUpdate()
        {
            var es = EventSystem.current;
            if (es == null || scroll == null || scroll.content == null) return;
            var sel = es.currentSelectedGameObject;
            if (sel == last) return;
            last = sel;
            if (sel == null || !sel.transform.IsChildOf(scroll.content)) return;
            var vp = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            var rt = sel.transform as RectTransform;
            if (rt == null) return;
            Canvas.ForceUpdateCanvases();
            rt.GetWorldCorners(corners);
            float top = vp.InverseTransformPoint(corners[1]).y, bottom = vp.InverseTransformPoint(corners[0]).y;
            Rect r = vp.rect;
            const float margin = 12f;
            float delta = 0f;
            if (top > r.yMax - margin) delta = top - (r.yMax - margin);
            else if (bottom < r.yMin + margin) delta = bottom - (r.yMin + margin);
            if (Mathf.Abs(delta) < 0.5f) return;
            scroll.StopMovement();
            var p = scroll.content.anchoredPosition;
            p.y -= delta;
            scroll.content.anchoredPosition = p;
        }

        /// <summary>Ajoute le suivi à toutes les zones défilantes sous « root ».</summary>
        public static void AddTo(Transform root)
        {
            if (root == null) return;
            foreach (var sr in root.GetComponentsInChildren<ScrollRect>(true))
                if (sr.GetComponent<ScrollFollowSelection>() == null) sr.gameObject.AddComponent<ScrollFollowSelection>();
        }
    }
}
