using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PuppetDungeon.EditorTools
{
    /// <summary>Petite bibliothèque pour construire des hiérarchies uGUI par script (9-slicing compris).</summary>
    public static class UIBuilder
    {
        public static Sprite Panel, PanelClean, ButtonSprite, White, Circle, GradientSprite;
        public static TMP_FontAsset Font;
        public static Material OutlineMaterial;

        public static readonly Color Ink = new Color(0.12f, 0.08f, 0.06f);
        public static readonly Color Parchment = new Color(0.96f, 0.9f, 0.78f);
        public static readonly Color Muted = new Color(0.79f, 0.72f, 0.61f);
        public static readonly Color Gold = new Color(0.95f, 0.76f, 0.31f);
        public static readonly Color Blood = new Color(0.78f, 0.18f, 0.14f);

        public static void LoadTheme()
        {
            Panel = PDPaths.Find<Sprite>(PDPaths.UIPanel);
            PanelClean = PDPaths.Find<Sprite>(PDPaths.UIPanelClean);
            ButtonSprite = PDPaths.Find<Sprite>(PDPaths.UIButton);
            White = PDAssets.White;
            Circle = PDAssets.Circle;
            GradientSprite = PDAssets.Gradient;
            // LiberationSans SDF : accents français via sa police de secours dynamique.
            Font = PDPaths.Find<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (Font == null) Font = TMP_Settings.defaultFontAsset;
            OutlineMaterial = CreateOutlineMaterial(Font);
        }

        /// <summary>Preset de matériau TMP (contour + ombre portée) : un asset partagé, pas d'instance perdue.</summary>
        public static Material CreateOutlineMaterial(TMP_FontAsset font)
        {
            if (font == null || font.material == null) return null;
            string path = PDPaths.Materials + "/PD_TextOutline.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            PDPaths.EnsureFolder(PDPaths.Materials);
            var m = new Material(font.material) { name = font.name + " Outline" };
            m.SetFloat("_OutlineWidth", 0.22f);
            m.SetColor("_OutlineColor", new Color(0.08f, 0.05f, 0.03f, 1f));
            m.EnableKeyword("OUTLINE_ON");
            m.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.6f));
            m.SetFloat("_UnderlayOffsetX", 0.6f);
            m.SetFloat("_UnderlayOffsetY", -0.6f);
            m.SetFloat("_UnderlaySoftness", 0.3f);
            m.EnableKeyword("UNDERLAY_ON");
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.layer = LayerMask.NameToLayer("UI");
            return rt;
        }

        public static RectTransform Center(Transform parent, string name, Vector2 pos, Vector2 size)
            => Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

        public static RectTransform Stretch(Transform parent, string name, float inset = 0f)
        {
            var rt = Rect(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        /// <summary>Image ; sliced = 9-slice. ppu : multiplicateur pour affiner/épaissir les bordures.</summary>
        public static Image Img(RectTransform rt, Sprite sprite, Color color, bool sliced = false, float ppu = 1f, bool raycast = true)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sliced && sprite != null)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = ppu;
                img.fillCenter = true;
            }
            return img;
        }

        public static Image Filled(RectTransform rt, Sprite sprite, Color color, Image.FillMethod method, int origin = 0)
        {
            var img = Img(rt, sprite != null ? sprite : White, color, false, 1f, false);
            img.type = Image.Type.Filled;
            img.fillMethod = method;
            img.fillOrigin = origin;
            img.fillAmount = 1f;
            return img;
        }

        public static TextMeshProUGUI Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false, bool outline = false)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.richText = true;
            t.textWrappingMode = TextWrappingModes.Normal;
            if (bold) t.fontStyle = FontStyles.Bold;
            if (outline && OutlineMaterial != null) t.fontSharedMaterial = OutlineMaterial;
            return t;
        }

        public static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color, Vector2 pos, Vector2 boxSize, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false, bool outline = false)
        {
            var rt = Center(parent, name, pos, boxSize);
            return Text(rt, text, size, color, align, bold, outline);
        }

        public static Button Btn(Transform parent, string name, string label, Vector2 pos, Vector2 size, float fontSize = 30f)
        {
            var rt = Center(parent, name, pos, size);
            var img = Img(rt, ButtonSprite != null ? ButtonSprite : PanelClean, Color.white, true, 6f);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.78f);
            colors.pressedColor = new Color(0.8f, 0.72f, 0.6f);
            colors.selectedColor = new Color(1f, 0.92f, 0.78f);
            colors.disabledColor = new Color(0.55f, 0.5f, 0.45f, 0.7f);
            b.colors = colors;
            var t = Stretch(rt, "Texte", 10f);
            Text(t, label, fontSize, Parchment, TextAlignmentOptions.Center, true, true);
            rt.gameObject.AddComponent<ButtonJuice>();
            return b;
        }

        public static CanvasGroup Group(RectTransform rt, float alpha = 1f, bool interactable = true)
        {
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = alpha;
            g.interactable = interactable;
            g.blocksRaycasts = interactable;
            return g;
        }

        public static Slider Slider(Transform parent, string name, string label, Vector2 pos, float width = 520f)
        {
            var row = Center(parent, name, pos, new Vector2(width, 56f));
            var l = Rect(row, "Libellé", new Vector2(0f, 0f), new Vector2(0.42f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            Text(l, label, 26f, Parchment, TextAlignmentOptions.MidlineLeft);
            var sRt = Rect(row, "Curseur", new Vector2(0.45f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 26f));
            var bg = Stretch(sRt, "Fond");
            Img(bg, White, new Color(0.08f, 0.05f, 0.04f, 0.85f));
            var fillArea = Stretch(sRt, "Zone de remplissage", 3f);
            var fill = Stretch(fillArea, "Remplissage");
            var fillImg = Img(fill, White, Gold);
            var handleArea = Stretch(sRt, "Zone de poignée");
            handleArea.offsetMin = new Vector2(12f, 0f);
            handleArea.offsetMax = new Vector2(-12f, 0f);
            var handle = Rect(handleArea, "Poignée", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 40f));
            var hImg = Img(handle, PanelClean != null ? PanelClean : White, Color.white, true, 12f);
            var s = sRt.gameObject.AddComponent<Slider>();
            s.fillRect = fill;
            s.handleRect = handle;
            s.targetGraphic = hImg;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = 0.8f;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            fill.anchorMax = new Vector2(0.8f, 1f);
            return s;
        }

        public static Toggle Toggle(Transform parent, string name, string label, Vector2 pos, float width = 520f)
        {
            var row = Center(parent, name, pos, new Vector2(width, 56f));
            var l = Rect(row, "Libellé", new Vector2(0f, 0f), new Vector2(0.8f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            Text(l, label, 26f, Parchment, TextAlignmentOptions.MidlineLeft);
            var box = Rect(row, "Case", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
            var bg = Img(box, PanelClean != null ? PanelClean : White, Color.white, true, 10f);
            var check = Stretch(box, "Coche", 12f);
            var checkImg = Img(check, White, Gold, false, 1f, false);
            var t = row.gameObject.AddComponent<Toggle>();
            t.targetGraphic = bg;
            t.graphic = checkImg;
            t.isOn = true;
            return t;
        }

        public static T SaveUI<T>(GameObject go, string name) where T : Component
        {
            PDPaths.EnsureFolder(PDPaths.UI);
            var saved = PrefabUtility.SaveAsPrefabAsset(go, PDPaths.UI + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return saved != null ? saved.GetComponent<T>() : null;
        }

        public static Canvas MakeCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            c.pixelPerfect = false;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return c;
        }
    }
}
