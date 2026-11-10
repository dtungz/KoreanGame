using System;
using System.Collections.Generic;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    public enum FoodRegion { None, HaNoi, Seoul }

    /// <summary>Món phụ đặt sẵn trong hộp (FixedFood.kind trỏ tới id). Chiếm nguyên ô, không bị cắt.</summary>
    [Serializable]
    public class SideDish
    {
        [Tooltip("Trùng với FixedFood.kind trong level, ví dụ egg, sausage, meat")]
        public string id;
        public string displayName;
        public FoodRegion region;
        public Sprite sprite;
    }

    /// <summary>Danh mục món ăn: món phụ (theo id) + món đặc trưng. Dùng cho khay, bộ sưu tập, đơn khách.</summary>
    [CreateAssetMenu(menuName = "KoreanGame/Food Catalog", fileName = "FoodCatalog")]
    public class FoodCatalog : ScriptableObject
    {
        public List<SideDish> sideDishes = new List<SideDish>();
        public List<DishData> dishes = new List<DishData>();

        public SideDish FindSide(string id) => sideDishes.Find(s => s != null && s.id == id);

        public Sprite SideSprite(string id) => FindSide(id)?.sprite;
    }
}
