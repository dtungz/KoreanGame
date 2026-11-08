using System.Collections.Generic;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    /// <summary>Dãy level liên tục (không chapter). Thứ tự trong list = thứ tự chơi.</summary>
    [CreateAssetMenu(menuName = "KoreanGame/Level Database", fileName = "LevelDatabase")]
    public class LevelDatabase : ScriptableObject
    {
        public List<LevelData> levels = new List<LevelData>();

        public int Count => levels.Count;
        public LevelData Get(int i) => levels.Count == 0 ? null : levels[Wrap(i)];
        public int Wrap(int i) => levels.Count == 0 ? 0 : ((i % levels.Count) + levels.Count) % levels.Count;
    }
}
