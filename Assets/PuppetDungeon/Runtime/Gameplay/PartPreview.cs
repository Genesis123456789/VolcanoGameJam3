using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Construit une copie « posée » d'un visuel de pièce (butin au sol, icônes).</summary>
    public static class PartPreview
    {
        public static GameObject Build(PartVisual prefab, Transform parent, float targetSize)
        {
            if (prefab == null) return null;
            var inst = Object.Instantiate(prefab, parent);
            inst.enabled = false;
            var root = inst.gameObject;
            // Les groupes sont déjà positionnés comme dans la pose de liaison : on recentre l'ensemble.
            var rends = root.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return root;
            Bounds b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
            float scale = size > 0.0001f ? targetSize / size : 1f;
            root.transform.localScale *= scale;
            // Recalcul après mise à l'échelle
            b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            root.transform.position += parent.position - b.center;
            foreach (var r in rends)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.gameObject.layer = parent.gameObject.layer;
            }
            return root;
        }
    }
}
