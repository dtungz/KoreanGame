using System.Collections.Generic;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Shared
{
    /// <summary>
    /// Baloo / Nunito không có chữ Hàn, còn font dự phòng của Unity lấy nét mảnh của hệ điều hành.
    /// Khi chọn 한국어: mọi Text trong scene đổi sang font Hàn (Jua, tròn đậm); đổi ngôn ngữ khác thì trả font cũ.
    /// Logo (tên trong Keep) giữ nguyên font.
    /// </summary>
    public static class LangFonts
    {
        static readonly HashSet<string> Keep = new HashSet<string> { "Bento", "Journey" };
        static readonly Dictionary<Text, Font> original = new Dictionary<Text, Font>();
        static Font korean;
        static bool subscribed;

        public static void Init(Font koreanFont)
        {
            korean = koreanFont;
            if (!subscribed) { EventBus<LanguageChangedEvent>.Subscribe(e => Apply(e.lang)); subscribed = true; }
            Apply(Loc.Current);
        }

        public static void Apply(Lang lang)
        {
            if (korean == null) return;
            foreach (var t in Object.FindObjectsOfType<Text>(true))
            {
                if (t == null || Keep.Contains(t.name)) continue;
                if (!original.ContainsKey(t)) original[t] = t.font;
                var want = lang == Lang.Ko ? korean : original[t];
                if (t.font != want) t.font = want;
            }
        }

        /// <summary>Editor (chụp ảnh / soát chữ): đổi font tạm theo ngôn ngữ, dùng font Hàn chỉ định.</summary>
        public static void ApplyWith(Font koreanFont, Lang lang)
        {
            korean = koreanFont;
            Apply(lang);
        }
    }
}
