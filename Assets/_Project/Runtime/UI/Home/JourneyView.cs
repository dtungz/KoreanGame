using System;
using KoreanGame.Core;
using KoreanGame.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Home
{
    /// <summary>
    /// Tab Hành trình: bộ sưu tập món ăn. Món đặc trưng (hàng Hà Nội, hàng Seoul) + món phụ đã gặp.
    /// Mở khi có trong CollectionSave hoặc đã qua màn chứa món (save cũ chưa có bộ sưu tập vẫn thấy đủ).
    /// Chạm một ô để đọc giới thiệu ở dải dưới cùng. Chỉ đọc dữ liệu, không có luật chơi mới.
    /// Tab con thứ hai là Sổ hành trình (RouteBookView): chọn màn đã qua để chơi lại.
    /// </summary>
    public class JourneyView : MonoBehaviour
    {
        [SerializeField] FoodCatalog catalog;
        [SerializeField] LevelDatabase database;
        [Tooltip("Cùng thứ tự với catalog.dishes")]
        [SerializeField] Button[] dishCells;
        [SerializeField] Image[] dishIcons;
        [SerializeField] Text[] dishLabels;
        [SerializeField] GameObject[] dishLocks;
        [Tooltip("Cùng thứ tự với catalog.sideDishes")]
        [SerializeField] Button[] sideCells;
        [SerializeField] Image[] sideIcons;
        [SerializeField] Text dishCount;
        [SerializeField] Text sideCount;
        [SerializeField] Text detailText;
        [Tooltip("Món chưa mở: hiện ổ khoá thay cho icon (giữ bí mật món)")]
        [SerializeField] Sprite lockedIcon;

        [Header("Tab con: Bộ sưu tập / Sổ hành trình")]
        [SerializeField] Button collectionTab;
        [SerializeField] Button bookTab;
        [SerializeField] GameObject collectionRoot;
        [SerializeField] GameObject bookRoot;
        [SerializeField] RouteBookView book;

        static readonly Color Locked = new Color(Palette.UiBrown.r, Palette.UiBrown.g, Palette.UiBrown.b, .4f);

        bool wired, showBook;

        /// <summary>onPlay(level): chơi màn chọn trong sổ hành trình (MainMenuView nạp màn và đóng menu).</summary>
        public void Init(Action<int> onPlay)
        {
            book.Init(onPlay);
            collectionTab.onClick.AddListener(() => ShowTab(false));
            bookTab.onClick.AddListener(() => ShowTab(true));
        }

        void ShowTab(bool bookPage)
        {
            showBook = bookPage;
            collectionRoot.SetActive(!bookPage);
            bookRoot.SetActive(bookPage);
            collectionTab.image.color = bookPage ? Color.white : Palette.UiPeach;
            bookTab.image.color = bookPage ? Palette.UiPeach : Color.white;
            if (bookPage) book.Refresh();
        }

        public void Refresh()
        {
            ShowTab(showBook);
            if (!wired) Wire();
            int open = 0, seen = 0;
            for (int i = 0; i < dishCells.Length && i < catalog.dishes.Count; i++)
            {
                var d = catalog.dishes[i];
                bool has = DishOpen(d, out int lvl);
                if (has) open++;
                // chưa mở: chỉ ổ khoá mờ ở giữa ô, không lộ hình món
                dishIcons[i].sprite = has ? d.icon : lockedIcon;
                dishIcons[i].color = has ? Color.white : Locked;
                dishIcons[i].rectTransform.localScale = has ? Vector3.one : Vector3.one * .5f;
                dishLocks[i].SetActive(false);
                dishLabels[i].text = has ? d.Name() : lvl >= 0 ? LocNames.Level(lvl + 1) : "???";
            }
            for (int i = 0; i < sideCells.Length && i < catalog.sideDishes.Count; i++)
            {
                bool has = SideSeen(catalog.sideDishes[i].id);
                if (has) seen++;
                sideIcons[i].sprite = catalog.sideDishes[i].sprite;
                sideIcons[i].color = has ? Color.white : Locked;
            }
            dishCount.text = $"{open}/{Mathf.Min(dishCells.Length, catalog.dishes.Count)}";
            sideCount.text = $"{seen}/{Mathf.Min(sideCells.Length, catalog.sideDishes.Count)}";
            detailText.text = Loc.T("journey.tap_hint");
        }

        void Wire()
        {
            wired = true;
            for (int i = 0; i < dishCells.Length; i++)
            {
                int k = i;
                dishCells[i].onClick.AddListener(() => ShowDish(k));
            }
            for (int i = 0; i < sideCells.Length; i++)
            {
                int k = i;
                sideCells[i].onClick.AddListener(() => ShowSide(k));
            }
        }

        void ShowDish(int i)
        {
            if (i >= catalog.dishes.Count) return;
            var d = catalog.dishes[i];
            detailText.text = DishOpen(d, out int lvl)
                ? $"<b>{d.Name()}</b> · {LocNames.Region(d.region)}\n{d.Intro()}"
                : $"<b>{Loc.T("journey.secret")}</b> · {LocNames.Region(d.region)}\n{Loc.T("journey.unlock_at", lvl + 1)}";
        }

        void ShowSide(int i)
        {
            if (i >= catalog.sideDishes.Count) return;
            var s = catalog.sideDishes[i];
            detailText.text = SideSeen(s.id)
                ? $"<b>{s.Name()}</b> · {(s.region == FoodRegion.None ? Loc.T("journey.side_common") : Loc.T("journey.side_region", LocNames.Region(s.region)))}"
                : $"<b>{Loc.T("journey.side_unknown")}</b>\n{Loc.T("journey.side_unknown_hint")}";
        }

        bool DishOpen(DishData d, out int level)
        {
            level = -1;
            for (int i = 0; database != null && i < database.Count; i++)
                if (database.levels[i] != null && database.levels[i].unlocksDish == d) { level = i; break; }
            return CollectionSave.HasDish(d.id) || (level >= 0 && ProgressSave.IsCleared(level));
        }

        bool SideSeen(string id)
        {
            if (CollectionSave.HasSide(id)) return true;
            for (int i = 0; database != null && i < database.Count; i++)
            {
                if (!ProgressSave.IsCleared(i)) continue;
                foreach (var f in database.levels[i].tray.fixedFoods) if (f.kind == id) return true;
            }
            return false;
        }
    }
}
