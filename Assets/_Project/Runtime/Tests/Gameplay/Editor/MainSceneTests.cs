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
        [Test]
        public void AllProjectReferencesAreAssigned()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(SceneBuilder.ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(SceneBuilder.ScenePath, OpenSceneMode.Additive);
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
                                && p.name != "m_Script" && p.name != "m_Material")
                                missing.Add($"{mb.GetType().Name} ({mb.name}).{p.propertyPath}");
                    }
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            Assert.IsEmpty(missing, "Chưa gán tham chiếu, chạy Tools/KoreanGame/Build Main Scene:\n" + string.Join("\n", missing));
        }
    }
}
