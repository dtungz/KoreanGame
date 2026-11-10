using KoreanGame.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Home
{
    /// <summary>
    /// Tab Cửa hàng (vỏ giao diện): xe đỗ ở trên (xe dùng chung của menu), 3 tab danh mục, lưới vật phẩm "Sắp có".
    /// Chưa có tiền tệ / vật phẩm / mua bán: thêm khi có thiết kế cho chúng. Chuyển trang do MainMenuView lo.
    /// </summary>
    public class ShopView : MonoBehaviour
    {
        [SerializeField] Button[] tabs;
        [SerializeField] Text[] tabLabels;

        public void Init()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int k = i;
                tabs[i].onClick.AddListener(() => Select(k));
            }
        }

        /// <summary>Mỗi lần vào tab Cửa hàng: về danh mục đầu.</summary>
        public void Open() => Select(0);

        void Select(int k)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].image.color = i == k ? Palette.UiOrange : Palette.UiCream;
                tabLabels[i].color = Palette.UiText;   // style guide: chữ mực cả trên nền cam
            }
        }
    }
}
