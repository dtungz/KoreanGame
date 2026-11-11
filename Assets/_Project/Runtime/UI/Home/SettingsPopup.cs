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
        [Tooltip("Tuỳ chọn: nút đổi ngôn ngữ (Việt → 한국어 → English)")]
        [SerializeField] Button languageButton;
        [SerializeField] Text languageLabel;
        [SerializeField] Button restartButton;
        [SerializeField] Button homeButton;
        [Tooltip("Chiều cao panel: mở trong màn chơi (đủ 5 nút) / mở từ menu (Âm thanh, Rung, Ngôn ngữ)")]
        [SerializeField] float inGameHeight = 438, menuHeight = 288;

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
            if (languageButton != null) languageButton.onClick.AddListener(() => { Loc.Current = Loc.Next(Loc.Current); Apply(); });
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
            AudioManager.PlayUi(Sfx.PopupOpen);
            this.KillTweens();
            panel.localScale = Vector3.one * .9f;
            panel.DOScale(1f, .2f).SetEase(Ease.OutBack).Guard(this);
        }

        public void Close()
        {
            if (root.activeSelf) AudioManager.PlayUi(Sfx.PopupClose);
            this.KillTweens(true);
            root.SetActive(false);
            input.Enabled = inputBefore;
        }

        void Apply()
        {
            bool sound = SettingsSave.Sound, vib = SettingsSave.Vibration;
            audioManager.Muted = !sound;
            Haptics.Enabled = vib;
            soundLabel.text = Loc.T(sound ? "ui.sound_on" : "ui.sound_off");
            vibrationLabel.text = Loc.T(vib ? "ui.vibration_on" : "ui.vibration_off");
            if (languageLabel != null) languageLabel.text = Loc.T("ui.language_value", Loc.T("ui.lang." + Loc.Current.ToString().ToLowerInvariant()));
            soundButton.image.color = sound ? Palette.UiSage : Palette.UiCreamDark;
            vibrationButton.image.color = vib ? Palette.UiSage : Palette.UiCreamDark;
        }

        void OnDisable() => this.KillTweens();
    }
}
