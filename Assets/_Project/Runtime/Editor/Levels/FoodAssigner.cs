using System.Collections.Generic;
using System.Text;
using KoreanGame.Gameplay;
using UnityEditor;
using UnityEngine;

namespace KoreanGame.EditorTools
{
    /// <summary>
    /// Gán món cho 40 level theo bảng đã duyệt (giai đoạn 2): vùng, món đặc trưng mở khoá, ngăn món phụ.
    /// Ngăn món phụ = 1–2 ô gắn vào cạnh phải / trái của hộp, trừ khỏi phần cần lấp cơm nên lời giải không đổi.
    /// Chạy lại nhiều lần được: ngăn do tool thêm (id món phụ mới) được gỡ ra rồi đặt lại; món có sẵn (egg, sausage, meat) giữ nguyên.
    /// Món phụ chưa có ảnh trong FoodCatalog thì chưa đặt (tránh hiện ảnh trứng thay cho kimchi).
    /// </summary>
    public static class FoodAssigner
    {
        const string Menu = "Tools/KoreanGame/Levels/Assign Foods/";
        public const string DishFolder = "Assets/_Project/Data/Dishes";
        public const string DishArtFolder = "Assets/_Project/Source/Art/InGame/Dishes";

        // ---- món phụ mới: id, tên, vùng, số ô (dọc) ----
        static readonly (string id, string name, FoodRegion region, int cells)[] Sides =
        {
            ("dua_gop", "Dưa góp", FoodRegion.HaNoi, 1),
            ("rau_muong", "Rau muống xào", FoodRegion.HaNoi, 1),
            ("dau_sot_ca", "Đậu sốt cà chua", FoodRegion.HaNoi, 1),
            ("gio_lua", "Giò lụa", FoodRegion.HaNoi, 1),
            ("cha", "Chả nướng", FoodRegion.HaNoi, 1),
            ("nem", "Nem rán", FoodRegion.HaNoi, 2),
            ("kimchi", "Kimchi", FoodRegion.Seoul, 1),
            ("gia_tron", "Giá trộn", FoodRegion.Seoul, 1),
            ("trung_cuon", "Trứng cuộn", FoodRegion.Seoul, 2),
            ("rong_bien", "Rong biển", FoodRegion.Seoul, 1),
            ("com_nam", "Cơm nắm", FoodRegion.Seoul, 1),
            ("lat_kimbap", "Lát kimbap", FoodRegion.Seoul, 1),
        };

        // ---- món đặc trưng: id, tên, vùng, lời giới thiệu ----
        static readonly (string id, string name, FoodRegion region, string intro)[] Dishes =
        {
            ("bun_cha", "Bún chả", FoodRegion.HaNoi, "Chả nướng than hoa thơm lừng, ăn cùng bún và nước chấm chua ngọt. Món trưa quen thuộc của người Hà Nội."),
            ("pho", "Phở", FoodRegion.HaNoi, "Bánh phở mềm, nước dùng ninh xương trong veo. Một bát phở nóng là cả buổi sáng Hà Nội."),
            ("nem_ran", "Nem rán", FoodRegion.HaNoi, "Cuốn nhân thịt, miến, mộc nhĩ rồi rán vàng giòn. Mâm cơm ngày Tết không thể thiếu."),
            ("banh_mi", "Bánh mì", FoodRegion.HaNoi, "Ổ bánh giòn rụm kẹp pate, chả, rau thơm. Món ăn đường phố nổi tiếng khắp thế giới."),
            ("com_nam_samgak", "Cơm nắm tam giác", FoodRegion.Seoul, "Samgak kimbap: cơm nắm hình tam giác bọc rong biển, món ăn nhanh của dân văn phòng Seoul."),
            ("kimbap", "Kimbap", FoodRegion.Seoul, "Cơm cuộn rong biển với trứng, rau, củ cải vàng, cắt thành từng lát tròn xinh xắn."),
            ("bibimbap", "Bibimbap", FoodRegion.Seoul, "Cơm trộn với rau, thịt, trứng và tương ớt gochujang. Trộn đều rồi thưởng thức!"),
            ("tteokbokki", "Tteokbokki", FoodRegion.Seoul, "Bánh gạo dẻo sốt cay ngọt đỏ au, món ăn vặt được yêu thích nhất ở Seoul."),
        };

