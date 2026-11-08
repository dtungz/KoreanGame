using System;
using System.Collections.Generic;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    public enum CardFamily { Line, Mold, Diag, Glue, Fn }
    public enum CardKind { Cut, Rot, Flip, Copy, Glue }

    [Serializable]
    public class Polyline
    {
        public List<Vector2Int> pts = new List<Vector2Int>();
    }

    /// <summary>Một loại thẻ. Thẻ cắt / dán có đường (polys) đi qua các điểm lưới; thẻ chức năng chỉ có kind.</summary>
    [CreateAssetMenu(menuName = "KoreanGame/Card", fileName = "Card_")]
    public class CardDef : ScriptableObject
    {
        public string displayName;
        public CardFamily family;
        public CardKind kind;
        public List<Polyline> polys = new List<Polyline>();
        public Sprite icon;

        List<Vector2Int[]> cache;

        public bool CanRotate => kind != CardKind.Copy;
        public bool IsFunction => kind == CardKind.Rot || kind == CardKind.Flip || kind == CardKind.Copy;

        public IReadOnlyList<Vector2Int[]> Polys
        {
            get
            {
                if (cache == null)
                {
                    cache = new List<Vector2Int[]>();
                    foreach (var p in polys) cache.Add(p.pts.ToArray());
                }
                return cache;
            }
        }

        /// <summary>Chữ ký để so trùng thẻ khi import.</summary>
        public string Signature
        {
            get
            {
                var s = family + "/" + kind + ":";
                foreach (var p in polys) { foreach (var v in p.pts) s += v.x + "," + v.y + " "; s += "|"; }
                return s;
            }
        }

        void OnValidate() => cache = null;
    }
}
