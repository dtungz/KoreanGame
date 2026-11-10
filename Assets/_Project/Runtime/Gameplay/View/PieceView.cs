using System.Collections.Generic;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Một miếng cơm: khối vân cơm + gờ đáy + viền ở mép ngoài (giống drawRice của prototype),
    /// kèm bóng đổ chỉ hiện khi miếng được nhấc lên.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PieceView : MonoBehaviour
    {
        [SerializeField] MeshFilter meshFilter;
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] MeshFilter shadowFilter;
        [SerializeField] MeshRenderer shadowRenderer;

        Mesh mesh, shadow;
        Material colorMaterial, riceMaterial;
        public Piece Piece { get; private set; }

        /// <summary>Tâm miếng theo toạ độ local (x, -y).</summary>
        public Vector3 LocalCenter { get; private set; }

        void OnValidate()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        }

        public void Init(Material colorMat, Material riceMat)
        {
            colorMaterial = colorMat;
            riceMaterial = riceMat;
            mesh = new Mesh { name = "Piece" };
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterials = new[] { colorMat, riceMat, colorMat };
            shadow = new Mesh { name = "PieceShadow" };
            shadowFilter.sharedMesh = shadow;
            shadowRenderer.sharedMaterial = colorMat;
            shadowRenderer.enabled = false;
        }

        public void Build(Piece p) => Build(p, null, default);

        /// <summary>
        /// dish != null: miếng nằm gọn trong hộp của màn có món đặc trưng. Mặt cơm hiện đúng phần ảnh món ở chỗ đó
        /// (UV theo khung chữ nhật của khay), nên hộp "thành món" dần theo từng miếng đặt vào.
        /// </summary>
        public void Build(Piece p, Material dish, BBox trayBox)
        {
            Piece = p;
            Build(p.tris);
            if (dish == null)
            {
                meshRenderer.sharedMaterials = new[] { colorMaterial, riceMaterial, colorMaterial };
                return;
            }
            var uv = mesh.uv;
            float w = trayBox.W, h = trayBox.H;
            for (int i = 0; i < uv.Length; i++)   // UV gốc = (x, -y) toạ độ thớt → (0..1) trong khung khay, gốc ảnh ở dưới-trái
                uv[i] = new Vector2((uv[i].x - trayBox.minx) / w, (uv[i].y + trayBox.maxy + 1) / h);
            mesh.uv = uv;
            meshRenderer.sharedMaterials = new[] { colorMaterial, dish, colorMaterial };
        }

        public void Build(List<Tri> tris)
        {
            var mb = new MeshBuilder(3);
            var edge = Geometry.Boundary(tris);
            mb.Lines(0, edge, .1f, Palette.RiceLip, new Vector2(0, .07f));
            mb.Tris(1, tris, Color.white);
            mb.Lines(2, edge, .065f, Palette.RiceEdge);
            mb.Lines(2, edge, .025f, new Color(1, 1, 1, .8f), new Vector2(0, .035f));
            mb.Apply(mesh);

            var sb = new MeshBuilder(1);
            sb.Tris(0, tris, new Color(.25f, .16f, .1f, .22f), new Vector2(.08f, .16f));
            sb.Lines(0, edge, .12f, new Color(.25f, .16f, .1f, .22f), new Vector2(.08f, .16f));
            sb.Apply(shadow);

            var b = Geometry.BBox(tris);
            LocalCenter = new Vector3((b.minx + b.maxx + 1) / 2f, -(b.miny + b.maxy + 1) / 2f, 0);
            SetLifted(false);
        }

        public void SetOrder(int order)
        {
            meshRenderer.sortingOrder = order;
            shadowRenderer.sortingOrder = order - 1;
        }

        /// <summary>Nhấc lên: to hơn quanh tâm miếng + hiện bóng đổ.</summary>
        public void SetLifted(bool lifted, float scale = 1.06f)
        {
            shadowRenderer.enabled = lifted;
            float s = lifted ? scale : 1f;
            transform.localScale = new Vector3(s, s, 1);
        }

        /// <summary>Đặt vị trí sao cho phép phóng to xoay quanh tâm miếng, cộng thêm offset (local).</summary>
        public void PlaceAt(Vector3 offset)
        {
            float s = transform.localScale.x;
            transform.localPosition = offset + LocalCenter * (1 - s);
        }
    }
}
