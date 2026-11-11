using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KoreanGame.Core
{
    public enum Lang { Vi, Ko, En }

    /// <summary>Vừa đổi ngôn ngữ: mọi chữ đang hiện cần vẽ lại.</summary>
    public struct LanguageChangedEvent { public Lang lang; }

    /// <summary>
    /// Bảng dịch Việt / Hàn / Anh đọc từ Data/Localization/localization.csv (cột key, vi, ko, en).
    /// Loc.T("ui.play") trả chữ theo ngôn ngữ đang chọn; thiếu bản dịch thì dùng tiếng Việt, thiếu nữa thì trả về key.
    /// Ngôn ngữ lưu trong PlayerPrefs; lần đầu chọn theo ngôn ngữ máy (Hàn → 한국어, Việt → Tiếng Việt, còn lại → English).
    /// </summary>
    public static class Loc
    {
        public const string PrefKey = "settings.lang";
        public const string CsvAssetPath = "Assets/_Project/Data/Localization/localization.csv";

        static Dictionary<string, string[]> table;
        static Lang? current;

        public static bool Loaded => table != null;

        public static Lang Current
        {
            get
            {
                if (current == null)
                    current = PlayerPrefs.HasKey(PrefKey) ? (Lang)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey), 0, 2) : FromSystem(Application.systemLanguage);
                return current.Value;
            }
            set
            {
                if (current == value) return;
                current = value;
                PlayerPrefs.SetInt(PrefKey, (int)value);
                PlayerPrefs.Save();
                EventBus<LanguageChangedEvent>.Raise(new LanguageChangedEvent { lang = value });
            }
        }

        public static Lang FromSystem(SystemLanguage s) =>
            s == SystemLanguage.Korean ? Lang.Ko : s == SystemLanguage.Vietnamese ? Lang.Vi : Lang.En;

        /// <summary>Ngôn ngữ kế tiếp (nút đổi ngôn ngữ xoay vòng Việt → Hàn → Anh).</summary>
        public static Lang Next(Lang l) => (Lang)(((int)l + 1) % 3);

        public static void Load(TextAsset csv)
        {
            if (csv != null) Load(csv.text);
        }

        public static void Load(string csv)
        {
            table = new Dictionary<string, string[]>();
            if (string.IsNullOrEmpty(csv)) return;
            var rows = Parse(csv);
            for (int r = 1; r < rows.Count; r++)   // dòng 0 là tiêu đề
            {
                var row = rows[r];
                if (row.Count < 2 || string.IsNullOrWhiteSpace(row[0])) continue;
                table[row[0].Trim()] = new[] { At(row, 1), At(row, 2), At(row, 3) };
            }
        }

        static string At(List<string> row, int i) => i < row.Count ? row[i] : "";

        public static bool Has(string key) => EnsureLoaded() && table.ContainsKey(key);

        /// <summary>Chữ theo ngôn ngữ đang chọn. args điền vào {0}, {1}…</summary>
        public static string T(string key, params object[] args) => TIn(Current, key, args);

        public static string TIn(Lang lang, string key, params object[] args)
        {
            string s = null;
            if (EnsureLoaded() && key != null && table.TryGetValue(key, out var v))
            {
                s = v[(int)lang];
                if (string.IsNullOrEmpty(s)) s = v[0];
            }
            if (string.IsNullOrEmpty(s)) s = key;
            return args != null && args.Length > 0 ? SafeFormat(s, args) : s;
        }

        /// <summary>Như T nhưng thiếu key thì trả fallback (dữ liệu cũ chưa có trong bảng dịch).</summary>
        public static string Or(string key, string fallback, params object[] args)
        {
            if (!Has(key)) return args != null && args.Length > 0 ? SafeFormat(fallback, args) : fallback;
            return T(key, args);
        }

        static string SafeFormat(string s, object[] args)
        {
            try { return string.Format(s, args); }
            catch (System.FormatException) { return s; }
        }

        static bool EnsureLoaded()
        {
#if UNITY_EDITOR
            if (table == null) Load(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(CsvAssetPath));   // test / editor chưa qua GameBootstrap
#endif
            return table != null;
        }

        /// <summary>CSV chuẩn: ô có dấu phẩy / xuống dòng nằm trong "…", "" là dấu ngoặc kép.</summary>
        static List<List<string>> Parse(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            int i = text.Length > 0 && text[0] == '﻿' ? 1 : 0;
            for (; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                        else quoted = false;
                    }
                    else cell.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == ',') { row.Add(cell.ToString()); cell.Clear(); }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    row.Add(cell.ToString()); cell.Clear();
                    rows.Add(row); row = new List<string>();
                }
                else cell.Append(c);
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
            return rows;
        }
    }
}
