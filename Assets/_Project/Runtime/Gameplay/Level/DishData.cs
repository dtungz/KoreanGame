using UnityEngine;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Món đặc trưng của một vùng (bún chả, kimbap…). Mở khoá ở level có trỏ tới món (LevelData.unlocksDish):
    /// thắng lần đầu thì hiện popup giới thiệu và thêm vào bộ sưu tập. Chỉ là hình + chữ, không ảnh hưởng luật chơi.
    /// </summary>
    [CreateAssetMenu(menuName = "KoreanGame/Dish", fileName = "Dish_")]
    public class DishData : ScriptableObject
    {
        [Tooltip("Mã cố định, không đổi sau khi phát hành (dùng để lưu bộ sưu tập)")]
        public string id;
        public string displayName;
        public FoodRegion region;
        [Tooltip("Ảnh hộp đầy đủ, hiện trong popup món mới")]
        public Sprite art;
        [Tooltip("Ảnh nhỏ cho bộ sưu tập / đơn khách")]
        public Sprite icon;
        [TextArea(2, 5)] public string intro;
    }
}
