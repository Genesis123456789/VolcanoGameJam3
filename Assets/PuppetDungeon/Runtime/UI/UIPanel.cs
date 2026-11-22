using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PuppetDungeon
{
    /// <summary>Panneau avec ouverture/fermeture animée (alpha + léger rebond d'échelle).</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanel : MonoBehaviour
    {
        public RectTransform animatedRoot;
        public GameObject firstSelected;
        public float openDuration = 0.22f;

        public bool IsOpen { get; private set; }
        protected CanvasGroup group;
        private Coroutine anim;
        private GameObject selectionBeforeOpen;

        private static readonly System.Collections.Generic.List<UIPanel> openPanels = new System.Collections.Generic.List<UIPanel>();

        internal static void ResetStatics() => openPanels.Clear();

        /// <summary>Panneau ouvert le plus récent (celui qui reçoit la manette).</summary>
        public static UIPanel Top
        {
            get
            {
                for (int i = openPanels.Count - 1; i >= 0; i--)
                {
                    var p = openPanels[i];
                    if (p == null || !p.IsOpen || !p.isActiveAndEnabled) { openPanels.RemoveAt(i); continue; }
                    return p;
                }
                return null;
            }
        }

        /// <summary>Le bouton Retour de la manette (B) ferme ce panneau.</summary>
        public virtual bool CloseOnCancel => true;

        protected virtual void Awake()
        {
            group = GetComponent<CanvasGroup>();
            IsOpen = false;
            SetVisible(false, true);
        }

        public virtual void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            selectionBeforeOpen = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            openPanels.Remove(this);
            openPanels.Add(this);
            gameObject.SetActive(true);
            SetVisible(true, false);
            AudioManager.Sfx(l => l.uiClick, 0.6f, 0.9f);
            if (firstSelected != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(firstSelected);
            GamepadUI.Ensure();
            OnOpened();
        }

        public virtual void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            openPanels.Remove(this);
            SetVisible(false, false);
            AudioManager.Sfx(l => l.uiClick, 0.5f, 0.75f);
            OnClosed();
            RestoreSelection();
        }

        private void OnDisable() => openPanels.Remove(this);

        /// <summary>Rend la main manette à ce qui était sélectionné avant l'ouverture (ex : bouton Options du menu pause).</summary>
        private void RestoreSelection()
        {
            var es = EventSystem.current;
            if (es == null) return;
            var cur = es.currentSelectedGameObject;
            if (cur != null && cur.activeInHierarchy && !cur.transform.IsChildOf(transform)) return;
            GameObject target = null;
            if (IsUsable(selectionBeforeOpen)) target = selectionBeforeOpen;
            else
            {
                var top = Top;
                if (top != null) target = top.FirstUsable();
            }
            es.SetSelectedGameObject(target);
            selectionBeforeOpen = null;
        }

        /// <summary>Premier élément sélectionnable du panneau (firstSelected, sinon le premier actif).</summary>
        public GameObject FirstUsable()
        {
            if (IsUsable(firstSelected)) return firstSelected;
            foreach (var s in GetComponentsInChildren<UnityEngine.UI.Selectable>())
                if (IsUsable(s.gameObject)) return s.gameObject;
            return null;
        }

        public static bool IsUsable(GameObject go)
        {
            if (go == null || !go.activeInHierarchy) return false;
            var s = go.GetComponent<UnityEngine.UI.Selectable>();
            return s == null || s.IsInteractable();
        }

        public void Toggle()
        {
            if (IsOpen) Close(); else Open();
        }

        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }

        private void SetVisible(bool visible, bool instant)
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            group.interactable = visible;
            group.blocksRaycasts = visible;
            if (anim != null) StopCoroutine(anim);
            if (instant || !gameObject.activeInHierarchy)
            {
                group.alpha = visible ? 1f : 0f;
                if (animatedRoot != null) animatedRoot.localScale = Vector3.one;
                return;
            }
            anim = StartCoroutine(Animate(visible));
        }

        private IEnumerator Animate(bool visible)
        {
            float start = group.alpha, end = visible ? 1f : 0f;
            for (float t = 0; t < openDuration; t += Time.unscaledDeltaTime)
            {
                float k = t / openDuration;
                group.alpha = Mathf.Lerp(start, end, k);
                if (animatedRoot != null)
                {
                    float s = visible ? 0.92f + 0.08f * EaseOutBack(k) : Mathf.Lerp(1f, 0.95f, k);
                    animatedRoot.localScale = Vector3.one * s;
                }
                yield return null;
            }
            group.alpha = end;
            if (animatedRoot != null) animatedRoot.localScale = Vector3.one;
        }

        public static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
