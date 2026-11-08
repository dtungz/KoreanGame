using System;
using System.Collections.Generic;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    [Serializable]
    public class RicePiece
    {
        public List<Tri> tris = new List<Tri>();
        public Vector2Int at;
    }

    [Serializable]
    public class FixedFood
    {
        public string kind;   // egg | sausage | meat
        public List<Vector2Int> cells = new List<Vector2Int>();

        public FixedFood Clone() => new FixedFood { kind = kind, cells = new List<Vector2Int>(cells) };
    }

    [Serializable]
    public class TrayData
    {
        public List<Tri> tris = new List<Tri>();
        public Vector2Int at;
        public List<Wall> walls = new List<Wall>();
        public List<FixedFood> fixedFoods = new List<FixedFood>();
    }

    /// <summary>Một màn chơi. Toạ độ tương đối, dời theo `at` khi đặt lên thớt 7×10.</summary>
    [CreateAssetMenu(menuName = "KoreanGame/Level", fileName = "Level_")]
    public class LevelData : ScriptableObject
    {
        [TextArea(2, 4)] public string hint;
        public List<RicePiece> rice = new List<RicePiece>();
        public TrayData tray = new TrayData();
        public List<CardDef> cards = new List<CardDef>();

        [Header("Meta")]
        public bool isTutorial;
        public bool unlocksSilhouette;

        [Header("Solver (do tool ghi)")]
        public int difficulty;
        public int solutions;
        [TextArea(2, 6)] public string solution;
    }
}
