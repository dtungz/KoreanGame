using System.Collections.Generic;
using KoreanGame.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoreanGame.Tests
{
    /// <summary>
    /// Scene Main phải được dựng lại sau khi thêm field vào component (Tools/KoreanGame/Build Main Scene).
    /// Test này bắt lỗi kiểu "UnassignedReferenceException: flashMesh of BoardView has not been assigned".
    /// </summary>
    public class MainSceneTests
    {
        // Main (SceneBuilder) và Bento Journey (BentoJourneyBuilder); scene chưa dựng thì bỏ qua
        static readonly string[] Scenes = { SceneBuilder.ScenePath, BentoJourneyBuilder.ScenePath };

        // tham chiếu tuỳ chọn, cố ý để trống ở scene Main (Main vào thẳng màn chơi, không có menu)
        static readonly HashSet<string> OptionalInMain = new HashSet<string>
        {
            "GameBootstrap.mainMenu", "GameBootstrap.settingsPopup", "GameBootstrap.settingsButton",
            "GameBootstrap.customerChat", "CardView.selectRing", "TrayView.catalog", "GameBootstrap.dishPopup", "GameBootstrap.cosmetics", "GameBootstrap.sideDishToast", "GameBootstrap.tutorial", "GameBootstrap.hintButton",
            "AudioManager.musicSource", "GameBootstrap.localization", "GameBootstrap.koreanFont",
        };

        // tham chiếu tuỳ chọn ở mọi scene (hiện chưa dùng ở đâu)
        static readonly HashSet<string> OptionalAnywhere = new HashSet<string> { "GameBootstrap.targetPreview", "HudView.cardsLeftText", "AudioManager.menuMusic", "AudioManager.gameMusic" };

        // tiếng UI (từ Sfx.Click trở đi) chờ file âm thanh: để trống thì tiếng đó im, không lỗi
        static bool OptionalClip(string key)
        {
            const string pre = "AudioManager.clips.Array.data[";
            return key.StartsWith(pre) && int.TryParse(key.Substring(pre.Length).TrimEnd(']'), out int i) && i >= (int)KoreanGame.Core.Sfx.Click;
        }

        static void RequireScene(string path)
        {
            if (!System.IO.File.Exists(path)) Assert.Ignore($"Chưa dựng {path}");
        }

        [TestCaseSource(nameof(Scenes))]
        public void AllProjectReferencesAreAssigned(string path)
        {
            RequireScene(path);
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var missing = new List<string>();
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (mb == null) { missing.Add($"{root.name}: script bị mất"); continue; }
                        var ns = mb.GetType().Namespace;
                        if (ns == null || !ns.StartsWith("KoreanGame")) continue;
                        var p = new SerializedObject(mb).GetIterator();
                        while (p.NextVisible(true))
                            if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null
                                && p.name != "m_Script" && p.name != "m_Material"
                                && !OptionalAnywhere.Contains($"{mb.GetType().Name}.{p.propertyPath}") && !OptionalClip($"{mb.GetType().Name}.{p.propertyPath}")
                                && !(path == SceneBuilder.ScenePath && OptionalInMain.Contains($"{mb.GetType().Name}.{p.propertyPath}")))
                                missing.Add($"{mb.GetType().Name} ({mb.name}).{p.propertyPath}");
                    }
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            Assert.IsEmpty(missing, $"{path}: chưa gán tham chiếu, dựng lại scene (Tools/KoreanGame):\n" + string.Join("\n", missing));
        }

        [TestCaseSource(nameof(Scenes))]
        public void SceneHasExactlyOneAudioListener(string path)
        {
            RequireScene(path);
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            int count = 0;
            try
            {
                foreach (var root in scene.GetRootGameObjects()) count += root.GetComponentsInChildren<AudioListener>(true).Length;
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            Assert.AreEqual(1, count, $"{path} cần đúng 1 AudioListener (trên Main Camera)");
        }
    }
}
