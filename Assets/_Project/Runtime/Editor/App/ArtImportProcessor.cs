using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace KoreanGame.EditorTools
{
    /// <summary>
    /// Thiết lập import cho ảnh trong Source/Art: Sprite, không mipmap, viền 9-slice lấy từ slicing.json.
    /// rice_texture là texture lặp (Repeat) để phủ vân cơm lên mesh.
    /// </summary>
    public class ArtImportProcessor : AssetPostprocessor
    {
        const string Root = "Assets/_Project/Source/Art/";
        const string SlicingPath = Root + "slicing.json";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var ti = (TextureImporter)assetImporter;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;

            if (assetPath.EndsWith("rice_texture.png"))
            {
                ti.textureType = TextureImporterType.Default;
                ti.wrapMode = TextureWrapMode.Repeat;
                return;
            }

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.spritePixelsPerUnit = assetPath.EndsWith("board.png") ? 88 : 100;

            var rel = assetPath.Substring(Root.Length);
            var border = ReadBorder(rel);
            if (border.HasValue)
            {
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteBorder = border.Value;
                ti.SetTextureSettings(settings);
            }
        }

        /// <summary>slicing.json: "đường/dẫn.png": [L, B, R, T].</summary>
        static Vector4? ReadBorder(string rel)
        {
            if (!File.Exists(SlicingPath)) return null;
            var json = File.ReadAllText(SlicingPath);
            var m = Regex.Match(json, "\"" + Regex.Escape(rel) + "\"\\s*:\\s*\\[\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)\\s*\\]");
            if (!m.Success) return null;
            return new Vector4(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value));
        }

        [MenuItem("Tools/KoreanGame/Art/Reimport Art")]
        public static void ReimportAll()
        {
            AssetDatabase.ImportAsset("Assets/_Project/Source/Art", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }
    }
}
