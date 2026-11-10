using UnityEngine;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Câu nói của khách trong bong bóng "Mẫu" (chỉ là lời thoại cho vui, không phải chữ hướng dẫn).
    /// Mỗi màn chọn ngẫu nhiên một câu từ chitChat + foodTalk, trừ khi LevelData.customerLine có câu riêng.
    /// Sửa câu trực tiếp trong asset Data/Config/CustomerLines (giữ câu ngắn: bong bóng chứa ~2 dòng).
    /// </summary>
    [CreateAssetMenu(menuName = "KoreanGame/Customer Lines", fileName = "CustomerLines")]
    public class CustomerLines : ScriptableObject
    {
        [Tooltip("Tám chuyện với chủ xe: thời tiết, phố, chiếc xe…")]
        [TextArea(1, 3)] public string[] chitChat;
        [Tooltip("Nói về món / hộp cơm mình gọi")]
        [TextArea(1, 3)] public string[] foodTalk;
        [Tooltip("Khi xếp xong hộp (thắng màn)")]
        [TextArea(1, 3)] public string[] win;

        [Header("Gọi món ({0} = tên món, viết thường)")]
        [Tooltip("Màn có món đặc trưng: khách gọi đúng món đó")]
        [TextArea(1, 3)] public string[] dishOrder =
        {
            "Cho mình một hộp {0} nhé!",
            "Hôm nay mình thèm {0} quá!",
            "Nghe nói xe có {0}, làm cho mình một hộp nha!",
        };
        [Tooltip("Màn thường có món phụ: khách nhắc món phụ")]
        [TextArea(1, 3)] public string[] sideOrder =
        {
            "Thêm {0} giúp mình nha!",
            "Có {0} là mình vui rồi!",
            "Hộp cơm kèm {0} là chuẩn bài!",
        };
        [Tooltip("Thắng màn có món đặc trưng")]
        [TextArea(1, 3)] public string[] dishWin =
        {
            "{0} thơm quá, cảm ơn nhé!",
            "Đúng vị {0} luôn, mai mình lại ghé!",
        };
    }
}
