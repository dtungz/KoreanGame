using DG.Tweening;
using KoreanGame.Core;
using KoreanGame.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Home
{
    /// <summary>
    /// Popup Cài đặt (GDD 9.2): bật/tắt âm thanh, rung (lưu lại); trong màn chơi có thêm Chơi lại và Về menu.
    /// Chỉ chứa tính năng đã có trong dự án (AudioManager.Muted, Haptics.Enabled, LevelController.Restart).
    /// </summary>
    public class SettingsPopup : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] RectTransform panel;
        [SerializeField] Button closeButton;
        [SerializeField] Button soundButton;
        [SerializeField] Text soundLabel;
        [SerializeField] Button vibrationButton;
        [SerializeField] Text vibrationLabel;
        [SerializeField] Button restartButton;
        [SerializeField] Button homeButton;
        [Tooltip("Chiều cao panel: mở trong màn chơi (đủ 4 nút) / mở từ menu (chỉ Âm thanh + Rung)")]
        [SerializeField] float inGameHeight = 370, menuHeight = 220;

        LevelController controller;
        AudioManager audioManager;
        BoardInput input;
        MainMenuView menu;
        bool inputBefore;

        public void Init(LevelController c, AudioManager audio, BoardInput boardInput, MainMenuView mainMenu)
        {
            controller = c;
            audioManager = audio;
            input = boardInput;
            menu = mainMenu;
            closeButton.onClick.AddListener(Close);
            soundButton.onClick.AddListener(() => { SettingsSave.Sound = !SettingsSave.Sound; Apply(); });
            vibrationButton.onClick.AddListener(() =>
            {
                SettingsSave.Vibration = !SettingsSave.Vibration;
                Apply();
                if (SettingsSave.Vibration) Haptics.Light();   // rung thử khi vừa bật
            });
            restartButton.onClick.AddListener(() => { Close(); controller.Restart(); });
            homeButton.onClick.AddListener(() => { Close(); menu.Show(); });
            root.SetActive(false);
            Apply();
        }

        /// <summary>inGame = mở từ màn chơi: hiện thêm Chơi lại / Về menu.</summary>
        public void Open(bool inGame)
        {
            restartButton.gameObject.SetActive(inGame);
            homeButton.gameObject.SetActive(inGame && menu != null);
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, inGame ? inGameHeight : menuHeight);   // pivot giữa: vẫn căn giữa
            inputBefore = input.Enabled;
            input.Enabled = false;   // chạm vào popup không lọt xuống thớt / tay bài
            root.SetActive(true);
            this.KillTweens();
            panel.localScale = Vector3.one * .9f;
            panel.DOScale(1f, .2f).SetEase(Ease.OutBack).Guard(this);
        }

        public void Close()
        {
            this.KillTweens(true);
            root.SetActive(false);
            input.Enabled = inputBefore;
        }

        void Apply()
        {
            bool sound = SettingsSave.Sound, vib = SettingsSave.Vibration;
            audioManager.Muted = !sound;
            Haptics.Enabled = vib;
            soundLabel.text = sound ? "Âm thanh: Bật" : "Âm thanh: Tắt";
            vibrationLabel.text = vib ? "Rung: Bật" : "Rung: Tắt";
            soundButton.image.color = sound ? Palette.UiSage : Palette.UiCreamDark;
            vibrationButton.image.color = vib ? Palette.UiSage : Palette.UiCreamDark;
        }

        void OnDisable() => this.KillTweens();
    }
}
