using KoreanGame.App;
using KoreanGame.Core;
using KoreanGame.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KoreanGame.EditorTools
{
    /// <summary>
    /// Dựng scene Main từ đầu theo bố cục 390×844 của prototype play.html (1 ô = 44 px, thớt 7×10).
    /// Chạy lại bất cứ lúc nào: KoreanGame/Build Main Scene.
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        const string Art = "Assets/_Project/Source/Art/";
        const string MatFolder = "Assets/_Project/Source/Effect/Materials/";
        const float RefW = 390, RefH = 844, Cell = 44;

        static Font font;
        static Sprite S(string rel) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + rel + ".png");
        static Color C(string hex) => Palette.Hex(hex);

        /// <summary>Các object chính BuildContent tạo ra, để tool khác (BentoJourneyBuilder) dựng tiếp.</summary>
        public class Built
        {
            public Camera cam;
            public Canvas hud, popup;
            public GameObject ui;          // nhóm "---- GIAO DIỆN ----"
            public GameBootstrap boot;
        }

        [MenuItem("Tools/KoreanGame/Build Main Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildContent();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("[SceneBuilder] Đã dựng " + ScenePath);
        }

        /// <summary>Dựng toàn bộ object của màn chơi vào scene đang mở. Không lưu scene, không đổi Build Settings.</summary>
        public static Built BuildContent()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var colorMat = Material("Mat_Color", null);
            var riceMat = Material("Mat_Rice", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "InGame/Gameplay/rice_texture.png"));

            // ---------- [Managers] ----------
            var managers = new GameObject("[Managers]");
            var bootGo = Child(managers, "GameBootstrap");
            var boot = bootGo.AddComponent<GameBootstrap>();
            var controller = Child(managers, "Manager").AddComponent<LevelController>();
            var input = controller.gameObject.AddComponent<BoardInput>();
            var feedback = controller.gameObject.AddComponent<GameFeedback>();
            var audioMgr = BuildAudio(managers);

            // ---------- ĐIỀU KHIỂN ----------
            var ctl = new GameObject("---- ĐIỀU KHIỂN ----");
            var camGo = Child(ctl, "Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();   // cần 1 AudioListener trong scene để nghe được âm
            cam.orthographic = true;
            cam.orthographicSize = RefH / Cell / 2;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = C("#F3E9DA");
            camGo.transform.position = new Vector3(0, 0, -10);
            var es = Child(ctl, "EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            // ---------- MÀN CHƠI ----------
            var play = new GameObject("---- MÀN CHƠI ----");
            // Không còn banner: dồn thớt + tay bài xuống, chừa ~60 px dưới cùng cho thanh home của điện thoại
            const float BX = 29, BY = 172, OPX = 12, BW = 7 * Cell + 2 * OPX, BH = 10 * Cell + 2 * OPX;
            var bg = Child(play, "BoardBg").AddComponent<SpriteRenderer>();
            bg.sprite = S("InGame/Gameplay/board");
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(BW / Cell, BH / Cell);
            bg.transform.position = PxToWorld(BX + BW / 2, BY + BH / 2);
            bg.sortingOrder = -20;

            var boardGo = Child(play, "Board");
            boardGo.transform.position = PxToWorld(BX + OPX, BY + OPX);
            var board = boardGo.AddComponent<BoardView>();
            var grid = MeshChild(boardGo, "Grid", colorMat, -10);
            var content = Child(boardGo, "Content");   // khay + cơm + hiệu ứng, nảy khi thắng
            var trayGo = MeshChild(content, "Tray", colorMat, 0);
            var tray = trayGo.AddComponent<TrayView>();
            var food = Child(trayGo, "FoodTemplate").AddComponent<SpriteRenderer>();
            Set(tray, "foodTemplate", food);
            Set(tray, "egg", S("InGame/Gameplay/food_egg"));
            Set(tray, "sausage", S("InGame/Gameplay/food_sausage"));
            Set(tray, "meat", S("InGame/Gameplay/food_pork_belly"));
            AutoFill(tray);
            var pieceGo = MeshChild(content, "PieceTemplate", colorMat, 10);
            var piece = pieceGo.AddComponent<PieceView>();
            AutoFill(piece);
            var shadowGo = MeshChild(pieceGo, "Shadow", colorMat, 9);
            Set(piece, "shadowFilter", shadowGo.GetComponent<MeshFilter>());
            Set(piece, "shadowRenderer", shadowGo.GetComponent<MeshRenderer>());
            var previewGo = MeshChild(content, "Preview", colorMat, 20);
            var slashGo = MeshChild(content, "Slash", colorMat, 40);
            var flashGo = MeshChild(content, "Flash", colorMat, 38);
            var lidGo = MeshChild(content, "Lid", colorMat, 50);
            var burst = MakeRiceBurst(content, colorMat);
            Set(board, "cam", cam);
            Set(board, "gridMesh", grid.GetComponent<MeshFilter>());
            Set(board, "trayView", tray);
            Set(board, "pieceTemplate", piece);
            Set(board, "previewMesh", previewGo.GetComponent<MeshFilter>());
            Set(board, "previewRenderer", previewGo.GetComponent<MeshRenderer>());
            Set(board, "slashMesh", slashGo.GetComponent<MeshFilter>());
            Set(board, "slashRenderer", slashGo.GetComponent<MeshRenderer>());
            Set(board, "content", content.transform);
            Set(board, "flashMesh", flashGo.GetComponent<MeshFilter>());
            Set(board, "flashRenderer", flashGo.GetComponent<MeshRenderer>());
            Set(board, "lidMesh", lidGo.GetComponent<MeshFilter>());
            Set(board, "lidRenderer", lidGo.GetComponent<MeshRenderer>());
            Set(board, "riceBurst", burst);
            Set(board, "colorMaterial", colorMat);
            Set(board, "riceMaterial", riceMat);

            // ---------- GIAO DIỆN ----------
            var ui = new GameObject("---- GIAO DIỆN ----");
            var hudCanvas = MakeCanvas(ui, "Canvas_HUD", 0);
            var hudRoot = hudCanvas.transform;
            Panel(hudRoot, "TopBg", 0, 0, 390, 132, C("#E8D8C4"), false);
            Img(hudRoot, "Mascot", S("UI/Common/mascot_egg"), 296, 58, 84, 60, false);
            Img(hudRoot, "Shelf", S("UI/Gameplay/shelf"), -6, 112, 402, 46, false);
            Img(hudRoot, "Plaque", S("UI/Gameplay/plaque_tab"), 110, 21, 170, 90, false);
            var lvText = Label(hudRoot, "LevelText", "Level 1", 195, 60, 200, 30, 17, C("#FFF6E6"), FontStyle.Bold, C("#96623C"));
            var cardsText = Label(hudRoot, "CardsLeft", "×1", 195, 90, 120, 34, 22, C("#FFF6E6"), FontStyle.Bold, C("#96623C"));
            var restartBtn = Btn(hudRoot, "RestartButton", S("UI/Common/btn_restart"), 16, 26, 52, 55);


            const float HY = BY + BH + 30, HandH = 116;
            var undoBtn = Btn(hudRoot, "UndoButton", S("UI/Common/btn_undo"), BX, HY + (HandH - 52) / 2, 52, 56);
            var handGo = Panel(hudRoot, "Hand", BX + 60, HY, BW - 60, HandH, C("#E2D4C2"), false);
            var handView = handGo.AddComponent<HandView>();
            var cardsArr = new CardView[3];
            var emptyArr = new GameObject[3];
            float hw = BW - 60, gap = 8, cw = 84, ch = 104, x0 = (hw - (3 * cw + 2 * gap)) / 2;
            for (int k = 0; k < 3; k++)
            {
                float cx = x0 + k * (cw + gap), cy = (HandH - ch) / 2;
                var empty = Panel(handGo.transform, $"Empty{k}", cx, cy, cw, ch, new Color(.48f, .38f, .32f, .12f), false);
                emptyArr[k] = empty;
                cardsArr[k] = MakeCard(handGo.transform, k, cx, cy, cw, ch);
            }
            var queue = Panel(hudRoot, "QueueBadge", BX + BW - 6 - 84, HY - 10, 84, 20, C("#8E5E3E"), false);
            var queueText = Label(queue.transform, "Text", "+1 thẻ chờ", 42, 10, 84, 20, 11, C("#FFF6E6"), FontStyle.Bold, null);
            Set(handView, "cards", cardsArr);
            Set(handView, "emptySlots", emptyArr);
            Set(handView, "queueBadge", queue);
            Set(handView, "queueText", queueText);
            Set(handView, "canvas", hudCanvas);

            // bóng thẻ bay theo ngón tay khi kéo (lớp phủ kín canvas, nằm trên cùng HUD)
            var ghostLayer = new GameObject("GhostLayer", typeof(RectTransform));
            ghostLayer.transform.SetParent(hudCanvas.transform, false);
            Stretch((RectTransform)ghostLayer.transform);
            var ghostRoot = new GameObject("Ghost", typeof(RectTransform));
            ghostRoot.transform.SetParent(ghostLayer.transform, false);
            var gr = (RectTransform)ghostRoot.transform;
            gr.anchorMin = gr.anchorMax = gr.pivot = new Vector2(.5f, .5f);
            gr.sizeDelta = new Vector2(84, 104);
            var ghostCard = MakeCard(ghostRoot.transform, 9, 0, 0, 84, 104);
            ghostCard.name = "GhostCard";
            ghostRoot.SetActive(false);
            Set(handView, "ghostLayer", (RectTransform)ghostLayer.transform);
            Set(handView, "ghostRoot", gr);
            Set(handView, "ghost", ghostCard);


            var hud = hudCanvas.gameObject.AddComponent<HudView>();
            Set(hud, "levelText", lvText);
            Set(hud, "cardsLeftText", cardsText);
            Set(hud, "restartButton", restartBtn);
            Set(hud, "undoButton", undoBtn);

            // Popup thắng
            var popCanvas = MakeCanvas(ui, "Canvas_Popup", 20);
            var win = popCanvas.gameObject.AddComponent<WinPopup>();
            // Lớp tối phủ KÍN màn hình (không chỉ khung 390×844), popup căn giữa màn hình
            var winRoot = new GameObject("WinPopup", typeof(RectTransform));
            winRoot.transform.SetParent(popCanvas.transform, false);
            Stretch((RectTransform)winRoot.transform);
            var dim = winRoot.AddComponent<Image>();
            dim.color = new Color(40 / 255f, 25 / 255f, 18 / 255f, .32f);
            var winGroup = winRoot.AddComponent<CanvasGroup>();
            var winFrame = new GameObject("Frame", typeof(RectTransform));
            winFrame.transform.SetParent(winRoot.transform, false);
            var wf = (RectTransform)winFrame.transform;
            wf.anchorMin = wf.anchorMax = wf.pivot = new Vector2(.5f, .5f);
            wf.sizeDelta = new Vector2(RefW, RefH);
            const float PopW = 300, PopH = 270;
            var pop = Panel(winFrame.transform, "Panel", (RefW - PopW) / 2, (RefH - PopH) / 2, PopW, PopH, C("#FDF4E8"), true);
            // tâm phóng to ở giữa popup (không phải góc trên-trái) để hiệu ứng hiện lên không bị lệch
            var popRt = (RectTransform)pop.transform;
            popRt.anchorMin = popRt.anchorMax = popRt.pivot = new Vector2(.5f, .5f);
            popRt.anchoredPosition = Vector2.zero;
            pop.GetComponent<Image>().sprite = S("UI/Common/popup");
            pop.GetComponent<Image>().type = Image.Type.Sliced;
            pop.GetComponent<Image>().pixelsPerUnitMultiplier = 2;
            pop.GetComponent<Image>().color = Color.white;
            Img(pop.transform, "Mascot", S("UI/Common/mascot_egg"), 105, 16, 90, 64, false);
            Label(pop.transform, "Title", "Hoàn thành!", 150, 120, 280, 40, 28, C("#7A6252"), FontStyle.Bold, null);
            var winInfo = Label(pop.transform, "Info", "Level 1 xong.", 150, 152, 280, 24, 14, C("#7A6252"), FontStyle.Normal, null);
            var nextBtn = Btn(pop.transform, "NextButton", S("UI/Common/btn_pill_green"), 70, 182, 160, 54);
            nextBtn.GetComponent<Image>().type = Image.Type.Sliced;
            nextBtn.GetComponent<Image>().pixelsPerUnitMultiplier = 2.4f;
            var nextLbl = Label(nextBtn.transform, "Label", "Tiếp tục", 80, 25, 160, 40, 19, C("#FFF6E6"), FontStyle.Bold, C("#5E8A3A"));
            Set(win, "root", winRoot);
            Set(win, "group", winGroup);
            Set(win, "panel", (RectTransform)pop.transform);
            Set(win, "infoText", winInfo);
            Set(win, "nextLabel", nextLbl);
            Set(win, "nextButton", nextBtn);

            // ---------- Bootstrap ----------
            Set(boot, "levelDatabase", AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelImporter.DatabasePath));
            Set(boot, "levelController", controller);
            Set(boot, "boardInput", input);
            Set(boot, "boardView", board);
            Set(boot, "handView", handView);
            Set(boot, "hudView", hud);
            Set(boot, "winPopup", win);
            Set(boot, "audioManager", audioMgr);
            Set(boot, "gameFeedback", feedback);

            return new Built { cam = cam, hud = hudCanvas, popup = popCanvas, ui = ui, boot = boot };
        }

        // ================= Thẻ =================
        static CardView MakeCard(Transform parent, int k, float x, float y, float w, float h)
        {
            var go = new GameObject($"Card{k}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, w, h);
            var view = go.AddComponent<CardView>();

            var body = new GameObject("Body", typeof(RectTransform), typeof(CanvasGroup));
            body.transform.SetParent(go.transform, false);
            Stretch((RectTransform)body.transform);
            var group = body.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            var frame = body.AddComponent<Image>();
            frame.sprite = S("UI/Gameplay/card_line");
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 2;
            frame.raycastTarget = false;

            var artGo = new GameObject("Art", typeof(RectTransform), typeof(CanvasRenderer));
            artGo.transform.SetParent(body.transform, false);
            Place((RectTransform)artGo.transform, 12, 10, 60, 60);
            var art = artGo.AddComponent<CardArtGraphic>();
            art.raycastTarget = false;

            var icon = Img(body.transform, "Icon", null, 18, 14, 48, 48, false);
            icon.preserveAspect = true;
            var name = Label(body.transform, "Name", "Thẳng 1", w / 2, h - 18, w, 18, 11, C("#7A6252"), FontStyle.Bold, null);

            var badge = Panel(body.transform, "RotBadge", w - 22, 4, 18, 18, Color.white, false);
            Label(badge.transform, "Glyph", "↻", 9, 9, 18, 18, 11, C("#8A6A55"), FontStyle.Bold, null);

            Set(view, "body", (RectTransform)body.transform);
            Set(view, "group", group);
            Set(view, "frame", frame);
            Set(view, "art", art);
            Set(view, "icon", icon);
            Set(view, "nameText", name);
            Set(view, "rotBadge", badge);
            Set(view, "lineFrame", S("UI/Gameplay/card_line"));
            Set(view, "moldFrame", S("UI/Gameplay/card_mold"));
            Set(view, "diagFrame", S("UI/Gameplay/card_diag"));
            Set(view, "fnFrame", S("UI/Gameplay/card_fn"));
            return view;
        }

        // ================= Âm thanh / hạt =================
        static AudioManager BuildAudio(GameObject managers)
        {
            var go = Child(managers, "AudioManager");
            var mgr = go.AddComponent<AudioManager>();
            var sources = new AudioSource[6];
            for (int i = 0; i < sources.Length; i++)
            {
                var src = Child(go, "Sfx_" + i).AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0;
                sources[i] = src;
            }
            var names = new[] { "cut", "tick", "drop", "bad", "rotate", "pop", "lid", "win" };   // theo thứ tự enum Sfx
            var clips = new AudioClip[names.Length];
            for (int i = 0; i < names.Length; i++)
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Project/Source/Audio/Gameplay_SFX/sfx_{names[i]}.wav");
            Set(mgr, "sources", sources);
            Set(mgr, "clips", clips);
            return mgr;
        }

        /// <summary>Hạt cơm bắn khi cắt: hạt nhỏ màu kem, rơi nhẹ, phát thủ công (Emit).</summary>
        static ParticleSystem MakeRiceBurst(GameObject parent, Material mat)
        {
            var go = Child(parent, "RiceBurst");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.25f, .45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(.06f, .11f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1, .99f, .95f), new Color(.93f, .89f, .8f));
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var em = ps.emission;
            em.enabled = false;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = .05f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .6f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.sortingOrder = 41;
            return ps;
        }

        // ================= Helpers =================
        static Vector3 PxToWorld(float px, float py) => new Vector3((px - RefW / 2) / Cell, (RefH / 2 - py) / Cell, 0);

        static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static GameObject MeshChild(GameObject parent, string name, Material mat, int order)
        {
            var go = Child(parent, name);
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.sortingOrder = order;
            return go;
        }

        static Material Material(string name, Texture2D tex)
        {
            var path = MatFolder + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.mainTexture = tex;
            EditorUtility.SetDirty(m);
            return m;
        }

        internal static Canvas MakeCanvas(GameObject parent, string name, int order)
        {
            var go = Child(parent, name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var sc = go.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(RefW, RefH);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight = 1;
            go.AddComponent<GraphicRaycaster>();
            // khung 390×844 căn giữa, toạ độ tính từ góc trên-trái
            var frame = new GameObject("Frame", typeof(RectTransform));
            frame.transform.SetParent(go.transform, false);
            var rt = (RectTransform)frame.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = new Vector2(RefW, RefH);
            // Canvas trả về để thêm con; con thật sự gắn vào Frame
            frame.name = "Frame";
            return c;
        }

        static Transform FrameOf(Transform t)
        {
            var c = t.GetComponent<Canvas>();
            return c != null ? t.Find("Frame") : t;
        }

        static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        internal static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static GameObject Panel(Transform parent, string name, float x, float y, float w, float h, Color col, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(FrameOf(parent), false);
            Place((RectTransform)go.transform, x, y, w, h);
            var img = go.AddComponent<Image>();
            img.color = col;
            img.raycastTarget = raycast;
            return go;
        }

        internal static Image Img(Transform parent, string name, Sprite s, float x, float y, float w, float h, bool raycast)
        {
            var go = Panel(parent, name, x, y, w, h, Color.white, raycast);
            var img = go.GetComponent<Image>();
            img.sprite = s;
            return img;
        }

        internal static Button Btn(Transform parent, string name, Sprite s, float x, float y, float w, float h)
        {
            var img = Img(parent, name, s, x, y, w, h, true);
            img.preserveAspect = true;
            var b = img.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.ColorTint;
            return b;
        }


        /// <summary>Chữ căn giữa tại (cx, cy) theo toạ độ của cha.</summary>
        internal static Text Label(Transform parent, string name, string text, float cx, float cy, float w, float h, int size, Color col, FontStyle style, Color? outline)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(FrameOf(parent), false);
            Place((RectTransform)go.transform, cx - w / 2, cy - h / 2, w, h);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = col;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (outline.HasValue)
            {
                var o = go.AddComponent<Outline>();
                o.effectColor = outline.Value;
                o.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return t;
        }


        internal static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[SceneBuilder] {target.GetType().Name} không có field '{field}'"); return; }
            if (p.isArray && value is System.Array arr)
            {
                p.arraySize = arr.Length;
                for (int i = 0; i < arr.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = (Object)arr.GetValue(i);
            }
            else p.objectReferenceValue = value is GameObject g ? g : (Object)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AutoFill(Component c)
        {
            var so = new SerializedObject(c);
            var mf = so.FindProperty("meshFilter");
            var mr = so.FindProperty("meshRenderer");
            if (mf != null) mf.objectReferenceValue = c.GetComponent<MeshFilter>();
            if (mr != null) mr.objectReferenceValue = c.GetComponent<MeshRenderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
