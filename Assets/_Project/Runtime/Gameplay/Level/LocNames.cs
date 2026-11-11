using KoreanGame.Core;

namespace KoreanGame.Gameplay
{
    /// <summary>Tên hiển thị của dữ liệu game theo ngôn ngữ đang chọn (bảng dịch: dish.*, side.*, shop.*, region.*). Thiếu bản dịch thì dùng chữ gốc trong asset.</summary>
    public static class LocNames
    {
        public static string Name(this DishData d) => d == null ? "" : Loc.Or($"dish.{d.id}.name", d.displayName);
        public static string Intro(this DishData d) => d == null ? "" : Loc.Or($"dish.{d.id}.intro", d.intro);
        public static string Name(this SideDish s) => s == null ? "" : Loc.Or($"side.{s.id}.name", s.displayName);
        // thẻ chưa có id: tra theo tên tiếng Việt trong asset (Card_*.asset)
        static readonly System.Collections.Generic.Dictionary<string, string> CardKeys = new System.Collections.Generic.Dictionary<string, string>
        {
            { "Thẳng 1", "card.line_1" }, { "Thẳng 2", "card.line_2" }, { "Thẳng 3", "card.line_3" },
            { "Chéo 1", "card.diag_1" }, { "Chéo 2", "card.diag_2" }, { "Chữ L", "card.l" }, { "Chữ T", "card.t" },
            { "Khuôn 1", "card.mold_1" }, { "Khuôn 2×2", "card.mold_2x2" },
            { "Xoay", "card.rotate" }, { "Lật", "card.flip" }, { "Dán", "card.glue" }, { "Nhân bản", "card.copy" },
        };

        public static string Name(this CardDef c) =>
            c == null ? "" : CardKeys.TryGetValue(c.displayName ?? "", out var key) ? Loc.Or(key, c.displayName) : c.displayName;

        public static string Name(this ShopItem it) => it == null ? "" : Loc.Or($"shop.{it.id}", it.displayName);

        public static string Region(FoodRegion r) =>
            r == FoodRegion.HaNoi ? Loc.T("region.hanoi") : r == FoodRegion.Seoul ? Loc.T("region.seoul") : "";

        /// <summary>"Màn 5" / "5단계" / "Level 5" (n bắt đầu từ 1).</summary>
        public static string Level(int n) => Loc.T("ui.level", n);
    }
}
