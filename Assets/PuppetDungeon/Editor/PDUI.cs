using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static PuppetDungeon.EditorTools.UIBuilder;

namespace PuppetDungeon.EditorTools
{
    /// <summary>Construit tous les prefabs d'interface (panneaux bois en 9-slice).</summary>
    public static class PDUI
    {
        // ------------------------------------------------------------------ Petits prefabs

        public static ItemSlotView BuildSlot(string name, string placeholder, float size)
        {
            var root = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(size, size);
            var frame = Img(rt, PanelClean, Color.white, true, 5.5f);
            var glow = Stretch(rt, "Rareté", 10f);
            var glowImg = Img(glow, White, new Color(1f, 1f, 1f, 0f), false, 1f, false);
            var ph = Stretch(rt, "Emplacement", 12f);
            var phText = Text(ph, placeholder, 17f, new Color(0.79f, 0.72f, 0.61f, 0.65f), TextAlignmentOptions.Center);
            var iconRt = Stretch(rt, "Icône", 12f);
            var icon = Img(iconRt, null, Color.white, false, 1f, false);
            icon.preserveAspect = true;
            icon.enabled = false;
            var lvl = Rect(rt, "Niveau", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-10f, 6f), new Vector2(60f, 28f));
            var lvlText = Text(lvl, "", 18f, Parchment, TextAlignmentOptions.BottomRight, true, true);
            var hl = Stretch(rt, "Surbrillance", 4f);
            var hlImg = Img(hl, PanelClean, new Color(0.5f, 1f, 0.45f, 0.5f), true, 5.5f, false);
            hlImg.enabled = false;

            var slot = root.AddComponent<ItemSlotView>();
            slot.frame = frame;
            slot.rarityGlow = glowImg;
            slot.icon = icon;
            slot.levelText = lvlText;
            slot.placeholder = phText;
            slot.highlight = hlImg;
            slot.targetGraphic = frame;
            slot.transition = Selectable.Transition.None;
            return SaveUI<ItemSlotView>(root, name);
        }

        public static ToastView BuildToast()
        {
            var root = new GameObject("PD_Toast", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(560f, 64f);
            Img(rt, PanelClean, Color.white, true, 4f, false);
            var t = Stretch(rt, "Texte", 18f);
            var label = Text(t, "Notification", 22f, Color.white, TextAlignmentOptions.MidlineLeft);
            root.AddComponent<CanvasGroup>();
            var le = root.AddComponent<LayoutElement>();
            le.preferredHeight = 64f;
            le.preferredWidth = 560f;
            var view = root.AddComponent<ToastView>();
            view.label = label;
            return SaveUI<ToastView>(root, "PD_Toast");
        }

        public static OfferingCardView BuildOfferingCard()
        {
            var root = new GameObject("PD_OfferingCard", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(330f, 140f);
            var bg = Img(rt, PanelClean, Color.white, true, 4f);
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bg;
            var bar = Rect(rt, "Rareté", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(8f, -36f));
            var barImg = Img(bar, White, Color.white, false, 1f, false);
            var iconRt = Rect(rt, "Icône", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 0f), new Vector2(96f, 96f));
            var icon = Img(iconRt, null, Color.white, false, 1f, false);
            var nameRt = Rect(rt, "Nom", new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(140f, 0f), new Vector2(-160f, -24f));
            var nameT = Text(nameRt, "Pièce", 22f, Color.white, TextAlignmentOptions.BottomLeft, true);
            var infoRt = Rect(rt, "Infos", new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(140f, 0f), new Vector2(-160f, -24f));
            var info = Text(infoRt, "", 18f, Muted, TextAlignmentOptions.TopLeft);
            var sel = Stretch(rt, "Sélection", -6f);
            var selImg = Img(sel, PanelClean, Gold, true, 4f, false);
            selImg.enabled = false;
            root.AddComponent<ButtonJuice>();
            var card = root.AddComponent<OfferingCardView>();
            card.button = btn;
            card.icon = icon;
            card.rarityBar = barImg;
            card.selectedFrame = selImg;
            card.nameText = nameT;
            card.infoText = info;
            return SaveUI<OfferingCardView>(root, "PD_OfferingCard");
        }

        // ------------------------------------------------------------------ Racine de l'UI en jeu

        public static GameObject BuildUIRoot(ItemSlotView slotPrefab, ToastView toastPrefab, OfferingCardView cardPrefab)
        {
            var canvas = MakeCanvas("PD_UIRoot", 10);
            var root = (RectTransform)canvas.transform;
            var ui = canvas.gameObject.AddComponent<UIRoot>();
            ui.canvas = canvas;

            ui.hud = BuildHUD(root);
            BuildMessages(root, ui, toastPrefab);
            ui.inventory = BuildInventory(root, slotPrefab);
            var options = BuildOptions(root);
            ui.pause = BuildPause(root, options);
            ui.gameOver = BuildGameOver(root);
            ui.offering = BuildOffering(root, cardPrefab);
            ui.dialog = BuildDialog(root);
            ui.gacha = BuildGacha(root, cardPrefab);
            options.transform.SetAsLastSibling();
            ui.tooltip = BuildTooltip(root);
            ui.dragGhost = BuildDragGhost(root);

            PDPaths.EnsureFolder(PDPaths.UI);
            var saved = PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, PDPaths.UI + "/PD_UIRoot.prefab");
            Object.DestroyImmediate(canvas.gameObject);
            return saved;
        }

