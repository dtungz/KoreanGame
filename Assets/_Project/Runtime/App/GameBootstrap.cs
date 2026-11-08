using KoreanGame.Core;
using KoreanGame.Gameplay;
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

        void Awake()
        {
            Application.targetFrameRate = 60;
            boardView.Init();
            levelController.Init(levelDatabase, boardView, handView);
            boardInput.Init(levelController, boardView, handView);
            hudView.Init(levelController);
            winPopup.Init(levelController);
            gameFeedback.Init(audioManager);
        }

        void Start() => levelController.Load(StartLevel());

        int StartLevel()
        {
#if UNITY_EDITOR
            int forced = UnityEditor.EditorPrefs.GetInt(PlayFromLevelPref, -1);
            if (forced >= 0)
            {
                UnityEditor.EditorPrefs.DeleteKey(PlayFromLevelPref);
                return forced;
            }
#endif
            return ProgressSave.CurrentLevel;
        }
    }
}
