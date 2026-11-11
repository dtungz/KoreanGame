using DG.Tweening;
using KoreanGame.Core;
using KoreanGame.Gameplay;
using KoreanGame.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Home
{
    /// <summary>
    /// Main Menu (GDD 9.1) với 3 tab dưới cùng: Hành trình · Trang chủ · Cửa hàng. Xe đồ ăn dùng chung cho cả 3 trang.
    /// Đổi tab: xe chạy theo hướng đầu xe (sang trái) ra khỏi màn hình, trang đổi, xe chạy vào từ bên phải rồi đỗ
    /// ở chỗ của trang mới (~0,6 giây). Bấm tab lần nữa trong lúc chạy thì tới cuối hiệu ứng ngay.
    /// Bấm Chơi: xe chạy đi, menu mờ, vào màn chơi đã nạp sẵn (không đổi scene).
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        public const int JourneyTab = 0, HomeTab = 1, ShopTab = 2;

        [SerializeField] GameObject root;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform truck;
        [SerializeField] Button playButton;
        [SerializeField] Text playLabel;
        [SerializeField] Button settingsButton;
        [Tooltip("Thứ tự: Hành trình, Trang chủ, Cửa hàng")]
        [SerializeField] Button[] tabs;
        [SerializeField] CanvasGroup[] pages;
        [Tooltip("Chỗ xe đỗ (vị trí + kích thước) ở từng trang, cùng cha với xe")]
        [SerializeField] RectTransform[] truckSpots;
        [SerializeField] JourneyView journey;
        [SerializeField] ShopView shop;

        const float FrameW = 390, Off = 220;   // xe ra hẳn khỏi khung 390 đơn vị (cả màn hình rộng hơn khung)

        LevelController controller;
        BoardInput input;
        int current = HomeTab;
        bool leaving, switching;

        public void Init(LevelController c, BoardInput boardInput, SettingsPopup settings)
        {
            controller = c;
            input = boardInput;
            playButton.onClick.AddListener(Play);
            for (int i = 0; i < tabs.Length; i++)
            {
                int k = i;
                tabs[i].onClick.AddListener(() => SelectTab(k));
                ButtonPress.SetSound(tabs[i], Sfx.Tab);
            }
            shop.Init();
            EventBus<LanguageChangedEvent>.Subscribe(OnLanguage);
            journey.Init(PlayLevel);
            if (settings != null) settingsButton.onClick.AddListener(() => settings.Open(false));
            root.SetActive(false);
        }

        public void Show()
        {
            this.KillTweens();
            leaving = switching = false;
            // vừa chơi lại màn cũ (sổ hành trình) rồi về menu: "Chơi tiếp" quay lại màn đang tới
            if (controller.Index != ProgressSave.CurrentLevel && ProgressSave.IsCleared(controller.Index))
                controller.Load(ProgressSave.CurrentLevel);
            root.SetActive(true);
            EventBus<MenuOpenedEvent>.Raise(default);
            group.alpha = 1;
            group.blocksRaycasts = true;
            SetPage(HomeTab);
            Park(HomeTab);
            playLabel.text = LocNames.Level(controller.Index + 1);
            input.Enabled = false;   // chạm vào menu không lọt xuống tay bài / thớt bên dưới
            StartIdle();
        }

        void SelectTab(int k)
        {
            if (leaving) return;
            if (switching) this.KillTweens(true);   // đang chạy: tới cuối hiệu ứng ngay
            if (k == current) return;
            switching = true;
            this.KillTweens();                      // dừng nhún

            var from = truck.anchoredPosition;
            var exit = new Vector2(-truck.sizeDelta.x - Off, from.y);
            var spot = truckSpots[k];
            var target = spot.anchoredPosition;
            var enter = new Vector2(FrameW + Off, target.y);
            var oldPage = pages[current];
            var newPage = pages[k];

            float p1 = 0, p2 = 0, f1 = 1, f2 = 0;
            var s = DOTween.Sequence().Guard(this);
            // xe chạy theo hướng đầu xe (sang trái) ra khỏi màn hình, trang cũ mờ đi
            s.Append(DOTween.To(() => p1, v => { p1 = v; if (truck != null) truck.anchoredPosition = Vector2.LerpUnclamped(from, exit, v); }, 1f, .28f).SetEase(Ease.InQuad));
            s.Join(DOTween.To(() => f1, v => { f1 = v; if (oldPage != null) oldPage.alpha = v; }, 0f, .15f));
            // đổi trang khi xe đã khuất; xe đổi cỡ theo chỗ đỗ mới rồi vào từ bên phải
            s.AppendCallback(() =>
            {
                SetPage(k);
                newPage.alpha = 0;
                truck.sizeDelta = spot.sizeDelta;
                truck.anchoredPosition = enter;
            });
            s.Append(DOTween.To(() => p2, v => { p2 = v; if (truck != null) truck.anchoredPosition = Vector2.LerpUnclamped(enter, target, v); }, 1f, .3f).SetEase(Ease.OutQuad));
            s.Join(DOTween.To(() => f2, v => { f2 = v; if (newPage != null) newPage.alpha = v; }, 1f, .2f));
            s.OnComplete(() =>
            {
                switching = false;
                StartIdle();
            });
        }

        void SetPage(int k)
        {
            current = k;
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].gameObject.SetActive(i == k);
                pages[i].alpha = 1;
                tabs[i].image.color = i == k ? Palette.UiPeach : Color.white;   // tab đang mở: nền đào
            }
            if (k == JourneyTab) journey.Refresh();
            if (k == ShopTab) shop.Open();
        }

        void Park(int k)
        {
            truck.sizeDelta = truckSpots[k].sizeDelta;
            truck.anchoredPosition = truckSpots[k].anchoredPosition;
        }

        /// <summary>Xe nổ máy: nhún nhẹ liên tục tại chỗ đỗ.</summary>
        void StartIdle()
        {
            var home = truck.anchoredPosition;
            float y = 0;
            DOTween.To(() => y, v => { y = v; if (truck != null) truck.anchoredPosition = home + new Vector2(0, v); }, 3f, .6f)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).Guard(this);
        }

        /// <summary>Chơi màn chọn trong sổ hành trình: nạp màn đó rồi xe chạy đi như nút Chơi.</summary>
        void PlayLevel(int level)
        {
            if (leaving) return;
            controller.Load(level);
            playLabel.text = LocNames.Level(controller.Index + 1);
            Play();
        }

        void Play()
        {
            if (leaving) { this.KillTweens(true); return; }   // bấm lần nữa: tới cuối hiệu ứng ngay
            if (switching) this.KillTweens(true);
            leaving = true;
            this.KillTweens();
            group.blocksRaycasts = false;   // xe đang chạy đi: chạm lọt xuống màn chơi (nút Cài đặt, Gợi ý… bấm được ngay)
            var from = truck.anchoredPosition;
            var exit = new Vector2(-truck.sizeDelta.x - Off, from.y);   // theo hướng đầu xe
            float p = 0;
            var s = DOTween.Sequence().Guard(this);
            s.Append(DOTween.To(() => p, v => { p = v; if (truck != null) truck.anchoredPosition = Vector2.LerpUnclamped(from, exit, v); }, 1f, .45f).SetEase(Ease.InBack));
            s.Insert(.2f, DOTween.To(() => group.alpha, a => { if (group != null) group.alpha = a; }, 0f, .3f));
            s.OnComplete(Hide);
        }

        void Hide()
        {
            leaving = false;
            if (root != null) root.SetActive(false);
            if (input != null) input.Enabled = true;
            EventBus<MenuClosedEvent>.Raise(default);
        }

        void OnDisable() => this.KillTweens();

        void OnDestroy() => EventBus<LanguageChangedEvent>.Unsubscribe(OnLanguage);

        // đổi ngôn ngữ trong Cài đặt mở từ menu: vẽ lại nút Chơi và trang đang mở
        void OnLanguage(LanguageChangedEvent e)
        {
            if (controller == null) return;
            playLabel.text = LocNames.Level(controller.Index + 1);
            if (current == JourneyTab) journey.Refresh();
            if (current == ShopTab) shop.Redraw();
        }
    }
}
