using KoreanGame.Core;
using KoreanGame.Gameplay;
using KoreanGame.Home;
using UnityEngine;

namespace KoreanGame.App
{
    /// <summary>Composition root: nơi duy nhất Init các thành phần trong scene Main.</summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        public const string PlayFromLevelPref = "KoreanGame.PlayFromLevel";

        [SerializeField] LevelDatabase levelDatabase;
        [SerializeField] LevelController levelController;
        [SerializeField] BoardInput boardInput;
        [SerializeField] BoardView boardView;
        [SerializeField] HandView handView;
        [SerializeField] HudView hudView;
        [SerializeField] WinPopup winPopup;
        [SerializeField] AudioManager audioManager;
        [SerializeField] GameFeedback gameFeedback;
        [Tooltip("Tuỳ chọn: scene Main không có menu (vào thẳng màn chơi); scene BentoJourney có")]
        [SerializeField] MainMenuView mainMenu;
        [Tooltip("Tuỳ chọn: popup Cài đặt và nút mở nó trên HUD (chỉ scene BentoJourney)")]
        [SerializeField] SettingsPopup settingsPopup;
        [SerializeField] UnityEngine.UI.Button settingsButton;
        [Tooltip("Tuỳ chọn: hình Mẫu (khay thu nhỏ) trên HUD")]
        [SerializeField] TargetPreview targetPreview;
        [Tooltip("Tuỳ chọn: lời thoại của khách trong bong bóng Mẫu")]
        [SerializeField] CustomerChat customerChat;
        [Tooltip("Tuỳ chọn: popup Món mới khi mở khoá món đặc trưng")]
        [SerializeField] DishPopup dishPopup;

        bool skipMenu;   // Play from here (Level Browser): vào thẳng level, không hiện menu

        void Awake()
        {
            Application.targetFrameRate = 60;
            boardView.Init();
            levelController.Init(levelDatabase, boardView, handView);
            boardInput.Init(levelController, boardView, handView);
            hudView.Init(levelController);
            winPopup.Init(levelController);
            gameFeedback.Init(audioManager);
            // cài đặt đã lưu (mặc định bật) áp cho mọi scene, kể cả scene Main không có popup
            audioManager.Muted = !SettingsSave.Sound;
            Haptics.Enabled = SettingsSave.Vibration;
            if (settingsPopup != null) settingsPopup.Init(levelController, audioManager, boardInput, mainMenu);
            if (settingsPopup != null && settingsButton != null) settingsButton.onClick.AddListener(() => settingsPopup.Open(true));
            if (mainMenu != null) mainMenu.Init(levelController, boardInput, settingsPopup);
            if (targetPreview != null) targetPreview.Init(levelController);
            if (customerChat != null) customerChat.Init(levelController);
            if (dishPopup != null) dishPopup.Init(levelController);
        }

        void Start()
        {
            levelController.Load(StartLevel());
            if (mainMenu != null && !skipMenu) mainMenu.Show();
        }

        int StartLevel()
        {
#if UNITY_EDITOR
            int forced = UnityEditor.EditorPrefs.GetInt(PlayFromLevelPref, -1);
            if (forced >= 0)
            {
                UnityEditor.EditorPrefs.DeleteKey(PlayFromLevelPref);
                skipMenu = true;
                return forced;
            }
#endif
            return ProgressSave.CurrentLevel;
        }
    }
}
