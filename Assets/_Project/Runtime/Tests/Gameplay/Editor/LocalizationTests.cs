using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using KoreanGame.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KoreanGame.Tests
{
    /// <summary>Bảng dịch Việt / Hàn / Anh: đủ 3 thứ tiếng, chỗ điền {0} khớp nhau, key nào code dùng cũng có.</summary>
    public class LocalizationTests
    {
        static List<string[]> Rows()
        {
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>(Loc.CsvAssetPath);
            Assert.IsNotNull(csv, "Thiếu " + Loc.CsvAssetPath);
            Loc.Load(csv);
            var rows = new List<string[]>();
            foreach (var key in Keys(csv.text))
                rows.Add(new[] { key, Loc.TIn(Lang.Vi, key), Loc.TIn(Lang.Ko, key), Loc.TIn(Lang.En, key) });
            return rows;
        }

        static IEnumerable<string> Keys(string csv)
        {
            foreach (Match m in Regex.Matches(csv, @"^﻿?([a-z0-9_]+(?:\.[a-z0-9_]+)+),", RegexOptions.Multiline))
                yield return m.Groups[1].Value;
        }

        [Test]
        public void EveryRowHasAllThreeLanguages()
        {
            var bad = new List<string>();
            foreach (var r in Rows())
                for (int l = 1; l <= 3; l++)
                    if (string.IsNullOrWhiteSpace(r[l]) || r[l] == r[0]) bad.Add($"{r[0]} ({(Lang)(l - 1)})");
            Assert.IsEmpty(bad, "Thiếu bản dịch:\n" + string.Join("\n", bad));
        }

        [Test]
        public void PlaceholdersMatchAcrossLanguages()
        {
            var bad = new List<string>();
            foreach (var r in Rows())
            {
                string vi = Holes(r[1]);
                if (Holes(r[2]) != vi || Holes(r[3]) != vi) bad.Add($"{r[0]}: vi={vi} ko={Holes(r[2])} en={Holes(r[3])}");
            }
            Assert.IsEmpty(bad, "Chỗ điền {n} lệch giữa các ngôn ngữ:\n" + string.Join("\n", bad));
        }

        static string Holes(string s)
        {
            var set = new SortedSet<string>();
            foreach (Match m in Regex.Matches(s ?? "", @"\{\d+\}")) set.Add(m.Value);
            return string.Join("", set);
        }

        [Test]
        public void KeysUsedInCodeExist()
        {
            Rows();
            var missing = new List<string>();
            foreach (var file in Directory.GetFiles("Assets/_Project/Runtime", "*.cs", SearchOption.AllDirectories))
            {
                if (file.Replace('\\', '/').Contains("/Tests/")) continue;
                var src = File.ReadAllText(file);
                // Loc.T("key"…), Loc.T(cond ? "a" : "b"), Key = "key", { "Chữ", "key" } trong builder
                foreach (Match m in Regex.Matches(src, @"Loc\.(?:T|Or)\(\s*""([a-z0-9_]+(?:\.[a-z0-9_]+)+)"""))
                    Check(m.Groups[1].Value, file, missing);
                foreach (Match m in Regex.Matches(src, @"Loc\.T\([^()]*\?\s*""([a-z0-9_.]+)""\s*:\s*""([a-z0-9_.]+)"""))
                { Check(m.Groups[1].Value, file, missing); Check(m.Groups[2].Value, file, missing); }
                foreach (Match m in Regex.Matches(src, @"\{\s*""[^""]+"",\s*""([a-z0-9_]+\.[a-z0-9_.]+)""\s*\}"))
                    Check(m.Groups[1].Value, file, missing);
            }
            foreach (Lang l in System.Enum.GetValues(typeof(Lang))) Check("ui.lang." + l.ToString().ToLowerInvariant(), "SettingsPopup", missing);
            Assert.IsEmpty(missing, "Key chưa có trong bảng dịch:\n" + string.Join("\n", missing));
        }

        static void Check(string key, string file, List<string> missing)
        {
            if (!Loc.Has(key)) missing.Add($"{key}  ({Path.GetFileName(file)})");
        }

        [Test]
        public void SystemLanguagePicksDefault()
        {
            Assert.AreEqual(Lang.Ko, Loc.FromSystem(SystemLanguage.Korean));
            Assert.AreEqual(Lang.Vi, Loc.FromSystem(SystemLanguage.Vietnamese));
            Assert.AreEqual(Lang.En, Loc.FromSystem(SystemLanguage.French));
            Assert.AreEqual(Lang.Ko, Loc.Next(Lang.Vi));
            Assert.AreEqual(Lang.Vi, Loc.Next(Lang.En));
        }

        [Test]
        public void ParsesQuotedCells()
        {
            Loc.Load("key,vi,ko,en\na.b,\"Xin chào, bạn\",\"안녕, \"\"친구\"\"\",Hi\n");
            try
            {
                Assert.AreEqual("Xin chào, bạn", Loc.TIn(Lang.Vi, "a.b"));
                Assert.AreEqual("안녕, \"친구\"", Loc.TIn(Lang.Ko, "a.b"));
                Assert.AreEqual("Hi", Loc.TIn(Lang.En, "a.b"));
                Assert.AreEqual("missing.key", Loc.TIn(Lang.En, "missing.key"));
            }
            finally { Loc.Load(AssetDatabase.LoadAssetAtPath<TextAsset>(Loc.CsvAssetPath)); }
        }
    }
}