        // ---- bảng theo level (index 0) : món đặc trưng, các món phụ mới ----
        static readonly Dictionary<int, (string dish, string[] sides)> Plan = new Dictionary<int, (string, string[])>
        {
            { 4, ("bun_cha", null) }, { 6, (null, new[] { "dua_gop" }) },
            { 9, ("pho", null) }, { 10, (null, new[] { "rau_muong" }) }, { 11, (null, new[] { "dau_sot_ca" }) },
            { 12, (null, new[] { "gio_lua" }) }, { 13, (null, new[] { "cha" }) },
            { 14, ("nem_ran", null) }, { 15, (null, new[] { "nem" }) }, { 16, (null, new[] { "dua_gop" }) },
            { 17, (null, new[] { "rau_muong" }) }, { 18, (null, new[] { "cha", "gio_lua" }) },
            { 19, ("banh_mi", null) },
            { 20, (null, new[] { "kimchi" }) }, { 21, (null, new[] { "kimchi" }) }, { 22, (null, new[] { "gia_tron" }) },
            { 23, (null, new[] { "trung_cuon" }) }, { 24, ("com_nam_samgak", null) },
            { 25, (null, new[] { "kimchi" }) }, { 26, (null, new[] { "trung_cuon", "kimchi" }) },
            { 27, (null, new[] { "rong_bien" }) }, { 28, (null, new[] { "kimchi" }) }, { 29, ("kimbap", null) },
            { 30, (null, new[] { "lat_kimbap", "gia_tron" }) }, { 31, (null, new[] { "kimchi" }) },
            { 32, (null, new[] { "com_nam" }) }, { 33, (null, new[] { "rong_bien" }) }, { 34, ("bibimbap", null) },
            { 35, (null, new[] { "trung_cuon" }) }, { 36, (null, new[] { "kimchi", "gia_tron" }) },
            { 37, (null, new[] { "lat_kimbap" }) }, { 38, (null, new[] { "com_nam", "kimchi" }) },
            { 39, ("tteokbokki", null) },
        };

        public static FoodRegion RegionOf(int index) => index < 20 ? FoodRegion.HaNoi : FoodRegion.Seoul;

        static bool IsToolSide(string kind) => System.Array.Exists(Sides, s => s.id == kind);
        static int CellsOf(string kind) => System.Array.Find(Sides, s => s.id == kind).cells;

        [MenuItem(Menu + "Preview")]
        public static void Preview() => Debug.Log(Run(false));

        [MenuItem(Menu + "Apply")]
        public static void ApplyMenu()
        {
            if (!EditorUtility.DisplayDialog("Assign Foods", "Ghi vùng, món đặc trưng và ngăn món phụ vào 40 level?", "Ghi", "Huỷ")) return;
            Debug.Log(Run(true));
        }

        /// <summary>Chạy bảng gán món. apply = false chỉ báo cáo (không ghi gì).</summary>
        public static string Run(bool apply)
        {
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelImporter.DatabasePath);
            var cat = BentoJourneyBuilder.LoadOrCreateCatalog();
            var dishes = apply ? EnsureDishes(cat) : null;
            if (apply) EnsureSides(cat);

            var sb = new StringBuilder($"[Assign Foods] {(apply ? "ÁP DỤNG" : "XEM TRƯỚC")}\n");
            int placed = 0, waiting = 0, failed = 0;
            for (int i = 0; i < db.Count; i++)
            {
                var L = db.levels[i];
                Plan.TryGetValue(i, out var p);
                var line = new StringBuilder($"L{i + 1} {(RegionOf(i) == FoodRegion.HaNoi ? "Hà Nội" : "Seoul")}");
                if (p.dish != null) line.Append($" ★{p.dish}");
                foreach (var f in L.tray.fixedFoods) if (!IsToolSide(f.kind)) line.Append($" [có sẵn {f.kind}]");

                // làm trên bản sao: gỡ ngăn cũ của tool, đặt ngăn mới, so lời giải
                var work = Object.Instantiate(L);
                RemoveToolSides(work);
                var before = LevelSolver.Solve(work, all: true, maxNodes: 60000);
                var ready = new List<string>();
                if (p.sides != null)
                    foreach (var id in p.sides)
                    {
                        if (cat.SideSprite(id) == null) { line.Append($" +{id}(chờ art)"); waiting++; continue; }
                        if (Place(work, id)) { ready.Add(id); line.Append($" +{id}"); placed++; }
                        else { line.Append($" +{id}(KHÔNG CÓ CHỖ)"); failed++; }
                    }
                var after = ready.Count > 0 ? LevelSolver.Solve(work, all: true, maxNodes: 60000) : before;
                bool same = after.solved == before.solved && after.solutions == before.solutions;
                if (!same) { line.Append($" LỜI GIẢI ĐỔI {before.solutions}→{after.solutions}, bỏ qua món phụ"); failed++; }

                if (apply)
                {
                    L.region = RegionOf(i);
                    L.unlocksDish = p.dish != null ? dishes[p.dish] : null;
                    if (same)
                    {
                        L.tray = work.tray;
                        work.tray = new TrayData();
                    }
                    else RemoveToolSides(L);
                    EditorUtility.SetDirty(L);
                }
                Object.DestroyImmediate(work);
                sb.AppendLine(line.ToString());
            }
            if (apply) AssetDatabase.SaveAssets();
            sb.Insert(0, $"đặt {placed} ngăn, {waiting} chờ art, {failed} lỗi\n");
            return sb.ToString();
        }

