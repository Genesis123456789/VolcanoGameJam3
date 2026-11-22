using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>
    /// Confort manette pour les menus : redonne une sélection dès qu'on touche la manette (après un clic
    /// souris dans le vide, par exemple) et ferme le panneau du dessus avec B.
    /// Ajouté automatiquement sur l'EventSystem.
    /// </summary>
    public class GamepadUI : MonoBehaviour
    {
        public static void Ensure()
        {
            var es = EventSystem.current;
            if (es == null) es = FindAnyObjectByType<EventSystem>();
            if (es != null && es.GetComponent<GamepadUI>() == null) es.gameObject.AddComponent<GamepadUI>();
        }

        private void Update()
        {
            var g = Gamepad.current;
            var es = EventSystem.current;
            if (g == null || es == null) return;

            var top = UIPanel.Top;
            if (g.buttonEast.wasPressedThisFrame && top != null && top.CloseOnCancel)
            {
                top.Close();
                return;
            }

            bool navigating = g.leftStick.ReadValue().sqrMagnitude > 0.25f || g.dpad.ReadValue().sqrMagnitude > 0.25f || g.buttonSouth.wasPressedThisFrame;
            if (!navigating) return;
            var cur = es.currentSelectedGameObject;
            if (UIPanel.IsUsable(cur) && IsVisible(cur)) return;

            GameObject target = null;
            if (top != null) target = top.FirstUsable();
            else
            {
                // Menu principal (sans panneau ouvert) : premier bouton visible.
                var menu = FindAnyObjectByType<MainMenuView>();
                if (menu != null && menu.isActiveAndEnabled)
                    foreach (var s in menu.GetComponentsInChildren<Selectable>())
                        if (UIPanel.IsUsable(s.gameObject) && IsVisible(s.gameObject)) { target = s.gameObject; break; }
            }
            if (target != null) es.SetSelectedGameObject(target);
        }

        /// <summary>Pas dans un panneau fermé (CanvasGroup transparent).</summary>
        private static bool IsVisible(GameObject go)
        {
            foreach (var cg in go.GetComponentsInParent<CanvasGroup>())
            {
                if (cg.alpha <= 0.01f || !cg.interactable) return false;
                if (cg.ignoreParentGroups) break;
            }
            return true;
        }
    }
}
