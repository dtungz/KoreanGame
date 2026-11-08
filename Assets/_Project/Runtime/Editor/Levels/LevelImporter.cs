using System.Collections.Generic;
using System.IO;
using KoreanGame.Gameplay;
using UnityEditor;
using UnityEngine;

namespace KoreanGame.EditorTools
{
    /// <summary>Nạp các màn xuất từ prototype play.html (Data/Levels/Source/play_levels.json) thành LevelData.</summary>
    public static class LevelImporter
    {
        public const string LevelFolder = "Assets/_Project/Data/Levels";
        public const string DatabasePath = LevelFolder + "/LevelDatabase.asset";
        const string SourceJson = LevelFolder + "/Source/play_levels.json";

        [MenuItem("Tools/KoreanGame/Levels/Import Prototype Levels (1-14)")]
        public static void ImportPrototype()
        {
            CardLibrary.EnsureAll();
            var root = (List<object>)MiniJson.Parse(File.ReadAllText(SourceJson));
            var db = LoadOrCreateDatabase();
            for (int i = 0; i < root.Count; i++)
            {
                var L = GetOrCreateLevel(i + 1);
                Fill(L, (Dictionary<string, object>)root[i]);
                L.isTutorial = true;
                L.unlocksSilhouette = L.cards.Exists(c => c.family == CardFamily.Diag) && L.cards.Count >= 3;
                EditorUtility.SetDirty(L);
                SetInDatabase(db, i, L);
            }
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelImporter] Đã nạp {root.Count} màn từ prototype.");
        }

        public static LevelDatabase LoadOrCreateDatabase()
        {
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
            if (db != null) return db;
            db = ScriptableObject.CreateInstance<LevelDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
            return db;
        }

        public static string LevelPath(int number) => $"{LevelFolder}/Level_{number:00}.asset";

        public static LevelData GetOrCreateLevel(int number)
        {
            var path = LevelPath(number);
            var L = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (L != null) return L;
            L = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(L, path);
            return L;
        }

        public static void SetInDatabase(LevelDatabase db, int index, LevelData L)
        {
            while (db.levels.Count <= index) db.levels.Add(null);
            db.levels[index] = L;
        }

        static int I(object o) => (int)(double)o;

        static List<Tri> Tris(object o)
        {
            var r = new List<Tri>();
            foreach (List<object> q in (List<object>)o) r.Add(new Tri(I(q[0]), I(q[1]), I(q[2])));
            return r;
        }

        static Vector2Int V(object o)
        {
            var l = (List<object>)o;
            return new Vector2Int(I(l[0]), I(l[1]));
        }

        static void Fill(LevelData L, Dictionary<string, object> j)
        {
            L.hint = (string)j["hint"];
            L.rice = new List<RicePiece>();
            foreach (Dictionary<string, object> r in (List<object>)j["rice"])
                L.rice.Add(new RicePiece { tris = Tris(r["s"]), at = V(r["at"]) });

            var t = (Dictionary<string, object>)j["tray"];
            L.tray = new TrayData { tris = Tris(t["s"]), at = V(t["at"]) };
            foreach (List<object> w in (List<object>)t["walls"]) L.tray.walls.Add(new Wall(I(w[0]), I(w[1]), I(w[2]), I(w[3])));
            foreach (Dictionary<string, object> f in (List<object>)t["fixed"])
            {
                var ff = new FixedFood { kind = (string)f["kind"] };
                foreach (var c in (List<object>)f["cells"]) ff.cells.Add(V(c));
                L.tray.fixedFoods.Add(ff);
            }

            L.cards = new List<CardDef>();
            foreach (Dictionary<string, object> c in (List<object>)j["cards"])
            {
                var probe = ScriptableObject.CreateInstance<CardDef>();
                probe.family = Family((string)c["fam"], (string)c["kind"]);
                probe.kind = Kind((string)c["kind"]);
                foreach (List<object> pl in (List<object>)c["polys"])
                {
                    var line = new Polyline();
                    foreach (var p in pl) line.pts.Add(V(p));
                    probe.polys.Add(line);
                }
                var def = CardLibrary.FindBySignature(probe.Signature);
                Object.DestroyImmediate(probe);
                if (def == null) Debug.LogError($"[LevelImporter] Không tìm thấy thẻ cho {c["name"]}");
                else L.cards.Add(def);
            }
        }

        static CardFamily Family(string fam, string kind)
        {
            switch (fam)
            {
                case "mold": return CardFamily.Mold;
                case "diag": return CardFamily.Diag;
                case "glue": return CardFamily.Glue;
                case "fn": return CardFamily.Fn;
                default: return CardFamily.Line;
            }
        }

        static CardKind Kind(string kind)
        {
            switch (kind)
            {
                case "rot": return CardKind.Rot;
                case "flip": return CardKind.Flip;
                case "copy": return CardKind.Copy;
                case "glue": return CardKind.Glue;
                default: return CardKind.Cut;
            }
        }
    }
}
