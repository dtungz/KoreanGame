using System;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    /// <summary>Một tam giác trong ô (x, y). t = 0 trên, 1 phải, 2 dưới, 3 trái. Trục y hướng xuống.</summary>
    [Serializable]
    public struct Tri : IEquatable<Tri>, IComparable<Tri>
    {
        public int x, y, t;

        public Tri(int x, int y, int t) { this.x = x; this.y = y; this.t = t; }

        public Tri Shift(int dx, int dy) => new Tri(x + dx, y + dy, t);
        public Vector2Int Cell => new Vector2Int(x, y);

        public bool Equals(Tri o) => x == o.x && y == o.y && t == o.t;
        public override bool Equals(object o) => o is Tri q && Equals(q);
        public override int GetHashCode() => unchecked(((x * 7919) ^ (y * 104729)) * 4 + t);

        /// <summary>Thứ tự (y, x, t), dùng khi xếp miếng vào khay.</summary>
        public int CompareTo(Tri o) => y != o.y ? y.CompareTo(o.y) : x != o.x ? x.CompareTo(o.x) : t.CompareTo(o.t);

        public override string ToString() => x + "," + y + "," + t;
    }

    public enum EdgeKind : byte { H, V, D, A }

    /// <summary>Một cạnh đơn vị trên lưới: H ngang (cạnh trên ô x,y), V dọc (cạnh trái ô), D chéo "\", A chéo "/".</summary>
    public readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        public readonly EdgeKind kind;
        public readonly int x, y;

        public EdgeKey(EdgeKind kind, int x, int y) { this.kind = kind; this.x = x; this.y = y; }

        public bool Equals(EdgeKey o) => kind == o.kind && x == o.x && y == o.y;
        public override bool Equals(object o) => o is EdgeKey e && Equals(e);
        public override int GetHashCode() => unchecked(((x * 7919) ^ (y * 104729)) * 4 + (int)kind);
        public override string ToString() => kind.ToString().ToLowerInvariant() + x + "," + y;
    }

    /// <summary>Đoạn thẳng dài 1 bước lưới (ngang, dọc hoặc chéo).</summary>
    public readonly struct Seg
    {
        public readonly int x1, y1, x2, y2;
        public Seg(int x1, int y1, int x2, int y2) { this.x1 = x1; this.y1 = y1; this.x2 = x2; this.y2 = y2; }
    }

    /// <summary>Vách ngăn: đoạn thẳng trên cạnh ô, toạ độ điểm lưới.</summary>
    [Serializable]
    public struct Wall
    {
        public int x1, y1, x2, y2;
        public Wall(int x1, int y1, int x2, int y2) { this.x1 = x1; this.y1 = y1; this.x2 = x2; this.y2 = y2; }
        public Wall Shift(int dx, int dy) => new Wall(x1 + dx, y1 + dy, x2 + dx, y2 + dy);
    }

    public struct BBox
    {
        public int minx, maxx, miny, maxy;
        public int W => maxx - minx + 1;
        public int H => maxy - miny + 1;
    }

    public struct Neighbor
    {
        public Tri tri;
        public EdgeKey edge;
        public Neighbor(Tri tri, EdgeKey edge) { this.tri = tri; this.edge = edge; }
    }
}
