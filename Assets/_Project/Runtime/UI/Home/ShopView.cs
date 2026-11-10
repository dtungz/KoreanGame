using KoreanGame.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Home
{
    /// <summary>
    /// Tab Cửa hàng: 3 danh mục (Hộp cơm, Thớt, Theme), mỗi danh mục vài món trang trí.
    /// Món mở theo tiến độ (qua đủ màn), không dùng tiền. Chạm món đã mở để dùng ngay; món chưa mở ghi "Qua Màn X".
    /// </summary>
    public class ShopView : MonoBehaviour
    {
        [Tooltip("Thứ tự: Hộp cơm, Thớt, Theme (trùng ShopCategory)")]
        [SerializeField] Button[] tabs;
        [SerializeField] Text[] tabLabels;
        [SerializeField] CosmeticsApplier applier;
        [SerializeField] Button[] itemButtons;
        [SerializeField] Image[] itemIcons;
        [SerializeField] Text[] itemNames;
        [SerializeField] Text[] itemStates;
        [SerializeField] GameObject[] itemLocks;

        int current;
        ShopItem[] shown;

        public void Init()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int k = i;
                tabs[i].onClick.AddListener(() => Select(k));
            }
            for (int i = 0; i < itemButtons.Length; i++)
            {
                int k = i;
                itemButtons[i].onClick.AddListener(() => OnItem(k));
            }
        }

        /// <summary>Mỗi lần vào tab Cửa hàng: về danh mục đầu.</summary>
        public void Open() => Select(0);

        void Select(int k)
        {
            current = k;
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].image.color = i == k ? Palette.UiOrange : Palette.UiCream;
                tabLabels[i].color = Palette.UiText;   // style guide: chữ mực cả trên nền cam
            }
            Draw();
        }

        void Draw()
        {
            var cat = (ShopCategory)current;
            shown = applier.Catalog.Of(cat).ToArray();
            var equipped = applier.Equipped(cat);
            for (int i = 0; i < itemButtons.Length; i++)
            {
                bool has = i < shown.Length;
                itemButtons[i].gameObject.SetActive(has);
                if (!has) continue;
                var it = shown[i];
                bool open = CosmeticsApplier.IsUnlocked(it), using_ = it == equipped;
                itemIcons[i].sprite = it.icon;
                itemIcons[i].color = open ? it.iconTint : new Color(it.iconTint.r, it.iconTint.g, it.iconTint.b, .35f);
                itemNames[i].text = it.displayName;
                itemStates[i].text = using_ ? "Đang dùng" : open ? "Dùng" : $"Qua Màn {it.unlockAfter}";
                itemStates[i].color = using_ ? Palette.UiSage : open ? Palette.UiOrange : Palette.UiBrown;
                itemLocks[i].SetActive(!open);
                itemButtons[i].image.color = using_ ? Palette.UiPeach : Color.white;
            }
        }

        void OnItem(int i)
        {
            if (shown == null || i >= shown.Length) return;
            applier.Equip(shown[i]);
            Draw();
        }
    }
}
