using UnityEngine;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>Icône qui suit le pointeur pendant un glisser-déposer.</summary>
    public class DragGhost : MonoBehaviour
    {
        public Image icon;
        public Image glow;
        private RectTransform rt;
        private Canvas canvas;

        private void Awake()
        {
            rt = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
            Hide();
        }

        public void Show(Sprite sprite, Color rarity)
        {
            gameObject.SetActive(true);
            if (icon != null) { icon.sprite = sprite; icon.enabled = sprite != null; icon.preserveAspect = true; }
            if (glow != null) { rarity.a = 0.6f; glow.color = rarity; }
            transform.SetAsLastSibling();
        }

        public void Move(Vector2 screenPos)
        {
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            var parent = (RectTransform)rt.parent;
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, cam, out var local))
                rt.anchoredPosition = local;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 10f) * 4f);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
