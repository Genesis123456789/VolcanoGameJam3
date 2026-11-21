using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Rend automatiquement une icône pour chaque pièce sans icône (caméra hors-champ → RenderTexture).
    /// Ajouter du contenu ne demande donc pas de dessiner d'icône.
    /// </summary>
    public class PartIconRenderer : MonoBehaviour
    {
        private static PartIconRenderer instance;
        private static readonly Dictionary<PartDefinition, Sprite> cache = new Dictionary<PartDefinition, Sprite>();
        private static readonly Dictionary<PartDefinition, List<Action<Sprite>>> pending = new Dictionary<PartDefinition, List<Action<Sprite>>>();

        internal static void ResetStatics()
        {
            cache.Clear();
            pending.Clear();
            instance = null;
        }

        private const int Size = 160;
        private static readonly Vector3 Stage = new Vector3(0f, -400f, 0f);

        private Camera cam;
        private RenderTexture rt;
        private Light keyLight;
        private readonly Queue<PartDefinition> queue = new Queue<PartDefinition>();
        private bool busy;

        /// <summary>Retourne l'icône (immédiatement si connue) et appelle onReady quand elle est prête.</summary>
        public static Sprite Get(PartDefinition def, Action<Sprite> onReady)
        {
            if (def == null) return null;
            if (def.icon != null) return def.icon;
            if (cache.TryGetValue(def, out var s) && s != null) return s;
            if (onReady != null)
            {
                if (!pending.TryGetValue(def, out var list)) pending[def] = list = new List<Action<Sprite>>();
                list.Add(onReady);
            }
            Ensure().Enqueue(def);
            return null;
        }

        private static PartIconRenderer Ensure()
        {
            if (instance != null) return instance;
            var go = new GameObject("[PD] Icon Renderer");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<PartIconRenderer>();
            instance.Build();
            return instance;
        }

        private void Build()
        {
            rt = new RenderTexture(Size, Size, 16, RenderTextureFormat.ARGB32) { name = "PD_IconRT" };
            var camGo = new GameObject("Icon Camera");
            camGo.transform.SetParent(transform, false);
            cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.orthographic = true;
            cam.orthographicSize = 0.62f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 10f;
            cam.targetTexture = rt;
            cam.transform.position = Stage + new Vector3(0.9f, 0.7f, -2.2f);
            cam.transform.LookAt(Stage);
            var lightGo = new GameObject("Icon Light");
            lightGo.transform.SetParent(transform, false);
            keyLight = lightGo.AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.range = 8f;
            keyLight.intensity = 3f;
            keyLight.color = new Color(1f, 0.92f, 0.82f);
            lightGo.transform.position = Stage + new Vector3(-1.2f, 1.6f, -1.8f);
            keyLight.enabled = false;
        }

        private void Enqueue(PartDefinition def)
        {
            if (!queue.Contains(def)) queue.Enqueue(def);
            if (!busy) StartCoroutine(Process());
        }

        private IEnumerator Process()
        {
            busy = true;
            while (queue.Count > 0)
            {
                var def = queue.Dequeue();
                if (cache.ContainsKey(def)) continue;
                Sprite sprite = null;
                if (def.visualPrefab != null)
                {
                    var anchor = new GameObject("icon anchor").transform;
                    anchor.position = Stage;
                    var preview = PartPreview.Build(def.visualPrefab, anchor, 1f);
                    if (preview != null) preview.transform.Rotate(0f, 25f, 0f, Space.World);
                    keyLight.enabled = true;
                    cam.enabled = true;
                    yield return new WaitForEndOfFrame();
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                    tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                    tex.Apply();
                    RenderTexture.active = prev;
                    cam.enabled = false;
                    keyLight.enabled = false;
                    Destroy(anchor.gameObject);
                    sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
                    sprite.name = def.displayName + " (icône)";
                }
                cache[def] = sprite;
                if (pending.TryGetValue(def, out var list))
                {
                    pending.Remove(def);
                    foreach (var cb in list) cb?.Invoke(sprite);
                }
                yield return null;
            }
            busy = false;
        }
    }
}
