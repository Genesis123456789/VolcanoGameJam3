using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuppetDungeon
{
    /// <summary>Navigation manette explicite pour les grilles de cartes (offrande, gacha).</summary>
    public static class UINav
    {
        /// <summary>Nombre de colonnes d'une grille (GridLayoutGroup à colonnes fixes), sinon la valeur par défaut.</summary>
        public static int Columns(RectTransform grid, int fallback)
        {
            var gl = grid != null ? grid.GetComponent<GridLayoutGroup>() : null;
            return gl != null && gl.constraint == GridLayoutGroup.Constraint.FixedColumnCount ? Mathf.Max(1, gl.constraintCount) : Mathf.Max(1, fallback);
        }

        /// <summary>
        /// Relie les éléments d'une grille entre eux (haut/bas/gauche/droite), la dernière ligne vers « below »
        /// et la première ligne vers « above ». Retourne le premier élément de la dernière ligne (pour remonter).
        /// </summary>
        public static Selectable WireGrid(IList<Selectable> items, int columns, Selectable below, Selectable above)
        {
            int n = items.Count;
            if (n == 0) return null;
            columns = Mathf.Max(1, columns);
            int lastRow = (n - 1) / columns;
            for (int i = 0; i < n; i++)
            {
                var s = items[i];
                if (s == null) continue;
                int row = i / columns, col = i % columns;
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnLeft = col > 0 ? items[i - 1] : null;
                nav.selectOnRight = col < columns - 1 && i + 1 < n ? items[i + 1] : null;
                nav.selectOnUp = row > 0 ? items[i - columns] : above;
                if (row < lastRow) nav.selectOnDown = items[Mathf.Min(i + columns, n - 1)];
                else nav.selectOnDown = below;
                s.navigation = nav;
            }
            return items[lastRow * columns];
        }
    }
}
