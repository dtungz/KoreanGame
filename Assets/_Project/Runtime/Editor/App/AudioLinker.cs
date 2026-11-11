using System.Collections.Generic;
using System.IO;
using KoreanGame.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoreanGame.EditorTools
{
    /// <summary>
    /// Gán clip cho AudioManager theo tên file trong Source/Audio (mọi thư mục con, đuôi wav / ogg / mp3 đều được):
    /// sfx_click, sfx_tab, sfx_popup_open, … (xem enum Sfx) và bgm_menu, bgm_game. Thả file đúng tên vào rồi chạy menu là xong.
    /// </summary>
    public static class AudioLinker
    {
        public const string Root = "Assets/_Project/Source/Audio";
        public const string MenuMusic = "bgm_menu", GameMusic = "bgm_game";

        [MenuItem("Tools/KoreanGame/Audio/Relink Clips")]
        static void RelinkMenu()
        {
            var mgr = Object.FindObjectOfType<AudioManager>(true);
            if (mgr == null) { EditorUtility.DisplayDialog("Audio", "Scene đang mở không có AudioManager.", "OK"); return; }
            var missing = Link(mgr);
            EditorSceneManager.MarkSceneDirty(mgr.gameObject.scene);
            Debug.Log(missing.Count == 0 ? "[Audio] Đủ tất cả clip." : "[Audio] Còn thiếu: " + string.Join(", ", missing) + $" (thả vào {Root})");
        }

        /// <summary>Gán clip + tạo nguồn nhạc nền nếu chưa có. Trả về danh sách tên file còn thiếu.</summary>
        public static List<string> Link(AudioManager mgr)
        {
            var found = Index();
            var missing = new List<string>();
            AudioClip Get(string name)
            {
                if (found.TryGetValue(name, out var c)) return c;
                missing.Add(name);
                return null;
            }

            var kinds = (Sfx[])System.Enum.GetValues(typeof(Sfx));
            var clips = new AudioClip[kinds.Length];
            foreach (var s in kinds) clips[(int)s] = Get(AudioManager.FileName(s));

            var so = new SerializedObject(mgr);
            var arr = so.FindProperty("clips");
            arr.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];

            var music = so.FindProperty("musicSource");
            if (music.objectReferenceValue == null)
            {
                var t = mgr.transform.Find("Music");
                var go = t != null ? t.gameObject : new GameObject("Music");
                go.transform.SetParent(mgr.transform, false);
                var src = go.GetComponent<AudioSource>();
                if (src == null) src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = true;
                src.spatialBlend = 0;
                src.volume = 0;
                music.objectReferenceValue = src;
            }
            so.FindProperty("menuMusic").objectReferenceValue = StreamMusic(Get(MenuMusic));
            so.FindProperty("gameMusic").objectReferenceValue = StreamMusic(Get(GameMusic));
            so.ApplyModifiedPropertiesWithoutUndo();
            return missing;
        }

        /// <summary>Nhạc nền dài: phát dạng Streaming (không giải nén cả bài vào RAM), Vorbis chất lượng 60%.</summary>
        static AudioClip StreamMusic(AudioClip clip)
        {
            if (clip == null) return null;
            var path = AssetDatabase.GetAssetPath(clip);
            if (!(AssetImporter.GetAtPath(path) is AudioImporter imp)) return clip;
            var s = imp.defaultSampleSettings;
            if (s.loadType == AudioClipLoadType.Streaming && s.compressionFormat == AudioCompressionFormat.Vorbis) return clip;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = .6f;
            imp.defaultSampleSettings = s;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static Dictionary<string, AudioClip> Index()
        {
            var map = new Dictionary<string, AudioClip>();
            if (!AssetDatabase.IsValidFolder(Root)) return map;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (!map.ContainsKey(name)) map[name] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            return map;
        }
    }
}
