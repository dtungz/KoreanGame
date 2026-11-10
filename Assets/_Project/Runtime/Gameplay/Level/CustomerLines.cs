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
    }
}
