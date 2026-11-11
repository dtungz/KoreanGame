using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.EditorTools
{
    /// <summary>
    /// Soát chữ UI khi đang Play: chữ tràn khung (ngang / dọc), bị cắt, cỡ chữ quá nhỏ.
    /// Tools/KoreanGame/UI/Audit Text: soát mọi Text đang hiện. Stress(): thử một Text với nhiều câu (mọi ngôn ngữ) để bắt câu dài nhất.
    /// Đơn vị theo khung thiết kế 390 × 844.
    /// </summary>
    public static class UiTextAudit
    {
        public const int MinFont = 11;      // nhỏ hơn: khó đọc trên điện thoại
        const float Slack = 2f;

        [MenuItem("Tools/KoreanGame/UI/Audit Text (Play mode)")]
        static void AuditMenu()
        {
            if (!Application.isPlaying) { EditorUtility.DisplayDialog("Audit Text", "Vào Play rồi mở màn hình cần soát.", "OK"); return; }
            var issues = ScanAll();
            Debug.Log(issues.Count == 0 ? "[UI Audit] Không thấy chữ tràn / quá nhỏ." : "[UI Audit] " + issues.Count + " chỗ:\n" + string.Join("\n", issues));
        }

        public static List<string> ScanAll()
        {
            var issues = new List<string>();
            foreach (var t in Object.FindObjectsOfType<Text>())
            {
                if (!t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text) || t.color.a < .05f) continue;
                var cg = t.GetComponentInParent<CanvasGroup>();
                if (cg != null && cg.alpha < .05f) continue;
                var why = Check(t, t.text);
                if (why != null) issues.Add($"{Path(t)}: {why} «{Short(t.text)}»");
            }
            return issues;
        }

        // ======================= Soát scene Bento Journey ở chế độ Edit (không cần Play) =======================

        [MenuItem("Tools/KoreanGame/UI/Audit Bento Text (all languages)")]
        static void AuditSceneMenu()
        {
            var issues = AuditScene();
            Debug.Log(issues.Count == 0 ? "[UI Audit] Mọi chữ đều vừa khung ở cả 3 ngôn ngữ." : "[UI Audit] " + issues.Count + " chỗ:\n" + string.Join("\n", issues));
        }

        /// <summary>Thử mọi ô chữ với mọi câu có thể hiện ở đó (Việt / Hàn / Anh). Nhãn cố định: 3 bản dịch của key; ô động: tập câu tương ứng.</summary>
        public const string KoreanFontPath = "Assets/_Project/Source/Art/Fonts/Jua-Regular.ttf";

        public static List<string> AuditScene()
        {
            // từng ngôn ngữ một, với đúng font sẽ hiện (한국어: Jua)
            var kr = AssetDatabase.LoadAssetAtPath<Font>(KoreanFontPath);
            var all = new List<string>();
            foreach (Core.Lang l in System.Enum.GetValues(typeof(Core.Lang)))
            {
                Shared.LangFonts.ApplyWith(kr, l);
                all.AddRange(AuditScene(new[] { l }));
            }
            Shared.LangFonts.ApplyWith(kr, Core.Lang.Vi);
            var uniq = new List<string>(new HashSet<string>(all));
            uniq.Sort();
            return uniq;
        }

        static List<string> AuditScene(Core.Lang[] langs)
        {
            var issues = new List<string>();
            List<string> All(string key, params object[] args)
            {
                var l = new List<string>();
                foreach (var g in langs) l.Add(Core.Loc.TIn(g, key, args));
                return l;
            }
            List<string> Prefix(string prefix, params object[] args)
            {
                var l = new List<string>();
                var csv = AssetDatabase.LoadAssetAtPath<TextAsset>(Core.Loc.CsvAssetPath).text;
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(csv, @"^﻿?(" + System.Text.RegularExpressions.Regex.Escape(prefix) + @"[a-z0-9_.]*),", System.Text.RegularExpressions.RegexOptions.Multiline))
                    l.AddRange(All(m.Groups[1].Value, args));
                return l;
            }
            Text F(Object o, string field) => o == null ? null : new SerializedObject(o).FindProperty(field)?.objectReferenceValue as Text;
            Text[] FA(Object o, string field)
            {
                var p = o == null ? null : new SerializedObject(o).FindProperty(field);
                var l = new List<Text>();
                if (p != null && p.isArray) for (int i = 0; i < p.arraySize; i++) if (p.GetArrayElementAtIndex(i).objectReferenceValue is Text t) l.Add(t);
                return l.ToArray();
            }
            void S(Text t, IEnumerable<string> samples, string label) => issues.AddRange(Stress(t, samples, label));
            T Find<T>() where T : Object => Object.FindObjectOfType<T>(true);

            // nhãn cố định
            foreach (var lt in Object.FindObjectsOfType<Shared.LocText>(true))
                S(lt.GetComponent<Text>(), All(lt.Key), "nhãn " + lt.Key);

            var catalog = AssetDatabase.LoadAssetAtPath<Gameplay.FoodCatalog>(BentoJourneyBuilder.CatalogPath);
            var shop = AssetDatabase.LoadAssetAtPath<Gameplay.ShopCatalog>(BentoJourneyBuilder.ShopPath);
            var dishNames = new List<string>(); var intros = new List<string>(); var sideNames = new List<string>();
            foreach (var g in langs)
            {
                foreach (var d in catalog.dishes) { dishNames.Add(Core.Loc.TIn(g, $"dish.{d.id}.name")); intros.Add(Core.Loc.TIn(g, $"dish.{d.id}.intro")); }
                foreach (var s in catalog.sideDishes) sideNames.Add(Core.Loc.TIn(g, $"side.{s.id}.name"));
            }

            // HUD + tay bài
            S(F(Find<Gameplay.HudView>(), "levelText"), All("ui.level", 40), "HUD màn");
            S(F(Find<Gameplay.HandView>(), "queueText"), All("ui.queued_cards", 9), "thẻ chờ");
            foreach (var cv in Object.FindObjectsOfType<Gameplay.CardView>(true)) S(F(cv, "nameText"), Prefix("card."), "tên thẻ");
            foreach (var gv in Object.FindObjectsOfType<Home.GoldView>(true)) S(F(gv, "amount"), new[] { "9999" }, "vàng");
            var hint = Find<Gameplay.HintButton>();
            S(F(hint, "priceText"), new[] { "20" }, "giá gợi ý");

            // lời khách + tutorial
            var chat = Find<Gameplay.CustomerChat>();
            var lines = new List<string>(Prefix("order.", "Lát kimbap")); lines.AddRange(Prefix("thanks."));
            var cl = new SerializedObject(chat).FindProperty("lines").objectReferenceValue as Gameplay.CustomerLines;
            if (cl != null)
            {
                lines.AddRange(cl.chitChat); lines.AddRange(cl.foodTalk); lines.AddRange(cl.win);
                foreach (var t in cl.dishOrder) lines.Add(string.Format(t, "cơm nắm tam giác"));
                foreach (var t in cl.sideOrder) lines.Add(string.Format(t, "rau muống xào"));
                foreach (var t in cl.dishWin) lines.Add(string.Format(t, "Cơm nắm tam giác"));
            }
            S(F(chat, "speechText"), lines, "lời khách");
            var tut = Find<Gameplay.TutorialManager>();
            var tips = Prefix("tut."); tips.RemoveAll(s => s == Core.Loc.TIn(Core.Lang.Vi, "tut.skip") || s == Core.Loc.TIn(Core.Lang.Ko, "tut.skip") || s == Core.Loc.TIn(Core.Lang.En, "tut.skip"));
            S(F(tut, "tipVi"), tips, "tutorial");

            // popup
            var win = Find<Gameplay.WinPopup>();
            var winInfo = All("win.info_gold", 40, 55); winInfo.AddRange(All("win.info", 40));
            S(F(win, "infoText"), winInfo, "thắng: thông tin");
            var next = All("win.next"); next.AddRange(All("win.restart_all"));
            S(F(win, "nextLabel"), next, "thắng: nút");
            var dish = Find<Gameplay.DishPopup>();
            S(F(dish, "nameText"), dishNames, "món mới: tên");
            S(F(dish, "regionText"), Prefix("region."), "món mới: vùng");
            S(F(dish, "introText"), intros, "món mới: giới thiệu");
            S(F(Find<Gameplay.SideDishToast>(), "nameText"), sideNames, "món phụ mới");
            var set = Find<Home.SettingsPopup>();
            var snd = All("ui.sound_on"); snd.AddRange(All("ui.sound_off"));
            S(F(set, "soundLabel"), snd, "cài đặt: âm thanh");
            var vib = All("ui.vibration_on"); vib.AddRange(All("ui.vibration_off"));
            S(F(set, "vibrationLabel"), vib, "cài đặt: rung");
            var langLabels = new List<string>();
            foreach (var g in langs) foreach (var n in langs) langLabels.Add(Core.Loc.TIn(g, "ui.language_value", Core.Loc.TIn(g, "ui.lang." + n.ToString().ToLowerInvariant())));
            S(F(set, "languageLabel"), langLabels, "cài đặt: ngôn ngữ");

            // menu
            S(F(Find<Home.MainMenuView>(), "playLabel"), All("ui.level", 40), "menu: màn");
            var journey = Find<Home.JourneyView>();
            var dl = new List<string>(dishNames); dl.AddRange(All("ui.level", 40));
            foreach (var t in FA(journey, "dishLabels")) S(t, dl, "bộ sưu tập: tên món");
            var detail = new List<string>();
            foreach (var g in langs)
                foreach (var d in catalog.dishes)
                {
                    string region = Core.Loc.TIn(g, d.region == Gameplay.FoodRegion.Seoul ? "region.seoul" : "region.hanoi");
                    detail.Add($"<b>{Core.Loc.TIn(g, $"dish.{d.id}.name")}</b> · {region}\n{Core.Loc.TIn(g, $"dish.{d.id}.intro")}");
                    detail.Add($"<b>{Core.Loc.TIn(g, "journey.secret")}</b> · {region}\n{Core.Loc.TIn(g, "journey.unlock_at", 40)}");
                }
            foreach (var g in langs)
            {
                detail.Add($"<b>{Core.Loc.TIn(g, "journey.side_unknown")}</b>\n{Core.Loc.TIn(g, "journey.side_unknown_hint")}");
                detail.Add($"<b>{Core.Loc.TIn(g, "side.rau_muong.name")}</b> · {Core.Loc.TIn(g, "journey.side_region", Core.Loc.TIn(g, "region.hanoi"))}");
            }
            detail.AddRange(All("journey.tap_hint"));
            S(F(journey, "detailText"), detail, "bộ sưu tập: chi tiết");
            S(F(Find<Home.RouteBookView>(), "pageTitle"), All("journey.page", Core.Loc.TIn(Core.Lang.Vi, "region.hanoi"), 21, 40), "sổ: tiêu đề");
            var shopNames = new List<string>(); foreach (var it in shop.items) shopNames.AddRange(All($"shop.{it.id}"));
            var view = Find<Home.ShopView>();
            foreach (var t in FA(view, "itemNames")) S(t, shopNames, "shop: tên");
            var states = All("shop.using"); states.AddRange(All("shop.use")); states.AddRange(All("shop.unlock_at", 30)); states.Add("300");
            foreach (var t in FA(view, "itemStates")) S(t, states, "shop: trạng thái");

            // gộp trùng (nhiều thẻ / ô giống nhau)
            var uniq = new List<string>(new HashSet<string>(issues));
            uniq.Sort();
            return uniq;
        }

        /// <summary>Chụp toàn bộ UI (3 canvas overlay) ra PNG dọc ở chế độ Edit: tạm chuyển canvas sang Screen Space Camera rồi trả lại.</summary>
        public static void Shot(string file, int w = 780, int h = 1688)
        {
            var cam = Camera.main;
            var rt = new RenderTexture(w, h, 24);
            var canvases = new List<Canvas>();
            foreach (var c in Object.FindObjectsOfType<Canvas>()) if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay) canvases.Add(c);
            var handle = typeof(CanvasScaler).GetMethod("Handle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var old = cam.targetTexture;
            cam.targetTexture = rt;
            try
            {
                foreach (var c in canvases)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 5 - c.sortingOrder * .1f;
                    var cs = c.GetComponent<CanvasScaler>(); if (cs != null) handle.Invoke(cs, null);
                }
                Canvas.ForceUpdateCanvases();
                // như lúc chạy: neo thanh trên / khay thẻ theo màn hình w × h, thớt co vừa khoảng giữa
                // (DesignFrameFitter.Apply với màn hình 390 × 844, 1 pixel = 1 đơn vị thiết kế; tính lại ở đây vì canvas đang ở chế độ Camera)
                foreach (var f in Object.FindObjectsOfType<Shared.DesignFrameFitter>())
                {
                    var so = new SerializedObject(f);
                    var frames = so.FindProperty("frames");
                    for (int i = 0; i < frames.arraySize; i++)
                        if (frames.GetArrayElementAtIndex(i).objectReferenceValue is RectTransform fr) { fr.localScale = Vector3.one; fr.anchoredPosition = Vector2.zero; }
                    float topH = so.FindProperty("topHeight").floatValue, botH = so.FindProperty("bottomHeight").floatValue;
                    var center = so.FindProperty("boardCenter").vector2Value; var bsize = so.FindProperty("boardSize").vector2Value;
                    if (so.FindProperty("topFrame").objectReferenceValue is RectTransform top) { top.localScale = Vector3.one; top.anchoredPosition = new Vector2(0, 844 / 2f - topH / 2); }
                    if (so.FindProperty("bottomFrame").objectReferenceValue is RectTransform bot) { bot.localScale = Vector3.one; bot.anchoredPosition = new Vector2(0, -844 / 2f + botH / 2); }
                    if (bsize.x > 0 && bsize.y > 0)
                    {
                        float midTop = 844 - topH, midBottom = botH;
                        float k = Mathf.Min((midTop - midBottom) * .97f / bsize.y, 390 * .97f / bsize.x);
                        cam.orthographicSize = 844 / (2f * k);
                        var camPos = center - (new Vector2(195, (midTop + midBottom) / 2) - new Vector2(195, 422)) / k;
                        cam.transform.position = new Vector3(camPos.x, camPos.y, cam.transform.position.z);
                    }
                }
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
                RenderTexture.active = null;
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
                System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                foreach (var c in canvases)
                {
                    c.renderMode = RenderMode.ScreenSpaceOverlay;
                    var cs = c.GetComponent<CanvasScaler>(); if (cs != null) handle.Invoke(cs, null);
                }
                cam.targetTexture = old;
                Object.DestroyImmediate(rt);
            }
        }

        /// <summary>Điền mọi nhãn cố định (LocText) theo ngôn ngữ l, để chụp ảnh ở chế độ Edit.</summary>
        public static void FillLabels(Core.Lang l)
        {
            Shared.LangFonts.ApplyWith(AssetDatabase.LoadAssetAtPath<Font>(KoreanFontPath), l);
            foreach (var lt in Object.FindObjectsOfType<Shared.LocText>(true))
                lt.GetComponent<Text>().text = Core.Loc.TIn(l, lt.Key);
        }

        /// <summary>null nếu vừa; ngược lại mô tả lỗi.</summary>
        public static string Check(Text t, string text)
        {
            var size = t.rectTransform.rect.size;
            if (size.x < 1 || size.y < 1) return null;
            if (t.transform.parent != null && t.transform.parent.GetComponent<HorizontalOrVerticalLayoutGroup>() != null) return null;   // layout tự co giãn theo chữ
            var gen = new TextGenerator();
            var st = t.GetGenerationSettings(size);
            st.scaleFactor = 1;   // đo theo đơn vị khung thiết kế, không phụ thuộc cỡ Game view (canvas thu nhỏ làm font dựng ở vài pixel, sai số lớn)
            const float ppu = 1;
            int used = t.fontSize;
            if (t.resizeTextForBestFit)
            {
                gen.Populate(text, st);
                used = gen.fontSizeUsedForBestFit;
                if (used > 0) { st.resizeTextForBestFit = false; st.fontSize = used; }
                else used = t.fontSize;
            }
            var probs = new List<string>();
            if (used < MinFont) probs.Add($"cỡ {used} < {MinFont}");
            if (t.horizontalOverflow == HorizontalWrapMode.Overflow)
            {
                var one = st; one.horizontalOverflow = HorizontalWrapMode.Overflow;
                float w = gen.GetPreferredWidth(text, one) / ppu;
                if (w > size.x + Slack) probs.Add($"tràn ngang {w:0}>{size.x:0}");
            }
            var wrap = st; wrap.verticalOverflow = VerticalWrapMode.Overflow;
            float h = gen.GetPreferredHeight(text, wrap) / ppu;
            bool freeOverflow = t.verticalOverflow == VerticalWrapMode.Overflow && h <= size.y * 1.25f;
            if (h > size.y + Slack && !freeOverflow) probs.Add($"{(t.verticalOverflow == VerticalWrapMode.Truncate ? "bị cắt" : "tràn dọc")} {h:0}>{size.y:0}");
            return probs.Count == 0 ? null : string.Join(", ", probs);
        }

        /// <summary>Thử Text với từng câu; trả về các câu không vừa (giữ nguyên chữ cũ sau khi thử).</summary>
        public static List<string> Stress(Text t, IEnumerable<string> samples, string label)
        {
            var bad = new List<string>();
            if (t == null) return bad;
            foreach (var s in samples)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                var why = Check(t, s);
                if (why != null) bad.Add($"{label} [{Path(t)}]: {why} «{Short(s)}»");
            }
            return bad;
        }

        public static string Path(Component c)
        {
            var t = c.transform;
            string p = t.name;
            for (int i = 0; i < 3 && t.parent != null; i++) { t = t.parent; p = t.name + "/" + p; }
            return p;
        }

        static string Short(string s)
        {
            s = s.Replace("\n", " / ");
            return s.Length > 60 ? s.Substring(0, 57) + "…" : s;
        }
    }
}
