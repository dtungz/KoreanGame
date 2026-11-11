using System;
using System.Collections.Generic;
using KoreanGame.Core;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    /// <summary>Chỗ tutorial trỏ tới. Piece: index ≥ 0 theo thứ tự miếng; PieceLargestOutside / Second / Smallest chọn theo cỡ miếng ngoài khay.</summary>
    public enum TutTarget { None, Card, PieceLargestOutside, PieceSecondOutside, PieceSmallestOutside, TrayEmpty, Wall, FixedFood, QueueChip, Undo }

    public enum TutGesture { None, Tap, Drag }

    /// <summary>Điều kiện qua bước (đều là event có sẵn của game).</summary>
    public enum TutDone { CardUsed, CardRotated, PiecePlaced, PieceIntoTray, Win, Timer }

    [Serializable]
    public class TutorialStep
    {
        [Tooltip("Key trong localization.csv, ví dụ tut.cut_drag.step1")]
        public string key;
        [TextArea(1, 2)] public string vi;
        [TextArea(1, 2)] public string ko;

        [Header("Trỏ vào")]
        public TutTarget target;
        [Tooltip("Card: ô thẻ (0–2), hoặc tìm theo loại thẻ nếu byKind")]
        public int slot;
        public bool byKind;
        public CardKind cardKind;
        public TutGesture gesture;
        [Tooltip("Drag: kéo tới đâu")]
        public TutTarget dragTo;

        [Header("Qua bước khi")]
        public TutDone done;
        [Tooltip("CardUsed: chỉ tính đúng loại hiệu ứng")]
        public bool anyEffect = true;
        public CardEffect effect;
        public float timer = 2.5f;
        [Tooltip("Chỉ hiện khi trên tay có thẻ loại cardKind (ví dụ đợi thẻ Nhân bản từ hàng chờ vào)")]
        public bool waitForCard;
    }

    /// <summary>Một tutorial giới thiệu mechanic: chạy ở màn levelIndex (từ 0) nếu chưa xem. Dạy bằng cách làm, không chặn thao tác.</summary>
    [CreateAssetMenu(menuName = "KoreanGame/Tutorial", fileName = "Tut_")]
    public class TutorialDef : ScriptableObject
    {
        [Tooltip("Mã cố định, dùng làm cờ đã xem (PlayerPrefs tut.<id>)")]
        public string id;
        public int levelIndex;
        public List<TutorialStep> steps = new List<TutorialStep>();
    }
}
