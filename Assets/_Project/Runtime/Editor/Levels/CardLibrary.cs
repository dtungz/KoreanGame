using System.Collections.Generic;
using KoreanGame.Gameplay;
using UnityEditor;
using UnityEngine;

namespace KoreanGame.EditorTools
{
    /// <summary>Tạo / tìm các CardDef trong Data/Cards. Bộ thẻ theo GDD mục 5.</summary>
    public static class CardLibrary
    {
        public const string Folder = "Assets/_Project/Data/Cards";
        const string IconFolder = "Assets/_Project/Source/Art/UI/Gameplay/";

        static Vector2Int P(int x, int y) => new Vector2Int(x, y);

        static readonly (string asset, string name, CardFamily fam, CardKind kind, Vector2Int[][] polys, string icon)[] Specs =
        {
            ("Line_H1", "Thẳng 1", CardFamily.Line, CardKind.Cut, new[] { new[] { P(0, 0), P(1, 0) } }, null),
            ("Line_H2", "Thẳng 2", CardFamily.Line, CardKind.Cut, new[] { new[] { P(0, 0), P(2, 0) } }, null),
            ("Line_H3", "Thẳng 3", CardFamily.Line, CardKind.Cut, new[] { new[] { P(0, 0), P(3, 0) } }, null),
            ("Line_V1", "Thẳng 1", CardFamily.Line, CardKind.Cut, new[] { new[] { P(0, 0), P(0, 1) } }, null),
            ("Line_V2", "Thẳng 2", CardFamily.Line, CardKind.Cut, new[] { new[] { P(0, 0), P(0, 2) } }, null),
            ("Line_V3", "Thẳng 3", CardFamily.Line, CardKind.Cut, new[] { new[] { P(0, 0), P(0, 3) } }, null),
            ("Cut_L", "Chữ L", CardFamily.Line, CardKind.Cut, new[] { new[] { P(2, 0), P(2, 1), P(0, 1) } }, null),
            ("Cut_T", "Chữ T", CardFamily.Line, CardKind.Cut, new[] { new[] { P(0, 0), P(2, 0) }, new[] { P(1, 0), P(1, 1) } }, null),
            ("Mold_1", "Khuôn 1", CardFamily.Mold, CardKind.Cut, new[] { new[] { P(0, 0), P(1, 0), P(1, 1), P(0, 1), P(0, 0) } }, null),
            ("Mold_2x2", "Khuôn 2×2", CardFamily.Mold, CardKind.Cut, new[] { new[] { P(0, 0), P(2, 0), P(2, 2), P(0, 2), P(0, 0) } }, null),
            ("Diag_D1", "Chéo 1", CardFamily.Diag, CardKind.Cut, new[] { new[] { P(0, 0), P(1, 1) } }, null),
            ("Diag_D2", "Chéo 2", CardFamily.Diag, CardKind.Cut, new[] { new[] { P(0, 0), P(2, 2) } }, null),
            ("Diag_A1", "Chéo 1", CardFamily.Diag, CardKind.Cut, new[] { new[] { P(1, 0), P(0, 1) } }, null),
            ("Diag_A2", "Chéo 2", CardFamily.Diag, CardKind.Cut, new[] { new[] { P(2, 0), P(0, 2) } }, null),
            ("Fn_Rotate", "Xoay", CardFamily.Fn, CardKind.Rot, new Vector2Int[0][], "icon_rotate"),
            ("Fn_Flip", "Lật", CardFamily.Fn, CardKind.Flip, new Vector2Int[0][], "icon_flip"),
            ("Fn_Copy", "Nhân bản", CardFamily.Fn, CardKind.Copy, new Vector2Int[0][], "icon_copy"),
            ("Fn_Glue", "Dán", CardFamily.Glue, CardKind.Glue, new[] { new[] { P(0, 0), P(1, 0) } }, "icon_glue"),
        };

        [MenuItem("Tools/KoreanGame/Levels/Create Card Set")]
        public static Dictionary<string, CardDef> EnsureAll()
        {
            var map = new Dictionary<string, CardDef>();
            foreach (var s in Specs)
            {
                var path = $"{Folder}/Card_{s.asset}.asset";
                var c = AssetDatabase.LoadAssetAtPath<CardDef>(path);
                bool created = c == null;
                if (created) c = ScriptableObject.CreateInstance<CardDef>();
                c.displayName = s.name;
                c.family = s.fam;
                c.kind = s.kind;
                c.polys = new List<Polyline>();
                foreach (var pl in s.polys) c.polys.Add(new Polyline { pts = new List<Vector2Int>(pl) });
                c.icon = s.icon == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(IconFolder + s.icon + ".png");
                if (created) AssetDatabase.CreateAsset(c, path);
                else EditorUtility.SetDirty(c);
                map[s.asset] = c;
            }
            AssetDatabase.SaveAssets();
            return map;
        }

        public static CardDef Get(string asset) => AssetDatabase.LoadAssetAtPath<CardDef>($"{Folder}/Card_{asset}.asset");

        /// <summary>Tìm thẻ theo chữ ký (họ / loại / đường cắt).</summary>
        public static CardDef FindBySignature(string signature)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:CardDef", new[] { Folder }))
            {
                var c = AssetDatabase.LoadAssetAtPath<CardDef>(AssetDatabase.GUIDToAssetPath(guid));
                if (c != null && c.Signature == signature) return c;
            }
            return null;
        }
    }
}
