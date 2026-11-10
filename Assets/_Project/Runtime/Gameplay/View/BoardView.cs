using System.Collections.Generic;
using DG.Tweening;
using KoreanGame.Core;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Vẽ thớt 7×10: lưới, khay, các miếng cơm, xem trước khi kéo thẻ, và mọi hiệu ứng "feel"
    /// (vệt dao, miếng tách, hạt cơm, nhấc / hít ô / bay về, chớp sáng, nắp hộp khi thắng).
    /// Gốc transform = góc trên-trái ô (0, 0); toạ độ thớt (x, y) ↦ local (x, -y).
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        const int OrderTray = 0, OrderPiece = 10, OrderPreview = 20, OrderLift = 30, OrderFlash = 38, OrderSlash = 40, OrderLid = 50;
        const float SnapTime = .16f;

        [SerializeField] Camera cam;
        [SerializeField] Transform content;          // con chứa khay + cơm, dùng cho hiệu ứng nảy (không đụng gốc toạ độ)
        [SerializeField] MeshFilter gridMesh;
        [SerializeField] TrayView trayView;
        [SerializeField] PieceView pieceTemplate;
        [SerializeField] MeshFilter previewMesh;
        [SerializeField] MeshRenderer previewRenderer;
        [SerializeField] MeshFilter slashMesh;
        [SerializeField] MeshRenderer slashRenderer;
        [SerializeField] MeshFilter flashMesh;
        [SerializeField] MeshRenderer flashRenderer;
        [SerializeField] MeshFilter lidMesh;
        [SerializeField] MeshRenderer lidRenderer;
        [SerializeField] ParticleSystem riceBurst;
        [SerializeField] Material colorMaterial;
        [SerializeField] Material riceMaterial;

        readonly List<PieceView> pool = new List<PieceView>();
        readonly Dictionary<Piece, PieceView> views = new Dictionary<Piece, PieceView>();
        Mesh preview, slash, flash, lid;
        MaterialPropertyBlock block;
        SpriteRenderer dishReveal;   // ảnh món đặc trưng phủ lên khay khi thắng (tạo lúc chạy, không cần dựng lại scene)

        public Camera Camera => cam;

        public void Init()
        {
            pieceTemplate.gameObject.SetActive(false);
            trayView.Init(colorMaterial);
            block = new MaterialPropertyBlock();
            preview = Setup(previewMesh, previewRenderer, "Preview", OrderPreview);
            slash = Setup(slashMesh, slashRenderer, "Slash", OrderSlash);
            flash = Setup(flashMesh, flashRenderer, "Flash", OrderFlash);
            lid = Setup(lidMesh, lidRenderer, "Lid", OrderLid);
            riceBurst.GetComponent<ParticleSystemRenderer>().sortingOrder = OrderSlash + 1;
            dishReveal = new GameObject("DishReveal").AddComponent<SpriteRenderer>();
            dishReveal.transform.SetParent(content, false);
            dishReveal.sortingOrder = OrderFlash - 1;   // trên cơm, dưới chớp sáng và nắp
            dishReveal.enabled = false;
            BuildGrid();
        }

        Mesh Setup(MeshFilter f, MeshRenderer r, string name, int order)
        {
            var m = new Mesh { name = name };
            f.sharedMesh = m;
            r.sharedMaterial = colorMaterial;
            r.sortingOrder = order;
            return m;
        }

        void BuildGrid()
        {
            var mb = new MeshBuilder(1);
            for (int i = 1; i < Geometry.Cols; i++) mb.Line(0, new Vector2(i, 0), new Vector2(i, Geometry.Rows), .025f, Palette.Grid, false);
            for (int j = 1; j < Geometry.Rows; j++) mb.Line(0, new Vector2(0, j), new Vector2(Geometry.Cols, j), .025f, Palette.Grid, false);
            var m = new Mesh { name = "Grid" };
            mb.Apply(m);
            gridMesh.sharedMesh = m;
        }

        static Vector3 Local(Vector2 board) => new Vector3(board.x, -board.y, 0);

        /// <summary>Điểm màn hình → toạ độ thớt (đơn vị ô, y hướng xuống).</summary>
        public Vector2 ScreenToBoard(Vector2 screen)
        {
            var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            var local = transform.InverseTransformPoint(w);
            return new Vector2(local.x, -local.y);
        }

        // ======================= Vẽ trạng thái =======================

        public void Render(BoardState s)
        {
            trayView.KillTweens(true);
            trayView.transform.localPosition = Vector3.zero;
            trayView.Build(s.tray);
            trayView.SetOrder(OrderTray);

            views.Clear();
            for (int i = 0; i < s.pieces.Count; i++)
            {
                var v = Get(i);
                v.KillTweens(true);
                v.Build(s.pieces[i]);
                v.PlaceAt(Vector3.zero);
                v.SetOrder(OrderPiece);
                views[s.pieces[i]] = v;
            }
            for (int i = s.pieces.Count; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
            ClearPreview();
        }

        /// <summary>Xoá nắp hộp / chớp sáng còn sót (khi nạp màn mới, hoàn tác...).</summary>
        public void ResetFx()
        {
            lidRenderer.KillTweens();
            flashRenderer.KillTweens();
            lid.Clear();
            flash.Clear();
            dishReveal.KillTweens();
            dishReveal.enabled = false;
            content.KillTweens(true);
            content.localPosition = Vector3.zero;
        }

        PieceView Get(int i)
        {
            while (pool.Count <= i)
            {
                var v = Instantiate(pieceTemplate, content);
                v.Init(colorMaterial, riceMaterial);
                pool.Add(v);
            }
            pool[i].gameObject.SetActive(true);
            return pool[i];
        }

        // ======================= Kéo thả =======================

        /// <summary>Nhấc miếng cơm / khay đang kéo theo ngón tay (offset theo ô, chưa làm tròn).</summary>
        public void Lift(Piece piece, bool tray, List<Piece> carried, Vector2 offset)
        {
            var local = Local(offset);
            if (tray)
            {
                trayView.transform.localPosition = local;
                trayView.SetOrder(OrderLift);
                foreach (var p in carried)
                    if (views.TryGetValue(p, out var v)) { v.PlaceAt(local); v.SetOrder(OrderLift + 5); }
            }
            else if (piece != null && views.TryGetValue(piece, out var pv))
            {
                pv.SetLifted(true);
                pv.PlaceAt(local);
                pv.SetOrder(OrderLift + 5);
            }
        }

        /// <summary>
        /// Sau khi vẽ lại: cho miếng (hoặc khay + cơm bên trong) trượt từ vị trí đang thả về ô đích.
        /// Dùng cho cả "hít vào ô" (residual nhỏ) và "bay về chỗ cũ" (residual = cả quãng kéo).
        /// </summary>
        public void SnapFrom(Piece piece, bool tray, List<Piece> carried, Vector2 residual, bool bounce)
        {
            var from = Local(residual);
            var ease = bounce ? Ease.OutBack : Ease.OutCubic;
            float time = bounce ? SnapTime : SnapTime * 1.4f;
            if (tray)
            {
                var t = trayView.transform;
                t.localPosition = from;
                t.DOLocalMove(Vector3.zero, time).SetEase(ease).Guard(trayView);
                if (carried != null) foreach (var p in carried) TweenPiece(p, from, time, ease);
            }
            else if (piece != null) TweenPiece(piece, from, time, ease);
        }

        void TweenPiece(Piece p, Vector3 from, float time, Ease ease)
        {
            if (!views.TryGetValue(p, out var v)) return;
            v.KillTweens(true);
            var off = from;
            v.PlaceAt(off);
            DOTween.To(() => off, x => { off = x; if (v != null) v.PlaceAt(x); }, Vector3.zero, time).SetEase(ease).Guard(v);
        }

        // ======================= Xem trước khi kéo thẻ =======================

        public void ClearPreview() => preview.Clear();

        /// <summary>Khung xanh / đỏ ở chỗ sẽ thả miếng cơm hoặc khay.</summary>
        public void ShowDropOutline(List<Tri> tris, bool ok)
        {
            var mb = new MeshBuilder(1);
            foreach (var (p, q) in Geometry.Boundary(tris)) mb.DashedLine(0, p, q, .09f, ok ? Palette.Green : Palette.Red, .2f, .12f);
            mb.Apply(preview);
        }

        public void ShowPreview(MoveResult r)
        {
            var mb = new MeshBuilder(1);
            if (r != null)
            {
                switch (r.kind)
                {
                    case CardKind.Cut:
                        foreach (var sp in r.splits)
                            for (int i = 0; i < sp.parts.Count; i++) mb.Tris(0, sp.parts[i], Palette.Tint[i % Palette.Tint.Length]);
                        foreach (var g in r.segs)
                        {
                            Vector2 a = new Vector2(g.x1, g.y1), b = new Vector2(g.x2, g.y2);
                            if (r.valid) mb.DashedLine(0, a, b, .11f, Palette.CutValid, .2f, .12f);
                            else mb.DashedLine(0, a, b, .13f, Palette.CutInvalid, .18f, .12f);
                        }
                        if (r.valid) CutEndDots(mb, r.segs);
                        break;
                    case CardKind.Glue:
                        foreach (var p in r.touched) mb.Tris(0, p.tris, new Color(90 / 255f, 180 / 255f, 100 / 255f, .38f));
                        foreach (var g in r.segs)
                            mb.DashedLine(0, new Vector2(g.x1, g.y1), new Vector2(g.x2, g.y2), .16f, r.valid ? Palette.Green : Palette.CutInvalid, r.valid ? .05f : .18f, .12f);
                        break;
                    default:
                        if (r.target != null) mb.Tris(0, r.target.tris, r.valid ? new Color(90 / 255f, 180 / 255f, 100 / 255f, .35f) : new Color(214 / 255f, 51 / 255f, 42 / 255f, .28f));
                        if (r.valid) foreach (var (p, q) in Geometry.Boundary(r.tris)) mb.DashedLine(0, p, q, .09f, Palette.Green, .2f, .12f);
                        mb.Disc(0, r.point, .21f, Color.white, 14);
                        mb.Disc(0, r.point, .16f, r.valid ? Palette.Green : Palette.CutInvalid, 14);
                        break;
                }
            }
            mb.Apply(preview);
        }

        /// <summary>Chấm tròn xanh trời viền mực ở các đầu hở của đường cắt (đường khép kín như khuôn thì không có).</summary>
        static void CutEndDots(MeshBuilder mb, List<Seg> segs)
        {
            var count = new Dictionary<Vector2Int, int>();
            foreach (var g in segs)
                foreach (var p in new[] { new Vector2Int(g.x1, g.y1), new Vector2Int(g.x2, g.y2) })
                    count[p] = count.TryGetValue(p, out var n) ? n + 1 : 1;
            foreach (var kv in count)
            {
                if (kv.Value != 1) continue;
                mb.Disc(0, kv.Key, .17f, Palette.CutValid, 16);
                mb.Disc(0, kv.Key, .11f, Palette.UiSky, 16);
            }
        }

        // ======================= Hiệu ứng cắt =======================

        /// <summary>Sau khi cắt / dán / biến hình: vệt dao chạy dọc đường cắt, miếng tách ra rồi về, hạt cơm bắn.</summary>
        public void PlayCardFx(MoveResult r)
        {
            switch (r.kind)
            {
                case CardKind.Cut:
                    PlaySlash(r.segs, Color.white);
                    foreach (var sp in r.splits) NudgeParts(sp, r.segs);
                    BurstAlong(r.segs);
                    break;
                case CardKind.Glue:
                    PlaySlash(r.segs, new Color(.61f, .9f, .61f));
                    break;
                default:
                    if (r.tris != null) Flash(r.tris, new Color(1, 1, 1, .55f), false, .3f);
                    break;
            }
        }

        /// <summary>Vệt dao: hiện dần từ đầu tới cuối đường cắt, rồi mờ đi.</summary>
        void PlaySlash(List<Seg> segs, Color color)
        {
            slashRenderer.KillTweens();
            float total = segs.Count;
            float progress = 0;

            void Draw(float p)
            {
                var mb = new MeshBuilder(1);
                float left = p * total;
                foreach (var g in segs)
                {
                    if (left <= 0) break;
                    Vector2 a = new Vector2(g.x1, g.y1), b = new Vector2(g.x2, g.y2);
                    mb.Line(0, a, Vector2.Lerp(a, b, Mathf.Min(1, left)), .15f, color);
                    left -= 1;
                }
                mb.Apply(slash);
            }

            SetAlpha(slashRenderer, 1);
            Draw(0);
            var s = DOTween.Sequence().Guard(slashRenderer);
            s.Append(DOTween.To(() => progress, v => { progress = v; Draw(v); }, 1f, .12f).SetEase(Ease.OutQuad));
            float a = 1;
            s.Append(DOTween.To(() => a, v => { a = v; SetAlpha(slashRenderer, v); }, 0f, .28f));
        }

        /// <summary>Các miếng vừa tách dạt ra khỏi đường cắt ~0,1 ô rồi trở về.</summary>
        void NudgeParts(CutSplit sp, List<Seg> segs)
        {
            var center = Vector2.zero;
            foreach (var g in segs) center += new Vector2(g.x1 + g.x2, g.y1 + g.y2) / 2f;
            center /= Mathf.Max(1, segs.Count);
            foreach (var part in sp.parts)
            {
                PieceView v = null;
                foreach (var kv in views) if (ReferenceEquals(kv.Key.tris, part)) { v = kv.Value; break; }
                if (v == null) continue;
                var b = Geometry.BBox(part);
                var c = new Vector2((b.minx + b.maxx + 1) / 2f, (b.miny + b.maxy + 1) / 2f);
                var dir = (c - center).sqrMagnitude < 1e-4f ? Vector2.up : (c - center).normalized;
                var punch = Local(dir * .1f);
                var off = Vector3.zero;
                var s = DOTween.Sequence().Guard(v);
                s.Append(DOTween.To(() => off, x => { off = x; if (v != null) v.PlaceAt(x); }, punch, .07f).SetEase(Ease.OutQuad));
                s.Append(DOTween.To(() => off, x => { off = x; if (v != null) v.PlaceAt(x); }, Vector3.zero, .2f).SetEase(Ease.OutBack));
            }
        }

        /// <summary>Hạt cơm bắn ra ở hai đầu và giữa đường cắt.</summary>
        void BurstAlong(List<Seg> segs)
        {
            if (segs.Count == 0) return;
            var pts = new List<Vector2> { new Vector2(segs[0].x1, segs[0].y1), new Vector2(segs[segs.Count - 1].x2, segs[segs.Count - 1].y2) };
            var mid = segs[segs.Count / 2];
            pts.Add(new Vector2(mid.x1 + mid.x2, mid.y1 + mid.y2) / 2f);
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            foreach (var p in pts)
            {
                ep.position = transform.TransformPoint(Local(p));
                riceBurst.Emit(ep, 6);
            }
        }

        // ======================= Chớp sáng / báo lỗi =======================

        /// <summary>Chớp một vùng (tô hoặc chỉ viền) rồi mờ đi.</summary>
        public void Flash(List<Tri> tris, Color color, bool outlineOnly, float time = .35f, int blinks = 1)
        {
            var mb = new MeshBuilder(1);
            if (outlineOnly) mb.Lines(0, Geometry.Boundary(tris), .16f, color);
            else mb.Tris(0, tris, color);
            mb.Apply(flash);
            PulseAlpha(flashRenderer, time, blinks);
        }

        /// <summary>Viền khay chớp đỏ: miếng cơm thả vắt qua mép khay.</summary>
        public void FlashTrayEdge(TrayState tray) => Flash(tray.tris, WarnRed, true, .45f, 2);

        static readonly Color WarnRed = new Color(.84f, .2f, .16f, .9f);

        /// <summary>Khay đã kín nhưng miếng vắt qua vách: viền miếng + vách chớp đỏ 2 lần.</summary>
        public void FlashWallCross(List<Tri> pieceTris, TrayState tray)
        {
            var mb = new MeshBuilder(1);
            mb.Lines(0, Geometry.Boundary(pieceTris), .16f, WarnRed);
            foreach (var w in tray.walls) mb.Line(0, new Vector2(w.x1, w.y1), new Vector2(w.x2, w.y2), .2f, WarnRed);
            mb.Apply(flash);
            PulseAlpha(flashRenderer, .45f, 2);
        }

        /// <summary>Thả thẻ hỏng: đường cắt xám rung tại chỗ rồi mờ.</summary>
        public void PlayRejectCard(MoveResult r)
        {
            if (r == null || r.segs == null || r.segs.Count == 0) return;
            var mb = new MeshBuilder(1);
            foreach (var g in r.segs) mb.DashedLine(0, new Vector2(g.x1, g.y1), new Vector2(g.x2, g.y2), .13f, Palette.CutInvalid, .18f, .12f);
            mb.Apply(flash);
            flashRenderer.transform.localPosition = Vector3.zero;
            PulseAlpha(flashRenderer, .35f, 1);
            flashRenderer.transform.DOShakePosition(.25f, new Vector3(.12f, 0, 0), 24, 0).OnComplete(() => { if (flashRenderer != null) flashRenderer.transform.localPosition = Vector3.zero; }).Guard(flashRenderer.transform);
        }

        void PulseAlpha(MeshRenderer r, float time, int blinks)
        {
            r.KillTweens();
            float a = 1;
            SetAlpha(r, 1);
            var s = DOTween.Sequence().Guard(r);
            for (int i = 0; i < blinks; i++)
            {
                if (i > 0) s.Append(DOTween.To(() => a, v => { a = v; SetAlpha(r, v); }, 1f, .05f));
                s.Append(DOTween.To(() => a, v => { a = v; SetAlpha(r, v); }, 0f, time / blinks));
            }
        }

        void SetAlpha(Renderer r, float a)
        {
            if (r == null) return;
            r.GetPropertyBlock(block);
            block.SetColor("_Color", new Color(1, 1, 1, a));
            r.SetPropertyBlock(block);
        }

        // ======================= Thắng =======================

        /// <summary>
        /// Khoảnh khắc "vừa khít": khay nảy, viền cơm sáng một vòng, rồi nắp hộp sắt trượt xuống đóng lại.
        /// Trả về thời gian (giây) tới khi nắp đóng xong.
        /// </summary>
        /// <summary>
        /// Hiệu ứng thắng: nảy, chớp, nắp hộp đóng. dish != null (màn có món đặc trưng): ảnh món hiện dần lên khay
        /// đúng hình khuôn, giữ một nhịp cho người chơi nhìn rồi nắp mới đóng. Trả về thời điểm nắp đóng xong (giây).
        /// </summary>
        public float PlayWin(TrayState tray, Sprite dish = null)
        {
            float lidAt = .35f;
            if (dish != null)
            {
                // ảnh vẽ phủ khung chữ nhật của khuôn: căn theo bbox khay hiện tại (khay có thể đã bị kéo đi)
                var b = Geometry.BBox(tray.tris);
                dishReveal.sprite = dish;
                dishReveal.enabled = true;
                var size = dish.bounds.size;
                dishReveal.transform.localScale = new Vector3(b.W / size.x, b.H / size.y, 1);
                dishReveal.transform.localPosition = Local(new Vector2(b.minx + b.W / 2f, b.miny + b.H / 2f));
                dishReveal.color = new Color(1, 1, 1, 0);
                dishReveal.KillTweens();
                float al = 0;
                DOTween.To(() => al, v => { al = v; if (dishReveal != null) dishReveal.color = new Color(1, 1, 1, v); }, 1f, .45f)
                    .SetDelay(.15f).Guard(dishReveal);
                lidAt = 1.25f;
            }
            content.KillTweens(true);
            content.DOPunchPosition(new Vector3(0, -.14f, 0), .32f, 7, .5f).Guard(content);
            Flash(tray.tris, new Color(1, 1, 1, .9f), true, .45f);

            BuildLid(tray);
            lidRenderer.KillTweens();
            var t = lidRenderer.transform;
            t.localPosition = new Vector3(0, .9f, 0);
            float a = 0;
            SetAlpha(lidRenderer, 0);
            var s = DOTween.Sequence().Guard(lidRenderer).AppendInterval(lidAt);
            s.Append(t.DOLocalMove(Vector3.zero, .28f).SetEase(Ease.InQuad));
            s.Join(DOTween.To(() => a, v => { a = v; SetAlpha(lidRenderer, v); }, 1f, .18f));
            s.AppendCallback(() => EventBus<LidClosedEvent>.Raise(default));
            s.Append(t.DOPunchPosition(new Vector3(0, -.06f, 0), .18f, 4, .5f));
            return lidAt + .3f;
        }

        void BuildLid(TrayState tray)
        {
            const float rim = 9f / 44f;
            var tris = tray.tris;
            var edge = Geometry.Boundary(tris);
            var mb = new MeshBuilder(1);
            mb.Tris(0, tris, new Color(.35f, .27f, .22f, .25f), new Vector2(0, .14f));
            mb.Lines(0, edge, 2 * rim + .12f, new Color(.35f, .27f, .22f, .25f), new Vector2(0, .14f));
            mb.Tris(0, tris, Palette.TinOutline);
            mb.Lines(0, edge, 2 * rim + .1f, Palette.TinOutline);
            mb.Tris(0, tris, Palette.TinRimShadow);
            mb.Lines(0, edge, 2 * rim, Palette.TinRimShadow);
            mb.Tris(0, tris, Palette.TinRim, new Vector2(0, -.06f));
            mb.Lines(0, edge, 2 * rim - .08f, Palette.TinRim, new Vector2(0, -.06f));
            mb.Apply(lid);
        }
    }
}
