using DG.Tweening;
using KoreanGame.Core;
using KoreanGame.Gameplay;
using KoreanGame.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Home
{
    /// <summary>
    /// Tab Cửa hàng: 3 danh mục (Hộp cơm, Thớt, Theme), mỗi danh mục vài món trang trí.
    /// Món mở theo tiến độ (qua đủ màn) rồi mua bằng vàng: chưa mở ghi "Qua Màn X"; đã mở chưa mua hiện giá (đồng xu);
    /// chạm để mua (thiếu vàng thì thẻ rung); đã có thì chạm để dùng.
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
        [Tooltip("Đồng xu cạnh giá (hiện khi món đã mở, chưa mua)")]
        [SerializeField] GameObject[] itemCoins;

        int current;
        ShopItem[] shown;

        public void Init()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int k = i;
                tabs[i].onClick.AddListener(() => Select(k));
                ButtonPress.SetSound(tabs[i], Sfx.Tab);
            }
            for (int i = 0; i < itemButtons.Length; i++)
            {
                int k = i;
                itemButtons[i].onClick.AddListener(() => OnItem(k));
            }
        }

        /// <summary>Vẽ lại danh mục đang mở (đổi ngôn ngữ).</summary>
        public void Redraw() { if (shown != null) Draw(); }

        /// <summary>Mỗi lần vào tab Cửa hàng: về danh mục đầu.</summary>
        public void Open() => Select(0);

        bool Equip(ShopItem it)
        {
            applier.Equip(it);
            return true;
        }

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
                bool open = CosmeticsApplier.IsUnlocked(it), owned = CosmeticsApplier.IsOwned(it), using_ = it == equipped;
                itemIcons[i].sprite = it.icon;
                itemIcons[i].color = open ? it.iconTint : new Color(it.iconTint.r, it.iconTint.g, it.iconTint.b, .35f);
                itemNames[i].text = it.Name();
                bool forSale = open && !owned;
                itemStates[i].text = using_ ? Loc.T("shop.using") : forSale ? it.price.ToString() : open ? Loc.T("shop.use") : Loc.T("shop.unlock_at", it.unlockAfter);
                itemStates[i].color = using_ ? Palette.UiSage : forSale ? Palette.UiText : open ? Palette.UiOrange : Palette.UiBrown;
                if (itemCoins != null && i < itemCoins.Length && itemCoins[i] != null) itemCoins[i].SetActive(forSale);
                itemLocks[i].SetActive(!open);
                itemButtons[i].image.color = using_ ? Palette.UiPeach : Color.white;
            }
        }

        void OnItem(int i)
        {
            if (shown == null || i >= shown.Length) return;
            var it = shown[i];
            bool ok = !CosmeticsApplier.IsUnlocked(it) ? false
                : !CosmeticsApplier.IsOwned(it) ? applier.Buy(it)
                : Equip(it);
            if (!ok)
            {
                AudioManager.PlayUi(Sfx.Bad, .8f);   // mua thành công thì GameFeedback phát Buy (vàng giảm)
                var rt = (RectTransform)itemButtons[i].transform;
                rt.DOKill(true);
                rt.DOShakePosition(.35f, new Vector3(8, 0, 0), 20, 0).Guard(this);
            }
            Draw();
        }
    }
}