        static void RemoveToolSides(LevelData L)
        {
            var drop = new HashSet<Vector2Int>();
            L.tray.fixedFoods.RemoveAll(f =>
            {
                if (!IsToolSide(f.kind)) return false;
                foreach (var c in f.cells) drop.Add(c);
                return true;
            });
            L.tray.tris.RemoveAll(q => drop.Contains(q.Cell));
        }

        /// <summary>Gắn ngăn món phụ (1–2 ô dọc) vào cạnh phải, không được thì cạnh trái, cạnh ô đầy của hộp, trong thớt.</summary>
        static bool Place(LevelData L, string id)
        {
            int n = CellsOf(id);
            var at = L.tray.at;
            var cells = new Dictionary<Vector2Int, int>();
            foreach (var q in L.tray.tris) cells[q.Cell] = cells.TryGetValue(q.Cell, out var c) ? c + 1 : 1;
            var list = new List<Vector2Int>(cells.Keys);
            list.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            bool Free(Vector2Int c)
            {
                var w = c + at;
                return !cells.ContainsKey(c) && w.x >= 0 && w.x < Geometry.Cols && w.y >= 0 && w.y < Geometry.Rows;
            }
            bool Full(Vector2Int c) => cells.TryGetValue(c, out var k) && k == 4;

            foreach (int dx in new[] { 1, -1 })
                foreach (var c in list)
                {
                    var spot = new List<Vector2Int>();
                    for (int k = 0; k < n; k++)
                    {
                        var host = new Vector2Int(c.x, c.y + k);
                        var s = new Vector2Int(c.x + dx, c.y + k);
                        if (!Full(host) || !Free(s)) { spot = null; break; }
                        spot.Add(s);
                    }
                    if (spot == null) continue;
                    L.tray.tris.AddRange(Geometry.Full(spot));
                    L.tray.fixedFoods.Add(new FixedFood { kind = id, cells = spot });
                    return true;
                }
            return false;
        }

        static Dictionary<string, DishData> EnsureDishes(FoodCatalog cat)
        {
            if (!AssetDatabase.IsValidFolder(DishFolder)) AssetDatabase.CreateFolder("Assets/_Project/Data", "Dishes");
            var map = new Dictionary<string, DishData>();
            foreach (var d in Dishes)
            {
                var path = $"{DishFolder}/Dish_{d.id}.asset";
                var a = AssetDatabase.LoadAssetAtPath<DishData>(path);
                if (a == null)
                {
                    a = ScriptableObject.CreateInstance<DishData>();
                    a.id = d.id; a.displayName = d.name; a.region = d.region; a.intro = d.intro;
                    AssetDatabase.CreateAsset(a, path);
                }
                // ảnh: Source/Art/InGame/Dishes/dish_<id>.png (lấp đúng khuôn màn mở khoá) + icon_<id>.png; không ghi đè ảnh đã gán
                if (a.art == null) a.art = AssetDatabase.LoadAssetAtPath<Sprite>($"{DishArtFolder}/dish_{d.id}.png");
                if (a.icon == null) a.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{DishArtFolder}/icon_{d.id}.png");
                EditorUtility.SetDirty(a);
                map[d.id] = a;
                if (!cat.dishes.Contains(a)) cat.dishes.Add(a);
            }
            EditorUtility.SetDirty(cat);
            return map;
        }

        static void EnsureSides(FoodCatalog cat)
        {
            foreach (var s in Sides)
            {
                var side = cat.FindSide(s.id);
                if (side == null) cat.sideDishes.Add(side = new SideDish { id = s.id, displayName = s.name, region = s.region });
                // ảnh: Source/Art/InGame/Gameplay/food_<id>.png (rau muống dùng lại food_greens có sẵn); không ghi đè ảnh bạn đã gán
                if (side.sprite == null)
                    side.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                        $"Assets/_Project/Source/Art/InGame/Gameplay/food_{(s.id == "rau_muong" ? "greens" : s.id)}.png");
            }
            EditorUtility.SetDirty(cat);
        }
    }
}