        private static HUDView BuildHUD(RectTransform root)
        {
            var hudRt = Stretch(root, "HUD");
            var hud = hudRt.gameObject.AddComponent<HUDView>();

            // Cartouche du pantin (haut gauche)
            var card = Rect(hudRt, "Pantin", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(560f, 170f));
            Img(card, PanelClean, Color.white, true, 2.6f, false);
            hud.healthPunch = card;
            var name = Rect(card, "Nom", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(40f, -26f), new Vector2(-80f, 42f));
            hud.puppetName = Text(name, "Brindille", 30f, Parchment, TextAlignmentOptions.MidlineLeft, true, true);

            var bar = Rect(card, "Vie", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(40f, -76f), new Vector2(-150f, 36f));
            Img(bar, White, new Color(0.08f, 0.04f, 0.04f, 0.9f), false, 1f, false);
            var lag = Stretch(bar, "Retard", 3f);
            hud.healthLag = Filled(lag, White, new Color(1f, 0.85f, 0.6f, 0.8f), Image.FillMethod.Horizontal);
            var fill = Stretch(bar, "Remplissage", 3f);
            hud.healthFill = Filled(fill, White, Blood, Image.FillMethod.Horizontal);
            var hpText = Stretch(bar, "Texte");
            hud.healthText = Text(hpText, "60 / 60", 22f, Color.white, TextAlignmentOptions.Center, true, true);

            var dash = Rect(card, "Ruée", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -66f), new Vector2(58f, 58f));
            Img(dash, Circle, new Color(0.08f, 0.04f, 0.04f, 0.85f), false, 1f, false);
            var dashFill = Stretch(dash, "Charge", 5f);
            hud.dashFill = Filled(dashFill, Circle, Gold, Image.FillMethod.Radial360, 2);
            var dashLabel = Stretch(dash, "Libellé");
            Text(dashLabel, "RUÉE", 12f, Ink, TextAlignmentOptions.Center, true);

