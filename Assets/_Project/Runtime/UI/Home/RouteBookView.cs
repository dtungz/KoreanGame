using System;
using KoreanGame.Core;
using KoreanGame.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Home
{
    /// <summary>
    /// Sổ hành trình: mỗi trang một chặng (20 màn), các màn nối thành đường đi. Xe đỗ ở màn đang tới.
    /// Màn đã qua và màn đang tới chạm được để chơi (chơi lại không làm lùi tiến độ); màn chưa mở mờ đi.
    /// Màn mở khoá món đặc trưng có icon món nhỏ bên cạnh.
    /// </summary>
    public class RouteBookView : MonoBehaviour
    {
        [SerializeField] LevelDatabase database;
        [SerializeField] Button[] nodes;
        [SerializeField] Image[] nodeImages;
        [SerializeField] Text[] nodeLabels;
        [SerializeField] Image[] dishBadges;
        [SerializeField] RectTransform truckMarker;
        [SerializeField] Text pageTitle;
        [SerializeField] Button prevButton;
        [SerializeField] Button nextButton;
        [SerializeField] Sprite clearedSprite;
        [SerializeField] Sprite currentSprite;
        [Tooltip("Màn có món chưa mở: huy hiệu ổ khoá (không lộ món)")]
        [SerializeField] Sprite lockedBadge;

        Action<int> play;
        int page;
        bool wired;

        int PerPage => nodes.Length;
        int PageCount => Mathf.Max(1, (database.Count + PerPage - 1) / PerPage);
        int Frontier => Mathf.Min(ProgressSave.UnlockedLevel, database.Count - 1);   // màn xa nhất chơi được

        public void Init(Action<int> onPlay) => play = onPlay;

        /// <summary>Mở sổ ở trang có màn đang tới.</summary>
        public void Refresh()
        {
            if (!wired) Wire();
            page = Frontier / PerPage;
            Draw();
        }

        void Wire()
        {
            wired = true;
            prevButton.onClick.AddListener(() => { page = Mathf.Max(0, page - 1); Draw(); });
            nextButton.onClick.AddListener(() => { page = Mathf.Min(PageCount - 1, page + 1); Draw(); });
            for (int i = 0; i < nodes.Length; i++)
            {
                int k = i;
                nodes[i].onClick.AddListener(() =>
                {
                    int level = page * PerPage + k;
                    if (level <= Frontier) play?.Invoke(level);
                });
            }
        }

        void Draw()
        {
            int first = page * PerPage, last = Mathf.Min(first + PerPage, database.Count) - 1;
            var region = database.levels[first].region;
            pageTitle.text = $"{(region == FoodRegion.Seoul ? "Seoul" : "Hà Nội")} · Màn {first + 1}–{last + 1}";
            prevButton.interactable = page > 0;
            nextButton.interactable = page < PageCount - 1;
            int frontier = Frontier;
            truckMarker.gameObject.SetActive(false);
            for (int i = 0; i < nodes.Length; i++)
            {
                int level = first + i;
                bool exists = level <= last;
                nodes[i].gameObject.SetActive(exists);
                if (!exists) continue;
                bool open = level <= frontier, current = level == frontier;
                nodes[i].interactable = open;
                nodeImages[i].sprite = current ? currentSprite : clearedSprite;
                nodeImages[i].color = open ? Color.white : new Color(1, 1, 1, .45f);
                nodeLabels[i].text = (level + 1).ToString();
                nodeLabels[i].color = open ? Palette.UiText : new Color(Palette.UiText.r, Palette.UiText.g, Palette.UiText.b, .45f);
                var dish = database.levels[level].unlocksDish;
                dishBadges[i].gameObject.SetActive(dish != null);
                if (dish != null)
                {
                    bool got = ProgressSave.IsCleared(level) || CollectionSave.HasDish(dish.id);
                    dishBadges[i].sprite = got ? dish.icon : lockedBadge;   // chưa mở: chỉ biết "có món ở đây"
                    dishBadges[i].color = Color.white;
                }
                if (current)
                {
                    truckMarker.gameObject.SetActive(true);
                    var n = (RectTransform)nodes[i].transform;
                    truckMarker.anchoredPosition = n.anchoredPosition + new Vector2(n.sizeDelta.x / 2 - truckMarker.sizeDelta.x / 2, truckMarker.sizeDelta.y - 6);
                }
            }
        }
    }
}
