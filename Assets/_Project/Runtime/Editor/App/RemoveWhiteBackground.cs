using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KoreanGame.EditorTools
{
    /// <summary>
    /// Tách nền trắng của ảnh sinh từ AI (vật thể đứng riêng trên nền trắng) thành nền trong suốt, không cần thư viện ngoài.
    /// Lan từ mép ảnh vào: chỉ vùng gần-trắng NỐI LIỀN với mép bị xoá, nên màu trắng bên trong hình
    /// (áo trắng, mái sọc trắng…) được giữ nhờ nét viền bao quanh. Viền hình được làm mềm và khử quầng trắng.
    /// Ảnh gốc được sao lưu ra ArtSource/ (ngoài Assets, Unity không import); chạy lại sẽ luôn xử lý từ bản gốc.
    /// </summary>
    public static class RemoveWhiteBackground
    {
        const string MenuPath = "Tools/KoreanGame/Art/Remove White Background";
        const string BackupRoot = "ArtSource";
        const int Tolerance = 24;   // điểm ảnh coi là nền: mọi kênh màu ≥ 255 - Tolerance (chừa nhiễu JPEG)
        const int SoftRange = 70;   // viền hình: độ "tối" 0..SoftRange ↦ độ đục 0..1

        [MenuItem(MenuPath)]
        static void Run()
        {
            int done = 0;
            foreach (var path in SelectedImages())
            {
                try
                {
                    var outPath = Process(path);
                    Debug.Log($"[RemoveWhiteBackground] {path} → {outPath}");
                    done++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[RemoveWhiteBackground] {path}: {e.Message}");
                }
            }
            AssetDatabase.Refresh();
            Debug.Log($"[RemoveWhiteBackground] Đã xử lý {done} ảnh. Bản gốc ở thư mục {BackupRoot}/ (ngoài Assets).");
        }

        [MenuItem(MenuPath, true)]
        static bool Validate()
        {
            foreach (var _ in SelectedImages()) return true;
            return false;
        }

        static IEnumerable<string> SelectedImages()
        {
            foreach (var o in Selection.objects)
            {
                var p = AssetDatabase.GetAssetPath(o);
                var ext = Path.GetExtension(p).ToLowerInvariant();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") yield return p;
            }
        }

        /// <summary>Xử lý 1 ảnh, trả về đường dẫn PNG đã ghi.</summary>
        static string Process(string assetPath)
        {
            // sao lưu bản gốc một lần; lần sau luôn đọc từ bản gốc để chạy lại không làm hỏng thêm
            var backup = Path.Combine(BackupRoot, assetPath).Replace('\\', '/');
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!File.Exists(backup)) File.Copy(assetPath, backup);

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(backup))) throw new IOException("không đọc được ảnh");
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();

            // 1. lan từ mép: nền = gần trắng và nối liền với mép
            var bg = new bool[px.Length];
            var queue = new Queue<int>();
            void Seed(int i) { if (!bg[i] && NearWhite(px[i])) { bg[i] = true; queue.Enqueue(i); } }
            for (int x = 0; x < w; x++) { Seed(x); Seed((h - 1) * w + x); }
            for (int y = 0; y < h; y++) { Seed(y * w); Seed(y * w + w - 1); }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue(), x = i % w, y = i / w;
                if (x > 0) Seed(i - 1);
                if (x < w - 1) Seed(i + 1);
                if (y > 0) Seed(i - w);
                if (y < h - 1) Seed(i + w);
            }

            // 2. xoá nền; điểm ảnh sát nền thì làm mềm và khử quầng trắng (tách màu trộn với nền trắng)
            var output = new Color32[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (bg[i]) { output[i] = new Color32(c.r, c.g, c.b, 0); continue; }
                int x = i % w, y = i / w;
                bool edge = (x > 0 && bg[i - 1]) || (x < w - 1 && bg[i + 1]) || (y > 0 && bg[i - w]) || (y < h - 1 && bg[i + w]);
                if (!edge) { output[i] = new Color32(c.r, c.g, c.b, 255); continue; }
                int dark = 255 - Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                float a = Mathf.Clamp01(dark / (float)SoftRange);
                output[i] = a <= 0 ? new Color32(c.r, c.g, c.b, 0)
                    : new Color32(Unmix(c.r, a), Unmix(c.g, a), Unmix(c.b, a), (byte)Mathf.RoundToInt(a * 255));
            }
            tex.SetPixels32(output);
            tex.Apply();

            // ảnh JPG: ghi PNG cùng tên bên cạnh (không xoá file gốc của bạn); PNG: ghi đè
            var outPath = Path.ChangeExtension(assetPath, ".png");
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            return outPath;
        }

        static bool NearWhite(Color32 c) => c.r >= 255 - Tolerance && c.g >= 255 - Tolerance && c.b >= 255 - Tolerance;

        /// <summary>Màu thật của điểm ảnh viền: c = a·màu + (1 - a)·trắng.</summary>
        static byte Unmix(byte c, float a) => (byte)Mathf.Clamp(Mathf.RoundToInt((c - (1 - a) * 255f) / a), 0, 255);
    }
}
