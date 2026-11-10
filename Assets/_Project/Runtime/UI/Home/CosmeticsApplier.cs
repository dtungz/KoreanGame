using KoreanGame.Core;
using KoreanGame.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Home
{
    /// <summary>
    /// Áp món trang trí đang dùng: màu hộp (Palette.SetTraySkin), màu khung thớt, nền Main Menu.
    /// Món đã chọn nhưng chưa mở (save lệch) thì dùng món mặc định. Không đổi luật chơi.
    /// </summary>
    public class CosmeticsApplier : MonoBehaviour
    {
        [SerializeField] ShopCatalog catalog;
        [SerializeField] Image menuBackground;
        [SerializeField] AspectRatioFitter menuFit;
        [SerializeField] SpriteRenderer boardBg;

        LevelController controller;

        public ShopCatalog Catalog => catalog;

        public void Init(LevelController c)
        {
            controller = c;
            Apply();
        }

        public static bool IsUnlocked(ShopItem item) => item.unlockAfter <= 0 || ProgressSave.UnlockedLevel >= item.unlockAfter;

        public ShopItem Equipped(ShopCategory c)
        {
            var item = catalog.Find(CosmeticsSave.Get(c.ToString()));
            return item != null && item.category == c && IsUnlocked(item) ? item : catalog.Default(c);
        }

        public void Equip(ShopItem item)
        {
            if (item == null || !IsUnlocked(item)) return;
            CosmeticsSave.Set(item.category.ToString(), item.id);
            Apply();
        }

        public void Apply()
        {
            var box = Equipped(ShopCategory.Box);
            if (box != null && box.customBox) Palette.SetTraySkin(box.boxRim, box.boxOutline, box.boxWell);
            else Palette.ResetTraySkin();

            var board = Equipped(ShopCategory.Board);
            if (boardBg != null) boardBg.color = board != null ? board.boardTint : Color.white;

            var theme = Equipped(ShopCategory.Theme);
            if (menuBackground != null && theme != null && theme.menuBackground != null)
            {
                menuBackground.sprite = theme.menuBackground;
                if (menuFit != null) menuFit.aspectRatio = theme.menuBackground.rect.width / theme.menuBackground.rect.height;
            }
            if (controller != null) controller.Redraw();   // vẽ lại khay với màu hộp mới
        }
    }
}