            var style = Rect(card, "Style", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(40f, 18f), new Vector2(-330f, 34f));
            hud.styleLabel = Text(style, "Poing", 20f, Muted, TextAlignmentOptions.MidlineLeft);
            var money = Rect(card, "Monnaie", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 18f), new Vector2(240f, 34f));
            hud.currencyLabel = Text(money, "", 20f, Parchment, TextAlignmentOptions.MidlineRight);

            // Donjon (haut centre)
            var dl = Rect(hudRt, "Donjon", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(900f, 46f));
            hud.dungeonLabel = Text(dl, "Donjon 1", 32f, Parchment, TextAlignmentOptions.Center, true, true);
            var vl = Rect(hudRt, "Village", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(900f, 34f));
            hud.villageLabel = Text(vl, "", 21f, Muted, TextAlignmentOptions.Center, false, true);

            // Aide des touches (bas droite)
            var keys = Rect(hudRt, "Touches", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 20f), new Vector2(560f, 40f));
            Text(keys, "<color=#F2C14E>Clic/J</color> attaquer   <color=#F2C14E>Espace</color> ruée   <color=#F2C14E>I/Tab</color> pantin   <color=#F2C14E>Échap</color> pause", 18f, Muted, TextAlignmentOptions.BottomRight, false, true);

            // Invite d'interaction (bas centre)
            var prompt = Rect(hudRt, "Invite", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(520f, 70f));
            Img(prompt, ButtonSprite != null ? ButtonSprite : PanelClean, Color.white, true, 7f, false);
            hud.promptGroup = Group(prompt, 0f, false);
            var pt = Stretch(prompt, "Texte", 14f);
            hud.promptText = Text(pt, "[E] Interagir", 24f, Parchment, TextAlignmentOptions.Center, true, true);
            return hud;
        }

        private static void BuildMessages(RectTransform root, UIRoot ui, ToastView toastPrefab)
        {
            // Bannière
            var banner = Rect(root, "Bannière", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(1300f, 90f));
            var bannerBg = Stretch(banner, "Fond");
            Img(bannerBg, GradientSprite, new Color(0f, 0f, 0f, 0.8f), false, 1f, false);
            ui.bannerGroup = Group(banner, 0f, false);
            var bt = Stretch(banner, "Texte");
            ui.bannerText = Text(bt, "", 44f, Parchment, TextAlignmentOptions.Center, true, true);

            // Carte de titre
            var title = Stretch(root, "Titre");
            ui.titleGroup = Group(title, 0f, false);
            var strip = Center(title, "Bande", new Vector2(0f, 60f), new Vector2(2400f, 300f));
            Img(strip, GradientSprite, new Color(0f, 0f, 0f, 0.88f), false, 1f, false);
            ui.titleTop = Label(title, "Haut", "Donjon 1", 34f, Gold, new Vector2(0f, 150f), new Vector2(1400f, 50f), TextAlignmentOptions.Center, true, true);
            ui.titleMain = Label(title, "Nom", "Les Combles Poussiéreux", 76f, Parchment, new Vector2(0f, 75f), new Vector2(1600f, 100f), TextAlignmentOptions.Center, true, true);
            ui.titleSub = Label(title, "Sous-titre", "", 28f, Muted, new Vector2(0f, 0f), new Vector2(1400f, 50f), TextAlignmentOptions.Center, false, true);

            // Barre de boss
            var boss = Rect(root, "Boss", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1000f, 90f));
            ui.bossGroup = Group(boss, 0f, false);
            var bossName = Rect(boss, "Nom", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 40f));
            ui.bossName = Text(bossName, "Le Grand Ficelé", 30f, Parchment, TextAlignmentOptions.Center, true, true);
            var bossBar = Rect(boss, "Barre", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 40f));
            Img(bossBar, PanelClean, Color.white, true, 6f, false);
            var bossBack = Stretch(bossBar, "Fond", 9f);
            Img(bossBack, White, new Color(0.06f, 0.03f, 0.03f, 1f), false, 1f, false);
            var bossLag = Stretch(bossBar, "Retard", 9f);
            ui.bossFillLag = Filled(bossLag, White, new Color(1f, 0.85f, 0.6f), Image.FillMethod.Horizontal);
            var bossFill = Stretch(bossBar, "Remplissage", 9f);
            ui.bossFill = Filled(bossFill, White, new Color(0.6f, 0.12f, 0.35f), Image.FillMethod.Horizontal);

            // Notifications
            var toasts = Rect(root, "Notifications", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 70f), new Vector2(560f, 400f));
            var vl = toasts.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.LowerLeft;
            vl.spacing = 8f;
            vl.childControlHeight = false;
            vl.childControlWidth = false;
            vl.childForceExpandHeight = false;
            vl.childForceExpandWidth = false;
            ui.toastContainer = toasts;
            ui.toastPrefab = toastPrefab;
        }

        private static InventoryView BuildInventory(RectTransform root, ItemSlotView slotPrefab)
        {
            var screen = Stretch(root, "Inventaire");
            Img(screen, White, new Color(0f, 0f, 0f, 0.82f));
            screen.gameObject.AddComponent<CanvasGroup>();
            var panel = Center(screen, "Panneau", new Vector2(0f, -10f), new Vector2(1640f, 900f));
            Img(panel, Panel, Color.white, true, 1.6f);
            var inv = screen.gameObject.AddComponent<InventoryView>();
            inv.animatedRoot = panel;

            inv.titleText = Label(panel, "Titre", "Pantin", 44f, Parchment, new Vector2(0f, 385f), new Vector2(1200f, 60f), TextAlignmentOptions.Center, true, true);

            // Zone du pantin
            var dollBg = Center(panel, "Fond du pantin", new Vector2(-440f, -10f), new Vector2(620f, 700f));
            Img(dollBg, PanelClean, Color.white, true, 2.4f);
            var preview = Center(panel, "Aperçu", new Vector2(-440f, -10f), new Vector2(300f, 420f));
            var raw = preview.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            inv.puppetPreview = raw;

            Vector2[] pos =
            {
                new Vector2(-440f, 270f),   // Tête
                new Vector2(-440f, -290f),  // Torse
                new Vector2(-235f, 110f),   // Bras gauche (à droite de l'écran : le pantin nous fait face)
                new Vector2(-645f, 110f),   // Bras droit
                new Vector2(-235f, -110f),  // Jambe gauche
                new Vector2(-645f, -110f)   // Jambe droite
            };
            string[] labels = { "Tête", "Torse", "Bras\ngauche", "Bras\ndroit", "Jambe\ngauche", "Jambe\ndroite" };
            for (int i = 0; i < 6; i++)
            {
                var s = Object.Instantiate(slotPrefab, panel);
                s.name = "Emplacement " + (PuppetSlot)i;
                var rt = (RectTransform)s.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos[i];
                rt.sizeDelta = new Vector2(120f, 120f);
                if (s.placeholder != null) s.placeholder.text = labels[i];
                s.kind = ItemSlotView.Kind.Equip;
                s.equipSlot = (PuppetSlot)i;
                inv.equipSlots[i] = s;
            }

            // Stats
            var statsBg = Center(panel, "Stats", new Vector2(40f, -10f), new Vector2(330f, 700f));
            Img(statsBg, PanelClean, Color.white, true, 3f);
            Label(statsBg, "Titre", "Statistiques", 28f, Gold, new Vector2(0f, 310f), new Vector2(300f, 40f), TextAlignmentOptions.Center, true);
            var st = Rect(statsBg, "Texte", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(-50f, -100f));
            st.anchoredPosition = new Vector2(0f, -70f);
            inv.statsText = Text(st, "", 20f, Parchment, TextAlignmentOptions.TopLeft);

            // Sac
            inv.bagCountText = Label(panel, "Sac", "Sac", 28f, Gold, new Vector2(495f, 315f), new Vector2(540f, 40f), TextAlignmentOptions.Center, true);
            var grid = Center(panel, "Grille", new Vector2(495f, 20f), new Vector2(560f, 560f));
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(102f, 102f);
            gl.spacing = new Vector2(8f, 8f);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 5;
            gl.childAlignment = TextAnchor.UpperCenter;
            inv.bagGrid = grid;
            inv.slotPrefab = slotPrefab;

            // Corbeille
            var trash = Object.Instantiate(slotPrefab, panel);
            trash.name = "Corbeille";
            var trt = (RectTransform)trash.transform;
            trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(710f, -315f);
            trt.sizeDelta = new Vector2(96f, 96f);
            if (trash.placeholder != null) trash.placeholder.text = "Copeaux\n(détruire)";
            trash.kind = ItemSlotView.Kind.Trash;
            inv.trashSlot = trash;

            var hint = Rect(panel, "Aide", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-80f, 34f), new Vector2(1300f, 36f));
            inv.hintText = Text(hint, "", 19f, Muted, TextAlignmentOptions.Center);
            return inv;
        }

        public static OptionsView BuildOptions(RectTransform root)
        {
            var screen = Stretch(root, "Options");
            screen.gameObject.AddComponent<CanvasGroup>();
            var panel = Center(screen, "Panneau", Vector2.zero, new Vector2(760f, 640f));
            Img(panel, Panel, Color.white, true, 1.8f);
            var view = screen.gameObject.AddComponent<OptionsView>();
            view.animatedRoot = panel;
            Label(panel, "Titre", "Options", 44f, Parchment, new Vector2(0f, 245f), new Vector2(600f, 60f), TextAlignmentOptions.Center, true, true);
            view.master = Slider(panel, "Général", "Volume général", new Vector2(0f, 160f));
            view.music = Slider(panel, "Musique", "Musique", new Vector2(0f, 95f));
            view.sfx = Slider(panel, "Effets", "Effets sonores", new Vector2(0f, 30f));
            view.shake = Slider(panel, "Secousses", "Secousses d'écran", new Vector2(0f, -35f));
            view.hitStop = Toggle(panel, "Arrêt", "Arrêt sur image à l'impact", new Vector2(0f, -100f));
            view.tiltShift = Toggle(panel, "Maquette", "Effet maquette (tilt-shift)", new Vector2(0f, -160f));
            view.closeButton = Btn(panel, "Fermer", "Fermer", new Vector2(0f, -245f), new Vector2(300f, 80f));
            view.firstSelected = view.master.gameObject;
            return view;
        }

        private static PauseMenuView BuildPause(RectTransform root, OptionsView options)
        {
            var screen = Stretch(root, "Pause");
            Img(screen, White, new Color(0f, 0f, 0f, 0.8f));
            screen.gameObject.AddComponent<CanvasGroup>();
            var panel = Center(screen, "Panneau", Vector2.zero, new Vector2(620f, 560f));
            Img(panel, Panel, Color.white, true, 1.8f);
            var view = screen.gameObject.AddComponent<PauseMenuView>();
            view.animatedRoot = panel;
            Label(panel, "Titre", "Pause", 52f, Parchment, new Vector2(0f, 190f), new Vector2(500f, 70f), TextAlignmentOptions.Center, true, true);
            view.resumeButton = Btn(panel, "Reprendre", "Reprendre", new Vector2(0f, 70f), new Vector2(380f, 90f));
            view.optionsButton = Btn(panel, "Options", "Options", new Vector2(0f, -40f), new Vector2(380f, 90f));
            view.menuButton = Btn(panel, "Menu", "Menu principal", new Vector2(0f, -150f), new Vector2(380f, 90f));
            view.options = options;
            view.firstSelected = view.resumeButton.gameObject;
            return view;
        }

        private static GameOverView BuildGameOver(RectTransform root)
        {
            var screen = Stretch(root, "Fin");
            Img(screen, White, new Color(0.08f, 0f, 0f, 0.75f));
            screen.gameObject.AddComponent<CanvasGroup>();
            var panel = Center(screen, "Panneau", Vector2.zero, new Vector2(820f, 720f));
            Img(panel, Panel, Color.white, true, 1.8f);
            var view = screen.gameObject.AddComponent<GameOverView>();
            view.animatedRoot = panel;
            view.title = Label(panel, "Titre", "Vos fils sont coupés", 54f, new Color(1f, 0.55f, 0.45f), new Vector2(0f, 270f), new Vector2(740f, 80f), TextAlignmentOptions.Center, true, true);
            var sum = Center(panel, "Bilan", new Vector2(0f, 20f), new Vector2(600f, 380f));
            view.summary = Text(sum, "", 26f, Parchment, TextAlignmentOptions.TopLeft);
            view.retryButton = Btn(panel, "Rejouer", "Nouveau pantin", new Vector2(-170f, -265f), new Vector2(320f, 90f), 26f);
            view.menuButton = Btn(panel, "Menu", "Menu principal", new Vector2(170f, -265f), new Vector2(320f, 90f), 26f);
            view.firstSelected = view.retryButton.gameObject;
            return view;
        }

        private static OfferingView BuildOffering(RectTransform root, OfferingCardView cardPrefab)
        {
            var screen = Stretch(root, "Offrande");
            Img(screen, White, new Color(0f, 0f, 0f, 0.8f));
            screen.gameObject.AddComponent<CanvasGroup>();
            var panel = Center(screen, "Panneau", new Vector2(0f, 20f), new Vector2(1500f, 860f));
            Img(panel, Panel, Color.white, true, 1.6f);
            var view = screen.gameObject.AddComponent<OfferingView>();
            view.animatedRoot = panel;
            view.header = Label(panel, "Titre", "L'Offrande", 50f, Gold, new Vector2(0f, 360f), new Vector2(1200f, 70f), TextAlignmentOptions.Center, true, true);
            view.subHeader = Label(panel, "Sous-titre", "", 26f, Parchment, new Vector2(0f, 300f), new Vector2(1250f, 60f), TextAlignmentOptions.Center);

            view.recycleText = Label(panel, "Recyclage", "", 22f, Gold, new Vector2(0f, 250f), new Vector2(1250f, 40f), TextAlignmentOptions.Center);
            view.grid = CardGrid(panel, new Vector2(0f, -30f), new Vector2(1380f, 500f));
            view.cardPrefab = cardPrefab;

            view.counter = Label(panel, "Compteur", "0 / 1", 28f, Muted, new Vector2(-420f, -340f), new Vector2(300f, 50f), TextAlignmentOptions.Center, true);
            view.confirmButton = Btn(panel, "Offrir", "Offrir", new Vector2(250f, -340f), new Vector2(380f, 90f), 32f);
            return view;
        }

        /// <summary>Grille de cartes défilante (offrande, gacha).</summary>
        private static RectTransform CardGrid(RectTransform panel, Vector2 pos, Vector2 size)
        {
            var scroll = Center(panel, "Défilement", pos, size);
            var sr = scroll.gameObject.AddComponent<ScrollRect>();
            var viewport = Stretch(scroll, "Fenêtre");
            viewport.gameObject.AddComponent<RectMask2D>();
            Img(viewport, White, new Color(0f, 0f, 0f, 0.001f));
            var content = Rect(viewport, "Contenu", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, size.y));
            var gl = content.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(330f, 140f);
            gl.spacing = new Vector2(16f, 16f);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 4;
            gl.childAlignment = TextAnchor.UpperCenter;
            gl.padding = new RectOffset(8, 8, 8, 8);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = viewport;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 30f;
            return content;
        }

        private static GachaView BuildGacha(RectTransform root, OfferingCardView cardPrefab)
        {
            var screen = Stretch(root, "Gacha");
            Img(screen, White, new Color(0f, 0f, 0f, 0.82f));
            screen.gameObject.AddComponent<CanvasGroup>();
            var panel = Center(screen, "Panneau", new Vector2(0f, 10f), new Vector2(1500f, 900f));
            Img(panel, Panel, Color.white, true, 1.6f);
            var view = screen.gameObject.AddComponent<GachaView>();
            view.animatedRoot = panel;
            view.title = Label(panel, "Titre", "La Tisseuse de Fils", 48f, Gold, new Vector2(0f, 385f), new Vector2(1200f, 66f), TextAlignmentOptions.Center, true, true);
            view.flavor = Label(panel, "Accroche", "Recyclez vos vieilles pièces en bobines, la Tisseuse vous en tisse de nouvelles… au hasard.", 23f, Parchment, new Vector2(0f, 330f), new Vector2(1300f, 44f), TextAlignmentOptions.Center);
            view.currencyText = Label(panel, "Monnaie", "0 Bobines", 32f, Parchment, new Vector2(-430f, 272f), new Vector2(460f, 50f), TextAlignmentOptions.MidlineLeft, true, true);
            view.pityText = Label(panel, "Garantie", "", 22f, Muted, new Vector2(300f, 272f), new Vector2(720f, 44f), TextAlignmentOptions.MidlineRight);
            view.oddsText = Label(panel, "Probabilités", "", 21f, Parchment, new Vector2(0f, 222f), new Vector2(1320f, 40f), TextAlignmentOptions.Center);
            view.resultsGrid = CardGrid(panel, new Vector2(0f, -30f), new Vector2(1380f, 460f));
            view.cardPrefab = cardPrefab;
            view.resultHint = Label(panel, "Aide", "", 20f, Muted, new Vector2(0f, -285f), new Vector2(1300f, 36f), TextAlignmentOptions.Center);
            view.pullOneButton = Btn(panel, "Tirer1", "Tirer x1", new Vector2(-430f, -360f), new Vector2(380f, 90f), 30f);
            view.pullMultiButton = Btn(panel, "TirerX", "Tirer x5", new Vector2(0f, -360f), new Vector2(380f, 90f), 30f);
            view.closeButton = Btn(panel, "Fermer", "Fermer", new Vector2(430f, -360f), new Vector2(300f, 90f), 30f);
            view.pullOneLabel = view.pullOneButton.GetComponentInChildren<TextMeshProUGUI>();
            view.pullMultiLabel = view.pullMultiButton.GetComponentInChildren<TextMeshProUGUI>();
            view.firstSelected = view.pullOneButton.gameObject;
            return view;
        }

        private static CreditsView BuildCredits(RectTransform root)
        {
            var screen = Stretch(root, "Crédits");
            Img(screen, White, new Color(0f, 0f, 0f, 0.92f));
            screen.gameObject.AddComponent<CanvasGroup>();
            var panel = Center(screen, "Panneau", Vector2.zero, new Vector2(1100f, 940f));
            Img(panel, Panel, Color.white, true, 1.6f);
            var view = screen.gameObject.AddComponent<CreditsView>();
            view.animatedRoot = panel;
            var viewport = Center(panel, "Fenêtre", new Vector2(0f, 40f), new Vector2(960f, 700f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect(viewport, "Contenu", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 2000f));
            var body = Stretch(content, "Texte");
            view.body = FlowText(Text(body, "", 30f, Parchment, TextAlignmentOptions.Top));
            view.viewport = viewport;
            view.content = content;
            view.titleColor = Gold;
            view.headingColor = Gold;
            Label(panel, "Aide", "Espace / clic : accélérer   •   Échap : retour", 19f, Muted, new Vector2(0f, -340f), new Vector2(900f, 34f), TextAlignmentOptions.Center);
            view.closeButton = Btn(panel, "Retour", "Retour", new Vector2(0f, -405f), new Vector2(300f, 80f));
            view.firstSelected = view.closeButton.gameObject;
            return view;
        }

        private static DialogView BuildDialog(RectTransform root)
        {
            var screen = Stretch(root, "Dialogue");
            screen.gameObject.AddComponent<CanvasGroup>();
            var panel = Rect(screen, "Panneau", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1300f, 250f));
            Img(panel, Panel, Color.white, true, 2.2f);
            var view = screen.gameObject.AddComponent<DialogView>();
            view.animatedRoot = panel;
            var tag = Rect(panel, "Nom", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(340f, 64f));
            Img(tag, ButtonSprite != null ? ButtonSprite : PanelClean, Color.white, true, 7f, false);
            var tagText = Stretch(tag, "Texte", 10f);
            view.speaker = Text(tagText, "Marionnette", 26f, Gold, TextAlignmentOptions.Center, true, true);
            var body = Rect(panel, "Texte", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(-140f, -100f));
            view.text = Text(body, "", 30f, Parchment, TextAlignmentOptions.TopLeft);
            var hint = Rect(panel, "Suite", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-50f, 30f), new Vector2(300f, 40f));
            var hintT = Text(hint, "Continuer ▼", 20f, Muted, TextAlignmentOptions.BottomRight);
            view.continueHint = hintT.gameObject;
            return view;
        }

        private static TooltipView BuildTooltip(RectTransform root)
        {
            var holder = Stretch(root, "Info-bulle");
            var tip = holder.gameObject.AddComponent<TooltipView>();
            tip.group = holder.gameObject.AddComponent<CanvasGroup>();
            tip.group.blocksRaycasts = false;
            tip.group.interactable = false;
            var panel = Rect(holder, "Panneau", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), Vector2.zero, new Vector2(460f, 200f));
            Img(panel, PanelClean, Color.white, true, 3f, false);
            var vl = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(34, 34, 30, 30);
            vl.spacing = 6f;
            vl.childControlHeight = true;
            vl.childControlWidth = true;
            vl.childForceExpandHeight = false;
            vl.childForceExpandWidth = true;
            var fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            tip.panel = panel;

            var bar = Rect(panel, "Barre", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 6f));
            tip.rarityBar = Img(bar, White, Color.white, false, 1f, false);
            bar.gameObject.AddComponent<LayoutElement>().preferredHeight = 6f;
            tip.title = FlowText(Text(Rect(panel, "Nom", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), "Pièce", 28f, Color.white, TextAlignmentOptions.TopLeft, true));
            tip.rarity = FlowText(Text(Rect(panel, "Rareté", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), "COMMUN", 16f, Muted, TextAlignmentOptions.TopLeft, true));
            tip.body = FlowText(Text(Rect(panel, "Corps", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), "", 20f, Parchment, TextAlignmentOptions.TopLeft));
            tip.compare = FlowText(Text(Rect(panel, "Comparaison", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), "", 18f, Parchment, TextAlignmentOptions.TopLeft));
            return tip;
        }

        private static DragGhost BuildDragGhost(RectTransform root)
        {
            var ghost = Center(root, "Glisser", Vector2.zero, new Vector2(96f, 96f));
            var glow = Stretch(ghost, "Halo", -18f);
            var glowImg = Img(glow, PDAssets.Circle, new Color(1f, 1f, 1f, 0.5f), false, 1f, false);
            var icon = Stretch(ghost, "Icône");
            var iconImg = Img(icon, null, Color.white, false, 1f, false);
            var cg = ghost.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            var dg = ghost.gameObject.AddComponent<DragGhost>();
            dg.icon = iconImg;
            dg.glow = glowImg;
            return dg;
        }

        // ------------------------------------------------------------------ Menu principal

        public static GameObject BuildMainMenu()
        {
            var canvas = MakeCanvas("PD_MainMenu", 5);
            var root = (RectTransform)canvas.transform;
            var view = canvas.gameObject.AddComponent<MainMenuView>();

            var vignette = Stretch(root, "Voile");
            Img(vignette, GradientSprite, new Color(0f, 0f, 0f, 0.35f), false, 1f, false);

            var titleRoot = Center(root, "Titre", new Vector2(0f, 250f), new Vector2(1300f, 260f));
            Img(titleRoot, Panel, Color.white, true, 1.8f, false);
            view.titleRoot = titleRoot;
            view.title = Label(titleRoot, "Nom", GameInfo.Title.ToUpperInvariant(), 84f, Parchment, new Vector2(0f, 25f), new Vector2(1200f, 110f), TextAlignmentOptions.Center, true, true);
            Label(titleRoot, "Sous-titre", "Un donjon de bois, de ficelles et de pièces détachées", 28f, Gold, new Vector2(0f, -55f), new Vector2(1200f, 50f), TextAlignmentOptions.Center, false, true);

            view.playButton = Btn(root, "Jouer", "Nouvelle partie", new Vector2(0f, -20f), new Vector2(460f, 92f), 34f);
            view.optionsButton = Btn(root, "Options", "Options", new Vector2(0f, -125f), new Vector2(460f, 92f), 34f);
            view.creditsButton = Btn(root, "Crédits", "Crédits", new Vector2(0f, -230f), new Vector2(460f, 92f), 34f);
            view.quitButton = Btn(root, "Quitter", "Quitter", new Vector2(0f, -335f), new Vector2(460f, 92f), 34f);
            view.options = BuildOptions(root);
            view.credits = BuildCredits(root);

            var credit = Rect(root, "Crédit", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1400f, 40f));
            Text(credit, "Volcano Game Jam 3", 20f, Muted, TextAlignmentOptions.Center, false, true);

            PDPaths.EnsureFolder(PDPaths.UI);
            var saved = PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, PDPaths.UI + "/PD_MainMenu.prefab");
            Object.DestroyImmediate(canvas.gameObject);
            return saved;
        }
    }
}
