using System.IO;
using KoreanGame.Gameplay;
using KoreanGame.Home;
using KoreanGame.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.EditorTools
{
    /// <summary>
    /// Dựng scene Bento Journey theo style guide (Cute Casual 2D: viền nâu, bo tròn, bóng cứng) mà KHÔNG đụng scene Main:
    /// 1. Widget dùng chung (prefab): Button_Primary, Button_Secondary, Button_Icon, Panel.
    /// 2. SceneBuilder.BuildContent dựng màn chơi (giữ nguyên script / luật), rồi bố cục lại HUD theo ảnh tham khảo:
    ///    thanh trên + "Mẫu" neo đỉnh vùng an toàn, nút + khay thẻ neo đáy, thớt co vừa khoảng giữa (DesignFrameFitter).
    /// 3. Main Menu, Cửa hàng, popup Cài đặt / Thắng dùng cùng widget.
    /// Dựng lại sẽ ghi đè scene và các prefab (đường dẫn giữ nguyên nên GUID giữ nguyên). Toạ độ: đơn vị thiết kế 390×844, gốc trên-trái.
    /// </summary>
    public static class BentoJourneyBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/BentoJourney.unity";
        public const string HudPrefabPath = "Assets/_Project/Prefabs/UI/Screens/Game/Canvas_HUD.prefab";
        public const string PopupPrefabPath = "Assets/_Project/Prefabs/UI/Popups/Canvas_Popup.prefab";
        public const string MenuPrefabPath = "Assets/_Project/Prefabs/UI/Screens/Home/Canvas_Menu.prefab";
        const string WidgetDir = "Assets/_Project/Prefabs/UI/Widgets/";
        const string Art = "Assets/_Project/Source/Art/";

        const float TopH = 152, BottomH = 208;   // thanh trên + Mẫu / hàng nút + khay thẻ
        const float Ppum = 3;                     // sprite UI vẽ ở 3 px / đơn vị thiết kế
        // khung thớt (SceneBuilder: BX 29, BY 172, BW 332, BH 464, 1 ô = 44) theo toạ độ thế giới
        static readonly Vector2 BoardCenter = new Vector2(0, (844 / 2f - (172 + 464 / 2f)) / 44f);
        static readonly Vector2 BoardSize = new Vector2(332 / 44f, 464 / 44f);

        static GameObject primaryPrefab, secondaryPrefab, iconPrefab, panelPrefab;
        static Font heading, body;

        [MenuItem("Tools/KoreanGame/Bento Journey/Build Scene")]
        public static void Build() => Build(true);

        /// <summary>Dựng lại không hỏi (gọi từ tool tự động). Có scene đang sửa dở thì dừng, không lưu hộ.</summary>
        public static void BuildNoPrompt() => Build(false);

        static void Build(bool confirm)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[BentoJourneyBuilder] Đang Play: dừng Play rồi dựng lại.");
                return;
            }
            bool exists = File.Exists(ScenePath) || File.Exists(HudPrefabPath) || File.Exists(PopupPrefabPath) || File.Exists(MenuPrefabPath);
            if (confirm && exists && !EditorUtility.DisplayDialog("Bento Journey",
                    "Scene / prefab Bento Journey đã có. Dựng lại sẽ ghi đè chúng (chỉnh tay trong các prefab sẽ mất).\nScene Main không bị đụng tới.",
                    "Dựng lại", "Huỷ"))
                return;
            if (confirm ? !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() : AnySceneDirty()) return;

            heading = LoadFont("Baloo2-ExtraBold.ttf");
            body = LoadFont("Nunito-ExtraBold.ttf");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildWidgets();
            var built = SceneBuilder.BuildContent();

            StyleBoard(built);
            SceneBuilder.Set(Object.FindObjectOfType<TrayView>(true), "catalog", LoadOrCreateCatalog());
            BuildGameHud(built, out var topFrame, out var bottomFrame, out var hudSettings, out var chat);
            StyleWinPopup(built);
            var settings = BuildSettings(built.popup, out var settingsFrame);
            var dishPopup = BuildDishPopup(built.popup, out var dishFrame);
            SceneBuilder.Set(built.boot, "dishPopup", dishPopup);
            var menu = BuildMainMenu(built.ui, out var menuFrame);

            SceneBuilder.Set(built.boot, "mainMenu", menu);
            SceneBuilder.Set(built.boot, "cosmetics", menu.GetComponent<CosmeticsApplier>());
            SceneBuilder.Set(built.boot, "settingsPopup", settings);
            SceneBuilder.Set(built.boot, "settingsButton", hudSettings);
            SceneBuilder.Set(built.boot, "customerChat", chat);

            // safe area + mọi tỉ lệ màn hình dọc
            var fitter = built.cam.gameObject.AddComponent<DesignFrameFitter>();
            SceneBuilder.Set(fitter, "cam", built.cam);
            SceneBuilder.Set(fitter, "frames", new[]
            {
                (RectTransform)built.popup.transform.Find("WinPopup/Frame"), settingsFrame, dishFrame, menuFrame,
            });
            SceneBuilder.Set(fitter, "topFrame", topFrame);
            SceneBuilder.Set(fitter, "bottomFrame", bottomFrame);
            var so = new SerializedObject(fitter);
            so.FindProperty("topHeight").floatValue = TopH;
            so.FindProperty("bottomHeight").floatValue = BottomH;
            so.FindProperty("boardCenter").vector2Value = BoardCenter;
            so.FindProperty("boardSize").vector2Value = BoardSize;
            so.ApplyModifiedPropertiesWithoutUndo();

            foreach (var c in new[] { built.hud.gameObject, built.popup.gameObject, menu.gameObject }) ApplyFonts(c.transform);

            SavePrefab(built.hud.gameObject, HudPrefabPath);
            SavePrefab(built.popup.gameObject, PopupPrefabPath);
            SavePrefab(menu.gameObject, MenuPrefabPath);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[BentoJourneyBuilder] Đã dựng {ScenePath} + prefab HUD, Popup, Menu, Widgets (scene Main giữ nguyên)");
        }

        static bool AnySceneDirty()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                {
                    Debug.LogWarning("[BentoJourneyBuilder] Có scene chưa lưu: lưu hoặc bỏ thay đổi rồi dựng lại.");
                    return true;
                }
            return false;
        }

        // ================= Widget dùng chung =================

        static void BuildWidgets()
        {
            primaryPrefab = SaveWidget(MakeButton("Button_Primary", "ui_btn_primary", 260, 72, 24), "Button_Primary");
            secondaryPrefab = SaveWidget(MakeButton("Button_Secondary", "ui_btn_secondary", 200, 60, 20), "Button_Secondary");
            iconPrefab = SaveWidget(MakeIconButton(), "Button_Icon");
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            TopLeft((RectTransform)panel.transform, 0, 0, 300, 200);
            Sliced(panel.GetComponent<Image>(), Ui("ui_panel"), Color.white).raycastTarget = false;
            panelPrefab = SaveWidget(panel, "Panel");
        }

        /// <summary>Nút có bóng cứng: sprite thường / lún (SpriteSwap) + ButtonPress dời nội dung xuống 4 đv.</summary>
        static GameObject MakeButton(string name, string sprite, float w, float h, int fontSize)
        {
            var go = NewButtonRoot(name, sprite, w, h, out var content);
            var label = NewText(content, "Label", "", fontSize, Palette.UiText, heading);
            Stretch((RectTransform)label.transform, 0, 0, 0, 0);
            return go;
        }

        static GameObject MakeIconButton()
        {
            var go = NewButtonRoot("Button_Icon", "ui_btn_secondary", 56, 60, out var content);
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(content, false);
            Stretch((RectTransform)icon.transform, 10, 10, 10, 10);
            var img = icon.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            // chú thích nhỏ dưới nút (để trống nếu không cần)
            var cap = NewText(go.transform, "Caption", "", 13, Palette.UiBrown, body);
            var crt = (RectTransform)cap.transform;
            crt.anchorMin = new Vector2(0, 0);
            crt.anchorMax = new Vector2(1, 0);
            crt.pivot = new Vector2(.5f, 1);
            crt.anchoredPosition = new Vector2(0, -2);
            crt.sizeDelta = new Vector2(44, 18);
            return go;
        }

        static GameObject NewButtonRoot(string name, string sprite, float w, float h, out Transform content)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(ButtonPress));
            TopLeft((RectTransform)go.transform, 0, 0, w, h);
            var img = Sliced(go.GetComponent<Image>(), Ui(sprite), Color.white);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.spriteState = new SpriteState { pressedSprite = Ui(sprite + "_pressed") };
            var c = new GameObject("Content", typeof(RectTransform));
            c.transform.SetParent(go.transform, false);
            Stretch((RectTransform)c.transform, 0, 5, 0, 0);   // trừ phần bóng 5 đv ở đáy
            content = c.transform;
            SceneBuilder.Set(go.GetComponent<ButtonPress>(), "selectable", btn);
            SceneBuilder.Set(go.GetComponent<ButtonPress>(), "content", (RectTransform)content);
            return go;
        }

        static GameObject SaveWidget(GameObject go, string name)
        {
            EnsureFolder(WidgetDir.TrimEnd('/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, WidgetDir + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject Inst(GameObject prefab, Transform parent, string name, float x, float y, float w, float h)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            TopLeft((RectTransform)go.transform, x, y, w, h);
            return go;
        }

        static Button TextButton(GameObject prefab, Transform parent, string name, string text, float x, float y, float w, float h, int size, out Text label)
        {
            var go = Inst(prefab, parent, name, x, y, w, h);
            label = go.transform.Find("Content/Label").GetComponent<Text>();
            label.text = text;
            label.fontSize = size;
            return go.GetComponent<Button>();
        }

        static Button IconButton(Transform parent, string name, string icon, float x, float y, string caption, float w = 56, float h = 60)
        {
            var go = Inst(iconPrefab, parent, name, x, y, w, h);
            go.transform.Find("Content/Icon").GetComponent<Image>().sprite = Ui(icon);
            go.transform.Find("Caption").GetComponent<Text>().text = caption ?? "";
            return go.GetComponent<Button>();
        }

        // ================= Màn chơi =================

        static void StyleBoard(SceneBuilder.Built b)
        {
            b.cam.backgroundColor = Palette.UiCreamDark;   // nền kem sạch, không trang trí sau thớt
            var bg = GameObject.Find("BoardBg").GetComponent<SpriteRenderer>();
            var size = bg.size;
            bg.sprite = Load("InGame/Gameplay/board_frame");
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = size;
        }

        static void BuildGameHud(SceneBuilder.Built b, out RectTransform top, out RectTransform bottom, out Button hudSettings, out CustomerChat chat)
        {
            var canvas = b.hud.transform;
            var old = Req(canvas, "Frame");
            var hudView = b.hud.GetComponent<HudView>();
            foreach (var n in new[] { "TopBg", "Mascot", "Shelf", "Plaque" }) Req(old, n).gameObject.SetActive(false);

            top = NewFrame(canvas, "TopFrame", 390, TopH);
            bottom = NewFrame(canvas, "BottomFrame", 390, BottomH);

            // ---- thanh trên: Cài đặt | Màn N | kéo ×N ----
            hudSettings = IconButton(top, "SettingsButton", "ic_gear", 16, 6, null);
            var lv = Sliced(NewImage(top, "LevelPill", 115, 4, 160, 60), Ui("ui_btn_secondary"), Color.white);
            Restyle(Req(old, "LevelText"), lv.transform, 0, 0, 160, 55, 26, Palette.UiText, heading);
            var cnt = Sliced(NewImage(top, "CounterPill", 282, 6, 92, 56), Ui("ui_btn_secondary"), Color.white);
            NewImage(cnt.transform, "Scissors", 10, 9, 34, 34).sprite = Ui("ic_scissors");
            Restyle(Req(old, "CardsLeft"), cnt.transform, 40, 0, 48, 51, 24, Palette.UiText, heading);

            // ---- dải khách: avatar (chạm để nói chuyện) + bong bóng chỉ chứa lời thoại (mục tiêu đã có trên thớt) ----
            var order = Inst(panelPrefab, top, "OrderPanel", 16, 72, 358, 78).transform;
            var avatar = NewImage(order, "Customer", 10, 9, 56, 56);
            avatar.sprite = Ui("PH_customer_avatar");
            avatar.preserveAspect = true;
            avatar.raycastTarget = true;
            var avatarBtn = avatar.gameObject.AddComponent<Button>();
            avatarBtn.targetGraphic = avatar;
            var bubbleImg = Sliced(NewImage(order, "Bubble", 74, 8, 272, 58), Ui("ui_panel_peach"), Color.white);
            var bubbleRt = bubbleImg.rectTransform;
            bubbleRt.pivot = new Vector2(0, .5f);                 // nảy ra từ phía khách
            bubbleRt.anchoredPosition = new Vector2(74, -(8 + 29));
            var bubble = bubbleImg.transform;

            var speech = NewText(bubble, "Speech", "", 15, Palette.UiText, body);
            speech.alignment = TextAnchor.MiddleLeft;
            speech.horizontalOverflow = HorizontalWrapMode.Wrap;
            speech.lineSpacing = .9f;
            Stretch(speech.rectTransform, 16, 8, 14, 4);

            chat = b.hud.gameObject.AddComponent<CustomerChat>();
            SceneBuilder.Set(chat, "lines", LoadOrCreateLines());
            SceneBuilder.Set(chat, "avatarButton", avatarBtn);
            SceneBuilder.Set(chat, "bubble", bubbleRt);
            SceneBuilder.Set(chat, "speechText", speech);
            // icon món khách gọi: nằm đè góc dưới-phải avatar (bong bóng vẫn chỉ có lời thoại)
            var orderIcon = NewImage(order, "OrderIcon", 40, 34, 40, 40);
            orderIcon.preserveAspect = true;
            SceneBuilder.Set(chat, "orderIcon", orderIcon);
            SceneBuilder.Set(chat, "catalog", LoadOrCreateCatalog());

            // ---- hàng nút: Hoàn tác, Làm lại (chỉ những nút game có) ----
            var undo = IconButton(bottom, "UndoButton", "ic_undo", 16, 0, "Hoàn tác");
            var redo = IconButton(bottom, "RestartButton", "ic_reset", 84, 0, "Làm lại");
            Object.DestroyImmediate(Req(old, "UndoButton").gameObject);
            Object.DestroyImmediate(Req(old, "RestartButton").gameObject);
            SceneBuilder.Set(hudView, "undoButton", undo);
            SceneBuilder.Set(hudView, "restartButton", redo);

            // ---- khay thẻ màu đào neo đáy (kéo dài xuống dưới vùng an toàn cho kín thanh gesture) ----
            var tray = Sliced(NewImage(bottom, "CardTray", 0, 80, 390, 190), Ui("ui_panel_peach"), Color.white);
            var title = NewText(tray.transform, "Title", "THẺ CẮT", 13, Palette.UiBrown, body);
            title.alignment = TextAnchor.MiddleLeft;
            TopLeft((RectTransform)title.transform, 24, 6, 160, 18);

            var hand = Req(old, "Hand");
            hand.SetParent(bottom, false);
            TopLeft((RectTransform)hand, 16, 108, 358, 100);
            hand.GetComponent<Image>().color = Color.clear;
            var queue = Req(old, "QueueBadge");
            queue.SetParent(bottom, false);
            TopLeft((RectTransform)queue, 262, 84, 112, 22);
            Sliced(queue.GetComponent<Image>(), Ui("ui_chip_green"), Color.white);
            var qt = Req(queue, "Text").GetComponent<Text>();
            qt.color = Palette.UiText;
            qt.fontSize = 12;
            qt.font = body;
            Stretch((RectTransform)qt.transform, 0, 0, 0, 0);

            for (int k = 0; k < 3; k++)
            {
                TopLeft((RectTransform)Req(hand, $"Card{k}"), k * 127, 0, 104, 100);
                var empty = Req(hand, $"Empty{k}");
                TopLeft((RectTransform)empty, k * 127, 0, 104, 100);
                Sliced(empty.GetComponent<Image>(), Ui("ui_card_empty"), Color.white);
            }

            // bóng thẻ: trong BottomFrame để cùng cỡ thẻ, khung đáy nằm trên cùng nên bóng thẻ vẽ đè mọi thứ
            var ghostLayer = (RectTransform)Req(canvas, "GhostLayer");
            ghostLayer.SetParent(bottom, false);
            Stretch(ghostLayer, 0, 0, 0, 0);
            ghostLayer.SetAsLastSibling();
            var ghostRoot = (RectTransform)Req(ghostLayer, "Ghost");
            ghostRoot.sizeDelta = new Vector2(104, 100);
            TopLeft((RectTransform)Req(ghostRoot, "GhostCard"), 0, 0, 104, 100);

            foreach (var cv in b.hud.GetComponentsInChildren<CardView>(true)) StyleCard(cv);
            bottom.SetAsLastSibling();
        }

        /// <summary>Thẻ cắt: khung kem viền nâu, ô sơ đồ xanh nhạt (vẽ từ dữ liệu thẻ thật), tên đậm, dấu xoay tròn san hô.</summary>
        static void StyleCard(CardView cv)
        {
            var bodyT = Req(cv.transform, "Body");
            var card = Ui("ui_card");
            Sliced(bodyT.GetComponent<Image>(), card, Color.white);
            foreach (var f in new[] { "lineFrame", "moldFrame", "diagFrame", "fnFrame" }) SceneBuilder.Set(cv, f, card);

            var box = Sliced(NewImage(bodyT, "ArtBox", 26, 8, 52, 52), Ui("ui_panel"), new Color32(0xD9, 0xEE, 0xFA, 0xFF));
            box.pixelsPerUnitMultiplier = 6;
            box.transform.SetSiblingIndex(0);
            TopLeft((RectTransform)Req(bodyT, "Art"), 30, 10, 44, 44);
            TopLeft((RectTransform)Req(bodyT, "Icon"), 31, 11, 42, 42);
            Restyle(Req(bodyT, "Name"), bodyT, 0, 64, 104, 26, 17, Palette.UiText, heading);

            var rot = Req(bodyT, "RotBadge");
            TopLeft((RectTransform)rot, 80, -6, 28, 28);
            var ri = rot.GetComponent<Image>();
            ri.sprite = Ui("badge_red");
            ri.preserveAspect = true;
            ri.color = Color.white;
            Req(rot, "Glyph").gameObject.SetActive(false);   // font không có "↻": dùng icon vẽ sẵn
            NewImage(rot, "RotIcon", 5, 5, 18, 18).sprite = Ui("ic_rotate");

            var ring = Sliced(new GameObject("SelectRing", typeof(RectTransform), typeof(Image)).GetComponent<Image>(), Ui("ui_card_ring"), Color.white);
            ring.transform.SetParent(bodyT, false);
            Stretch(ring.rectTransform, -7, -7, -7, -7);
            ring.raycastTarget = false;
            ring.gameObject.SetActive(false);
            SceneBuilder.Set(cv, "selectRing", ring.gameObject);
        }

        static void StyleWinPopup(SceneBuilder.Built b)
        {
            var win = b.popup.GetComponent<WinPopup>();
            var dim = Req(b.popup.transform, "WinPopup").GetComponent<Image>();
            dim.color = new Color(Palette.UiText.r, Palette.UiText.g, Palette.UiText.b, .45f);
            var panel = Req(b.popup.transform, "WinPopup/Frame/Panel");
            Sliced(panel.GetComponent<Image>(), Ui("ui_panel"), Color.white);
            Restyle(Req(panel, "Title"), panel, 10, 96, 280, 44, 30, Palette.UiText, heading);
            Restyle(Req(panel, "Info"), panel, 10, 138, 280, 24, 16, Palette.UiBrown, body);
            var oldBtn = Req(panel, "NextButton").gameObject;
            var next = TextButton(primaryPrefab, panel, "NextButton", "Tiếp tục", 50, 180, 200, 64, 24, out var nextLabel);
            Object.DestroyImmediate(oldBtn);
            SceneBuilder.Set(win, "nextButton", next);
            SceneBuilder.Set(win, "nextLabel", nextLabel);
        }

        // ================= Main Menu: 3 tab dưới cùng (Hành trình · Trang chủ · Cửa hàng), xe dùng chung =================
        // Thứ tự trong khung: xe → 3 trang → nút Cài đặt → thanh tab. Trang vẽ đè lên xe (nhân vật đứng trước xe).

        static readonly string[] ShopTabs = { "Hộp cơm", "Thớt", "Theme" };   // trùng thứ tự ShopCategory

        static MainMenuView BuildMainMenu(GameObject ui, out RectTransform frame)
        {
            var canvas = SceneBuilder.MakeCanvas(ui, "Canvas_Menu", 10);
            var view = canvas.gameObject.AddComponent<MainMenuView>();

            var root = new GameObject("MainMenu", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)root.transform, 0, 0, 0, 0);
            var group = root.AddComponent<CanvasGroup>();

            // nền phố phủ kín màn hình, giữ tỉ lệ (cắt bớt mép); chặn chạm xuống màn chơi bên dưới
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(root.transform, false);
            Stretch((RectTransform)bgGo.transform, 0, 0, 0, 0);
            var bg = bgGo.AddComponent<Image>();
            var bgSprite = TryLoad("UI/Home/bg_menu_hanoi");
            if (bgSprite != null)
            {
                bg.sprite = bgSprite;
                var fit = bgGo.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = bgSprite.rect.width / bgSprite.rect.height;
            }
            else
            {
                Placeholder(bg, "bg_menu_hanoi", Palette.UiSky);
                bg.color = Palette.UiCreamDark;   // nền phải đục: không lộ màn chơi bên dưới
            }

            frame = (RectTransform)canvas.transform.Find("Frame");
            frame.SetParent(root.transform, false);
            frame.SetAsLastSibling();

            // xe dùng chung + chỗ đỗ ở từng trang (Hành trình: nhỏ trên cao, Trang chủ: giữa phố, Cửa hàng: khung xem trước)
            var truck = ArtImage(frame, "Truck", "truck_main", 20, 330, 350, 262, Palette.UiOrange);
            var spots = new[]
            {
                Spot(frame, "TruckSpot_Journey", 135, 80, 120, 80),   // nhỏ, chừa chỗ cho tab con
                Spot(frame, "TruckSpot_Home", 20, 330, 350, 262),
                Spot(frame, "TruckSpot_Shop", 60, 92, 270, 202),
            };

            var pages = new[] { Page(frame, "Page_Journey"), Page(frame, "Page_Home"), Page(frame, "Page_Shop") };
            var journey = BuildJourney(canvas.gameObject, pages[0].transform);
            var home = pages[1].transform;
            var shop = BuildShop(canvas.gameObject, pages[2].transform, BuildCosmetics(canvas.gameObject, bg));

            // ---- Trang chủ: logo, nhân vật, nút Chơi tiếp ----
            var plate = Inst(panelPrefab, home, "Logo", 70, 76, 250, 132).transform;
            var t1 = NewText(plate, "Bento", "Bento", 54, Palette.UiCoral, heading);
            TopLeft((RectTransform)t1.transform, 0, 4, 250, 64);
            var t2 = NewText(plate, "Journey", "Journey", 34, Palette.UiText, heading);
            TopLeft((RectTransform)t2.transform, 0, 62, 250, 44);
            var chip = Sliced(NewImage(home, "CityChip", 120, 194, 150, 30), Ui("ui_chip_peach"), Color.white);
            Stretch((RectTransform)NewText(chip.transform, "Label", "Hà Nội · Seoul", 14, Palette.UiText, body).transform, 0, 0, 0, 0);
            ArtImage(home, "CharVn", "char_vn", 0, 466, 110, 165, Palette.UiSage);
            ArtImage(home, "CharKr", "char_kr", 280, 466, 110, 165, Palette.UiCoral);

            var play = Inst(primaryPrefab, home, "PlayButton", 40, 638, 310, 80);
            var content = play.transform.Find("Content");
            var title = content.Find("Label").GetComponent<Text>();
            title.text = "Chơi tiếp";
            title.fontSize = 30;
            TopLeft(title.rectTransform, 64, 6, 230, 40);
            var sub = NewText(content, "Sub", "Màn 1", 15, Palette.UiText, body);
            TopLeft((RectTransform)sub.transform, 64, 44, 230, 22);
            NewImage(content, "PlayIcon", 26, 20, 34, 34).sprite = Ui("ic_play");

            // ---- chung: nút Cài đặt (trên trái), thanh 3 tab (dưới) ----
            var settingsBtn = IconButton(frame, "SettingsButton", "ic_gear", 16, 16, null);
            var dock = Inst(panelPrefab, frame, "TabBar", 16, 734, 358, 104).transform;
            var tabs = new[]
            {
                IconButton(dock, "Tab_Journey", "ic_map", 32, 8, "Hành trình"),
                IconButton(dock, "Tab_Home", "ic_home", 151, 8, "Trang chủ"),
                IconButton(dock, "Tab_Shop", "ic_bag", 270, 8, "Cửa hàng"),
            };

            SceneBuilder.Set(view, "root", root);
            SceneBuilder.Set(view, "group", group);
            SceneBuilder.Set(view, "truck", (RectTransform)truck.transform);
            SceneBuilder.Set(view, "playButton", play.GetComponent<Button>());
            SceneBuilder.Set(view, "playLabel", sub);
            SceneBuilder.Set(view, "settingsButton", settingsBtn);
            SceneBuilder.Set(view, "tabs", tabs);
            SceneBuilder.Set(view, "pages", pages);
            SceneBuilder.Set(view, "truckSpots", spots);
            SceneBuilder.Set(view, "journey", journey);
            SceneBuilder.Set(view, "shop", shop);
            pages[0].gameObject.SetActive(false);
            pages[2].gameObject.SetActive(false);
            return view;
        }

        static RectTransform Spot(Transform parent, string name, float x, float y, float w, float h)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            TopLeft(rt, x, y, w, h);
            return rt;
        }

        static CanvasGroup Page(Transform frame, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(frame, false);
            Stretch((RectTransform)go.transform, 0, 0, 0, 0);
            return go.GetComponent<CanvasGroup>();
        }

        /// <summary>Tiêu đề trang dạng viên kem có bóng (không phải nút).</summary>
        static void TitlePill(Transform parent, string text)
        {
            var pill = Sliced(NewImage(parent, "Title", 110, 24, 170, 54), Ui("ui_btn_secondary"), Color.white);
            Stretch((RectTransform)NewText(pill.transform, "Label", text, 24, Palette.UiText, heading).transform, 0, 5, 0, 0);
        }

        // ================= Tab Hành trình: bộ sưu tập món ăn =================

        static JourneyView BuildJourney(GameObject host, Transform page)
        {
            var view = host.AddComponent<JourneyView>();
            TitlePill(page, "Hành trình");
            var cat = LoadOrCreateCatalog();
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelImporter.DatabasePath);

            // ô to cho điện thoại: món đặc trưng 3 cột, món phụ 4 cột, cuộn dọc; dải giới thiệu cố định ở đáy
            var panel = Inst(panelPrefab, page, "Collection", 16, 200, 358, 526).transform;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(panel, false);
            TopLeft((RectTransform)viewport.transform, 0, 8, 358, 424);
            var vpImg = viewport.GetComponent<Image>();
            vpImg.color = new Color(1, 1, 1, 0);           // nhận chạm để kéo cuộn ở khoảng trống
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewport.transform, false);
            var content = (RectTransform)contentGo.transform;
            var scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30;

            Text Header(string name, string text, float y, out Text count)
            {
                var h = NewText(content, name, text, 15, Palette.UiBrown, body);
                h.alignment = TextAnchor.MiddleLeft;
                TopLeft(h.rectTransform, 20, y, 220, 24);
                count = NewText(content, name + "Count", "0/0", 15, Palette.UiBrown, body);
                count.alignment = TextAnchor.MiddleRight;
                TopLeft(count.rectTransform, 238, y, 100, 24);
                return h;
            }
            Button Cell(string name, float x, float y, float w, float h, out Image icon, float size)
            {
                var bg = Sliced(NewImage(content, name, x, y, w, h), Ui("ui_card"), Color.white);
                bg.raycastTarget = true;
                var btn = bg.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                icon = NewImage(bg.transform, "Icon", (w - size) / 2, 6, size, size);
                icon.preserveAspect = true;
                return btn;
            }

            // ---- món đặc trưng: mỗi vùng một khối 3 cột (thứ tự catalog.dishes: Hà Nội trước, Seoul sau) ----
            Header("DishHeader", "MÓN ĐẶC TRƯNG", 4, out var dishCount);
            int nd = cat.dishes.Count;
            var dishCells = new Button[nd];
            var dishIcons = new Image[nd];
            var dishLabels = new Text[nd];
            var dishLocks = new GameObject[nd];
            const float dw = 150, dh = 160, dgap = 14, dx0 = (358 - 2 * dw - dgap) / 2;   // 2×2 mỗi vùng, căn giữa
            float y = 32;
            for (int r = 0; r < 2; r++)
            {
                var chip = Sliced(NewImage(content, $"Region{r}", 20, y, 96, 26), Ui(r == 0 ? "ui_chip_peach" : "ui_chip_green"), Color.white);
                Stretch(NewText(chip.transform, "Label", r == 0 ? "Hà Nội" : "Seoul", 14, Palette.UiText, body).rectTransform, 0, 0, 0, 0);
                y += 32;
                int start = r * 4, count = Mathf.Min(4, nd - start);
                for (int k = 0; k < count; k++)
                {
                    int i = start + k;
                    dishCells[i] = Cell($"Dish{i}", dx0 + k % 2 * (dw + dgap), y + k / 2 * (dh + dgap), dw, dh, out dishIcons[i], 112);
                    var lockImg = NewImage(dishCells[i].transform, "Lock", dw - 34, 8, 26, 26);
                    lockImg.sprite = Ui("ic_lock");
                    lockImg.preserveAspect = true;
                    dishLocks[i] = lockImg.gameObject;
                    var label = NewText(dishCells[i].transform, "Label", "", 17, Palette.UiText, body);
                    TopLeft(label.rectTransform, 6, 122, dw - 12, 32);
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 12;
                    label.resizeTextMaxSize = 17;
                    label.horizontalOverflow = HorizontalWrapMode.Wrap;
                    dishLabels[i] = label;
                }
                y += ((count + 1) / 2) * (dh + dgap) + 6;
            }

            // ---- món phụ: lưới 4 cột ----
            Header("SideHeader", "MÓN PHỤ", y, out var sideCount);
            y += 30;
            int ns = cat.sideDishes.Count;
            var sideCells = new Button[ns];
            var sideIcons = new Image[ns];
            const float sw = 76, sh = 76, sgap = 8;
            for (int i = 0; i < ns; i++)
                sideCells[i] = Cell($"Side{i}", 19 + i % 4 * (sw + sgap), y + i / 4 * (sh + sgap), sw, sh, out sideIcons[i], 64);
            y += ((ns + 3) / 4) * (sh + sgap) + 8;
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, y);

            // ---- dải giới thiệu (cố định dưới vùng cuộn) ----
            var detailBg = Sliced(NewImage(panel, "Detail", 16, 438, 326, 76), Ui("ui_panel_peach"), Color.white);
            var detail = NewText(detailBg.transform, "Text", "", 14, Palette.UiText, body);
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            detail.verticalOverflow = VerticalWrapMode.Truncate;
            detail.supportRichText = true;
            detail.lineSpacing = .9f;
            detail.alignment = TextAnchor.MiddleCenter;
            detail.resizeTextForBestFit = true;
            detail.resizeTextMinSize = 11;
            detail.resizeTextMaxSize = 14;
            Stretch(detail.rectTransform, 12, 4, 12, 4);

            SceneBuilder.Set(view, "catalog", cat);
            SceneBuilder.Set(view, "database", db);
            SceneBuilder.Set(view, "dishCells", dishCells);
            SceneBuilder.Set(view, "dishIcons", dishIcons);
            SceneBuilder.Set(view, "dishLabels", dishLabels);
            SceneBuilder.Set(view, "dishLocks", dishLocks);
            SceneBuilder.Set(view, "sideCells", sideCells);
            SceneBuilder.Set(view, "sideIcons", sideIcons);
            SceneBuilder.Set(view, "dishCount", dishCount);
            SceneBuilder.Set(view, "sideCount", sideCount);
            SceneBuilder.Set(view, "detailText", detail);
            SceneBuilder.Set(view, "lockedIcon", Ui("ic_lock"));

            // ---- tab con + sổ hành trình ----
            Button SubTab(string name, string text, float x)
            {
                var b = TextButton(secondaryPrefab, page, name, text, x, 160, 150, 38, 15, out _);
                return b;
            }
            SceneBuilder.Set(view, "collectionTab", SubTab("CollectionTab", "Bộ sưu tập", 36));
            SceneBuilder.Set(view, "bookTab", SubTab("BookTab", "Sổ hành trình", 204));
            SceneBuilder.Set(view, "collectionRoot", panel.gameObject);
            var book = BuildRouteBook(host, page, db);
            SceneBuilder.Set(view, "bookRoot", book.gameObject);
            SceneBuilder.Set(view, "book", book.GetComponent<RouteBookView>());
            book.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Sổ hành trình: 20 màn mỗi trang, đường đi uốn khúc từ dưới lên (4 màn mỗi hàng).</summary>
        static Transform BuildRouteBook(GameObject host, Transform page, LevelDatabase db)
        {
            var panel = Inst(panelPrefab, page, "RouteBook", 16, 200, 358, 526).transform;
            var view = panel.gameObject.AddComponent<RouteBookView>();
            var prev = IconButton(panel, "PrevButton", "ic_back", 12, 8, null, 48, 48);
            var next = IconButton(panel, "NextButton", "ic_back", 298, 8, null, 48, 48);
            next.transform.Find("Content/Icon").localScale = new Vector3(-1, 1, 1);
            var title = NewText(panel, "Title", "Hà Nội · Màn 1–20", 18, Palette.UiText, heading);
            TopLeft(title.rectTransform, 64, 12, 230, 36);

            const int perRow = 4, rows = 5, n = perRow * rows;
            const float node = 52, x0 = 30, dx = 82, yBottom = 434, dy = 88;
            Vector2 Pos(int i)
            {
                int r = i / perRow, c = i % perRow;
                if (r % 2 == 1) c = perRow - 1 - c;            // đường uốn khúc
                return new Vector2(x0 + c * dx, yBottom - r * dy);
            }
            // đường đi: nối tâm các màn liên tiếp
            var road = new GameObject("Road", typeof(RectTransform)).transform;
            road.SetParent(panel, false);
            Stretch((RectTransform)road, 0, 0, 0, 0);
            for (int i = 0; i + 1 < n; i++)
            {
                Vector2 a = Pos(i) + Vector2.one * node / 2, b = Pos(i + 1) + Vector2.one * node / 2;
                float x = Mathf.Min(a.x, b.x) - 7, y = Mathf.Min(a.y, b.y) - 7;
                NewImage(road, $"Seg{i}", x, y, Mathf.Abs(b.x - a.x) + 14, Mathf.Abs(b.y - a.y) + 14).color = Palette.UiPeach;
            }

            var nodes = new Button[n];
            var images = new Image[n];
            var labels = new Text[n];
            var badges = new Image[n];
            for (int i = 0; i < n; i++)
            {
                var p = Pos(i);
                var img = Sliced(NewImage(panel, $"Node{i}", p.x, p.y, node, node), Ui("ui_btn_secondary"), Color.white);
                img.raycastTarget = true;
                nodes[i] = img.gameObject.AddComponent<Button>();
                nodes[i].targetGraphic = img;
                images[i] = img;
                labels[i] = NewText(img.transform, "Label", (i + 1).ToString(), 18, Palette.UiText, heading);
                Stretch(labels[i].rectTransform, 0, 5, 0, 0);
                badges[i] = NewImage(img.transform, "Dish", node - 17, -13, 30, 30);
                badges[i].preserveAspect = true;
                badges[i].gameObject.SetActive(false);
            }
            var truck = NewImage(panel, "Truck", 0, 0, 52, 40);
            truck.sprite = TryLoad("UI/Home/truck_main");
            truck.preserveAspect = true;
            var hint = NewText(panel, "Hint", "Chạm vào màn đã qua để chơi lại", 13, Palette.UiBrown, body);
            TopLeft(hint.rectTransform, 16, 494, 326, 20);

            SceneBuilder.Set(view, "database", db);
            SceneBuilder.Set(view, "nodes", nodes);
            SceneBuilder.Set(view, "nodeImages", images);
            SceneBuilder.Set(view, "nodeLabels", labels);
            SceneBuilder.Set(view, "dishBadges", badges);
            SceneBuilder.Set(view, "truckMarker", truck.rectTransform);
            SceneBuilder.Set(view, "pageTitle", title);
            SceneBuilder.Set(view, "prevButton", prev);
            SceneBuilder.Set(view, "nextButton", next);
            SceneBuilder.Set(view, "clearedSprite", Ui("ui_btn_secondary"));
            SceneBuilder.Set(view, "currentSprite", Ui("ui_btn_primary"));
            SceneBuilder.Set(view, "lockedBadge", Ui("ic_lock"));
            return panel;
        }

        // ================= Tab Cửa hàng (vỏ giao diện): xe đỗ trên cao, danh mục + vật phẩm "Sắp có" bên dưới =================

        static ShopView BuildShop(GameObject host, Transform page, CosmeticsApplier applier)
        {
            var view = host.AddComponent<ShopView>();
            TitlePill(page, "Cửa hàng");

            var panel = Inst(panelPrefab, page, "Items", 16, 312, 358, 432).transform;
            var tabs = new Button[ShopTabs.Length];
            var tabLabels = new Text[ShopTabs.Length];
            for (int i = 0; i < tabs.Length; i++)
                tabs[i] = TextButton(secondaryPrefab, panel, $"Tab{i}", ShopTabs[i], 12 + i * 113, 14, 107, 54, 17, out tabLabels[i]);

            // 3 thẻ to mỗi danh mục: ảnh xem trước, tên, trạng thái (Đang dùng / Dùng / Qua Màn X)
            const int n = 3;
            var buttons = new Button[n];
            var icons = new Image[n];
            var names = new Text[n];
            var states = new Text[n];
            var locks = new GameObject[n];
            for (int i = 0; i < n; i++)
            {
                var card = Sliced(NewImage(panel, $"Item{i}", 14 + i * 112, 84, 104, 250), Ui("ui_card"), Color.white);
                card.raycastTarget = true;
                buttons[i] = card.gameObject.AddComponent<Button>();
                buttons[i].targetGraphic = card;
                icons[i] = NewImage(card.transform, "Icon", 10, 12, 84, 120);
                icons[i].preserveAspect = true;
                names[i] = NewText(card.transform, "Name", "", 16, Palette.UiText, heading);
                names[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                names[i].lineSpacing = .85f;
                TopLeft(names[i].rectTransform, 6, 138, 92, 48);
                states[i] = NewText(card.transform, "State", "", 15, Palette.UiOrange, body);
                states[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                TopLeft(states[i].rectTransform, 4, 192, 96, 36);
                var lockImg = NewImage(card.transform, "Lock", 70, 8, 28, 28);
                lockImg.sprite = Ui("ic_lock");
                lockImg.preserveAspect = true;
                locks[i] = lockImg.gameObject;
            }
            var hint = NewText(panel, "Hint", "Qua thêm màn để mở món trang trí mới", 14, Palette.UiBrown, body);
            TopLeft(hint.rectTransform, 14, 352, 330, 24);

            SceneBuilder.Set(view, "tabs", tabs);
            SceneBuilder.Set(view, "tabLabels", tabLabels);
            SceneBuilder.Set(view, "applier", applier);
            SceneBuilder.Set(view, "itemButtons", buttons);
            SceneBuilder.Set(view, "itemIcons", icons);
            SceneBuilder.Set(view, "itemNames", names);
            SceneBuilder.Set(view, "itemStates", states);
            SceneBuilder.Set(view, "itemLocks", locks);
            return view;
        }

        /// <summary>Áp món trang trí: màu hộp, khung thớt (BoardBg), nền menu.</summary>
        static CosmeticsApplier BuildCosmetics(GameObject host, Image menuBg)
        {
            var applier = host.AddComponent<CosmeticsApplier>();
            SceneBuilder.Set(applier, "catalog", LoadOrCreateShop());
            SceneBuilder.Set(applier, "menuBackground", menuBg);
            SceneBuilder.Set(applier, "menuFit", menuBg.GetComponent<AspectRatioFitter>());
            SceneBuilder.Set(applier, "boardBg", GameObject.Find("BoardBg").GetComponent<SpriteRenderer>());
            return applier;
        }

        public const string ShopPath = "Assets/_Project/Data/Config/ShopCatalog.asset";

        /// <summary>Danh mục Cửa hàng; chưa có thì tạo bộ mặc định (mở theo tiến độ). Không ghi đè bản bạn đã sửa.</summary>
        public static ShopCatalog LoadOrCreateShop()
        {
            var cat = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopPath);
            if (cat != null) return cat;
            cat = ScriptableObject.CreateInstance<ShopCatalog>();
            var tin = TryLoad("InGame/Gameplay/tin_cell");
            var frame = TryLoad("InGame/Gameplay/board_frame");
            ShopItem Box(string id, string name, int at, string rim, string outline, string well) => new ShopItem
            {
                id = id, displayName = name, category = ShopCategory.Box, unlockAfter = at, icon = TryLoad("UI/Shop/shop_" + id) ?? tin,
                customBox = rim != null,
                boxRim = rim != null ? Palette.Hex(rim) : Color.white,
                boxOutline = outline != null ? Palette.Hex(outline) : Color.gray,
                boxWell = well != null ? Palette.Hex(well) : Color.gray,
            };
            ShopItem Board(string id, string name, int at, string tint) => new ShopItem
            {
                id = id, displayName = name, category = ShopCategory.Board, unlockAfter = at, icon = TryLoad("UI/Shop/shop_" + id) ?? frame,
                boardTint = Palette.Hex(tint),
            };
            ShopItem Theme(string id, string name, int at, string file) => new ShopItem
            {
                id = id, displayName = name, category = ShopCategory.Theme, unlockAfter = at,
                icon = TryLoad("UI/Home/" + file), menuBackground = TryLoad("UI/Home/" + file),
            };
            cat.items.Add(Box("box_tin", "Hộp thiếc", 0, null, null, null));
            cat.items.Add(Box("box_wood", "Hộp gỗ", 10, "#E2B57E", "#8A5A32", "#B98352"));
            cat.items.Add(Box("box_porcelain", "Hộp sứ xanh", 25, "#F4F7FA", "#4C7DB8", "#A9C6E8"));
            cat.items.Add(Board("board_light", "Gỗ sáng", 0, "#FFFFFF"));
            cat.items.Add(Board("board_dark", "Gỗ tối", 15, "#C49A7E"));
            cat.items.Add(Board("board_stone", "Thớt đá", 30, "#C9D0D8"));
            cat.items.Add(Theme("theme_hanoi", "Hà Nội ban ngày", 0, "bg_menu_hanoi"));
            cat.items.Add(Theme("theme_seoul", "Seoul về đêm", 20, "bg_menu_seoul"));
            AssetDatabase.CreateAsset(cat, ShopPath);
            AssetDatabase.SaveAssets();
            return cat;
        }

        // ================= Popup Cài đặt =================

        static SettingsPopup BuildSettings(Canvas popCanvas, out RectTransform frame)
        {
            var view = popCanvas.gameObject.AddComponent<SettingsPopup>();
            var root = new GameObject("SettingsPopup", typeof(RectTransform));
            root.transform.SetParent(popCanvas.transform, false);
            Stretch((RectTransform)root.transform, 0, 0, 0, 0);
            root.AddComponent<Image>().color = new Color(Palette.UiText.r, Palette.UiText.g, Palette.UiText.b, .45f);
            frame = NewFrame(root.transform, "Frame", 390, 844);

            // panel căn giữa; mở từ menu thì thu ngắn (SettingsPopup đổi chiều cao, pivot giữa nên vẫn căn giữa)
            var panel = Inst(panelPrefab, frame, "Panel", 45, 237, 300, 370);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(.5f, .5f);
            prt.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().raycastTarget = true;

            var pt = panel.transform;
            var title = NewText(pt, "Title", "Cài đặt", 28, Palette.UiText, heading);
            TopLeft((RectTransform)title.transform, 30, 16, 240, 40);
            var close = IconButton(pt, "CloseButton", "ic_close", 238, 12, null, 48, 52);
            var sound = TextButton(secondaryPrefab, pt, "SoundButton", "Âm thanh: Bật", 30, 70, 240, 60, 18, out var soundLabel);
            var vib = TextButton(secondaryPrefab, pt, "VibrationButton", "Rung: Bật", 30, 138, 240, 60, 18, out var vibLabel);
            var restart = TextButton(secondaryPrefab, pt, "RestartButton", "Chơi lại", 30, 216, 240, 60, 18, out _);
            var home = TextButton(primaryPrefab, pt, "HomeButton", "Về menu", 30, 284, 240, 64, 20, out _);

            SceneBuilder.Set(view, "root", root);
            root.SetActive(false);
            SceneBuilder.Set(view, "panel", prt);
            SceneBuilder.Set(view, "closeButton", close);
            SceneBuilder.Set(view, "soundButton", sound);
            SceneBuilder.Set(view, "soundLabel", soundLabel);
            SceneBuilder.Set(view, "vibrationButton", vib);
            SceneBuilder.Set(view, "vibrationLabel", vibLabel);
            SceneBuilder.Set(view, "restartButton", restart);
            SceneBuilder.Set(view, "homeButton", home);
            return view;
        }

        /// <summary>Popup "Món mới!": nằm trên popup Hoàn thành (anh em sau cùng trong Canvas_Popup).</summary>
        static DishPopup BuildDishPopup(Canvas popCanvas, out RectTransform frame)
        {
            var view = popCanvas.gameObject.AddComponent<DishPopup>();
            var root = new GameObject("DishPopup", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(popCanvas.transform, false);
            Stretch((RectTransform)root.transform, 0, 0, 0, 0);
            root.AddComponent<Image>().color = new Color(Palette.UiText.r, Palette.UiText.g, Palette.UiText.b, .55f);
            frame = NewFrame(root.transform, "Frame", 390, 844);

            var panel = Inst(panelPrefab, frame, "Panel", 24, 150, 342, 540);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(.5f, .5f);
            prt.anchoredPosition = new Vector2(0, 10);
            panel.GetComponent<Image>().raycastTarget = true;
            var pt = panel.transform;

            var title = NewText(pt, "Title", "Món mới!", 30, Palette.UiOrange, heading);
            TopLeft((RectTransform)title.transform, 20, 14, 302, 44);
            var chip = Sliced(NewImage(pt, "RegionChip", 111, 60, 120, 30), Ui("ui_chip_peach"), Color.white);
            var region = NewText(chip.transform, "Region", "Hà Nội", 15, Palette.UiText, body);
            Stretch(region.rectTransform, 0, 0, 0, 0);

            var artBg = Sliced(NewImage(pt, "ArtBg", 26, 102, 290, 220), Ui("ui_panel_peach"), Color.white);
            var art = NewImage(artBg.transform, "Art", 0, 0, 0, 0);
            Stretch(art.rectTransform, 14, 14, 14, 14);
            art.preserveAspect = true;

            var name = NewText(pt, "Name", "Bún chả", 28, Palette.UiText, heading);
            TopLeft((RectTransform)name.transform, 20, 330, 302, 40);
            var intro = NewText(pt, "Intro", "", 15, Palette.UiBrown, body);
            intro.horizontalOverflow = HorizontalWrapMode.Wrap;
            intro.alignment = TextAnchor.UpperCenter;
            intro.lineSpacing = .95f;
            TopLeft(intro.rectTransform, 26, 374, 290, 74);
            var ok = TextButton(primaryPrefab, pt, "OkButton", "Tuyệt!", 61, 456, 220, 64, 22, out _);

            SceneBuilder.Set(view, "root", root);
            SceneBuilder.Set(view, "group", root.GetComponent<CanvasGroup>());
            SceneBuilder.Set(view, "panel", prt);
            SceneBuilder.Set(view, "art", art);
            SceneBuilder.Set(view, "nameText", name);
            SceneBuilder.Set(view, "regionText", region);
            SceneBuilder.Set(view, "introText", intro);
            SceneBuilder.Set(view, "okButton", ok);
            root.transform.SetAsLastSibling();
            root.SetActive(false);
            return view;
        }

        // ================= Danh mục món ăn =================

        public const string CatalogPath = "Assets/_Project/Data/Config/FoodCatalog.asset";

        /// <summary>Lấy danh mục món; chưa có thì tạo với 3 món phụ đang dùng trong level. Không ghi đè món bạn đã thêm.</summary>
        public static FoodCatalog LoadOrCreateCatalog()
        {
            var cat = AssetDatabase.LoadAssetAtPath<FoodCatalog>(CatalogPath);
            if (cat != null) return cat;
            cat = ScriptableObject.CreateInstance<FoodCatalog>();
            SideDish Side(string id, string name, string file) => new SideDish
            {
                id = id, displayName = name, region = FoodRegion.None,
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "InGame/Gameplay/" + file + ".png"),
            };
            cat.sideDishes.Add(Side("egg", "Trứng ốp la", "food_egg"));
            cat.sideDishes.Add(Side("sausage", "Xúc xích", "food_sausage"));
            cat.sideDishes.Add(Side("meat", "Thịt ba chỉ", "food_pork_belly"));
            AssetDatabase.CreateAsset(cat, CatalogPath);
            AssetDatabase.SaveAssets();
            return cat;
        }

        // ================= Lời thoại khách =================

        const string LinesPath = "Assets/_Project/Data/Config/CustomerLines.asset";

        /// <summary>Lấy asset câu thoại; chưa có thì tạo với câu mặc định. Không bao giờ ghi đè câu bạn đã sửa.</summary>
        static CustomerLines LoadOrCreateLines()
        {
            var lines = AssetDatabase.LoadAssetAtPath<CustomerLines>(LinesPath);
            if (lines != null) return lines;
            lines = ScriptableObject.CreateInstance<CustomerLines>();
            lines.chitChat = new[]
            {
                "Hôm nay trời Hà Nội đẹp ghê!",
                "Xe của bạn dễ thương quá đi!",
                "Mình đi làm sớm nên đói lắm rồi…",
                "Nghe nói xe sắp đi Seoul hả?",
                "Phố đông quá, may mà còn xe của bạn.",
                "Lần đầu mình ghé đó, làm khéo nha!",
            };
            lines.foodTalk = new[]
            {
                "Cho mình một hộp như này nhé!",
                "Cơm phải đầy hộp mới ngon!",
                "Xếp khéo giúp mình, đừng hở chỗ nào nha.",
                "Hộp này mang lên văn phòng ăn trưa đó.",
                "Nhìn hình mẫu là thấy đói bụng rồi!",
            };
            lines.win = new[]
            {
                "Ngon quá! Cảm ơn nha!",
                "Đẹp y như hình luôn!",
                "Vừa khít luôn, giỏi ghê!",
                "Mai mình lại ghé nhé!",
            };
            EnsureFolder(Path.GetDirectoryName(LinesPath));
            AssetDatabase.CreateAsset(lines, LinesPath);
            AssetDatabase.SaveAssets();
            return lines;
        }

        // ================= Tiện ích =================

        static void SavePrefab(GameObject go, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
        }

        /// <summary>Khung thiết kế neo giữa cha (DesignFrameFitter đặt vị trí / tỉ lệ theo vùng an toàn).</summary>
        static RectTransform NewFrame(Transform parent, string name, float w, float h)
        {
            var f = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            f.SetParent(parent, false);
            f.anchorMin = f.anchorMax = f.pivot = new Vector2(.5f, .5f);
            f.sizeDelta = new Vector2(w, h);
            return f;
        }

        /// <summary>Neo góc trên-trái của cha, toạ độ (x, y) tính từ góc trên-trái, y hướng xuống.</summary>
        static void TopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>Phủ kín cha, chừa lề trái / dưới / phải / trên.</summary>
        static void Stretch(RectTransform rt, float l, float bottom, float r, float t)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(.5f, .5f);
            rt.offsetMin = new Vector2(l, bottom);
            rt.offsetMax = new Vector2(-r, -t);
        }

        static Image NewImage(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            TopLeft((RectTransform)go.transform, x, y, w, h);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        static Text NewText(Transform parent, string name, string text, int size, Color col, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = col;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Chuyển chữ có sẵn (giữ tham chiếu của script) sang cha mới, đổi font / cỡ / màu, bỏ viền chữ kiểu cũ.</summary>
        static void Restyle(Transform t, Transform parent, float x, float y, float w, float h, int size, Color col, Font font)
        {
            t.SetParent(parent, false);
            TopLeft((RectTransform)t, x, y, w, h);
            var text = t.GetComponent<Text>();
            text.font = font;
            text.fontStyle = FontStyle.Normal;
            text.fontSize = size;
            text.color = col;
            text.alignment = TextAnchor.MiddleCenter;
            var o = t.GetComponent<Outline>();
            if (o != null) Object.DestroyImmediate(o);
        }

        /// <summary>Chữ còn dùng font mặc định: đổi sang Nunito, bỏ viền chữ.</summary>
        static void ApplyFonts(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Text>(true))
            {
                if (t.font != null && t.font != heading && t.font != body)
                {
                    t.font = body;
                    t.fontStyle = FontStyle.Normal;
                }
                var o = t.GetComponent<Outline>();
                if (o != null) Object.DestroyImmediate(o);
            }
        }

        static Image Sliced(Image img, Sprite s, Color col)
        {
            img.sprite = s;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = Ppum;
            img.color = col;
            return img;
        }

        /// <summary>Ảnh art trong Source/Art/UI/Home; thiếu file thì thành ô giữ chỗ có màu và tên "(THIẾU ...)".</summary>
        static Image ArtImage(Transform parent, string name, string file, float x, float y, float w, float h, Color placeholder)
        {
            var s = TryLoad("UI/Home/" + file);
            var img = NewImage(parent, name, x, y, w, h);
            img.sprite = s;
            if (s != null) img.preserveAspect = true;
            else Placeholder(img, file, placeholder);
            return img;
        }

        static void Placeholder(Image img, string file, Color col)
        {
            img.color = new Color(col.r, col.g, col.b, .55f);
            img.gameObject.name += $" (THIẾU {file}.png)";
            Debug.LogWarning($"[BentoJourneyBuilder] Chưa có {Art}UI/Home/{file}.png: dùng ô giữ chỗ. Thêm file rồi dựng lại.");
        }

        static Sprite TryLoad(string rel) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + rel + ".png");

        static Sprite Ui(string name) => Load("UI/Common/" + name);

        static Sprite Load(string rel)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(Art + rel + ".png");
            if (s == null) throw new System.InvalidOperationException($"[BentoJourneyBuilder] Thiếu sprite {Art}{rel}.png");
            return s;
        }

        static Font LoadFont(string file)
        {
            var f = AssetDatabase.LoadAssetAtPath<Font>(Art + "Fonts/" + file);
            if (f == null) throw new System.InvalidOperationException($"[BentoJourneyBuilder] Thiếu font {Art}Fonts/{file}");
            return f;
        }

        /// <summary>Tìm con theo đường dẫn tên; báo lỗi rõ nếu SceneBuilder đổi tên object.</summary>
        static Transform Req(Transform root, string path)
        {
            var t = root.Find(path);
            if (t == null) throw new System.InvalidOperationException($"[BentoJourneyBuilder] Không tìm thấy {root.name}/{path} (SceneBuilder đã đổi tên object?)");
            return t;
        }

        /// <summary>Tạo thư mục asset (lồng nhau) nếu chưa có.</summary>
        static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
