using KoreanGame.Core;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    /// <summary>Nghe sự kiện gameplay → phát âm thanh + rung. Không có chữ trên màn hình (GDD 9.2).</summary>
    public class GameFeedback : MonoBehaviour
    {
        AudioManager audioManager;

        public void Init(AudioManager audio)
        {
            audioManager = audio;
            EventBus<CardUsedEvent>.Subscribe(OnCardUsed);
            EventBus<CardRotatedEvent>.Subscribe(OnRotated);
            EventBus<PiecePlacedEvent>.Subscribe(OnPlaced);
            EventBus<MoveRejectedEvent>.Subscribe(OnRejected);
            EventBus<UndoEvent>.Subscribe(OnUndo);
            EventBus<LevelWonEvent>.Subscribe(OnWon);
            EventBus<LidClosedEvent>.Subscribe(OnLid);
        }

        void OnDestroy()
        {
            EventBus<CardUsedEvent>.Unsubscribe(OnCardUsed);
            EventBus<CardRotatedEvent>.Unsubscribe(OnRotated);
            EventBus<PiecePlacedEvent>.Unsubscribe(OnPlaced);
            EventBus<MoveRejectedEvent>.Unsubscribe(OnRejected);
            EventBus<UndoEvent>.Unsubscribe(OnUndo);
            EventBus<LevelWonEvent>.Unsubscribe(OnWon);
            EventBus<LidClosedEvent>.Unsubscribe(OnLid);
        }

        void Play(Sfx s, float pitchJitter = .04f, float vol = 1f) { if (audioManager != null) audioManager.Play(s, pitchJitter, vol); }

        void OnCardUsed(CardUsedEvent e)
        {
            if (e.effect == CardEffect.Cut) { Play(Sfx.Cut, .08f); Haptics.Light(); }
            else { Play(Sfx.Pop); Haptics.Light(); }
        }

        void OnRotated(CardRotatedEvent e) => Play(Sfx.Rotate, .02f, .7f);

        void OnPlaced(PiecePlacedEvent e)
        {
            if (e.intoTray) { Play(Sfx.Tick, .06f); Haptics.Light(); }
            else Play(Sfx.Drop, .06f, .8f);
        }

        void OnRejected(MoveRejectedEvent e) { Play(Sfx.Bad, .03f, .9f); Haptics.Medium(); }

        void OnUndo(UndoEvent e) => Play(Sfx.Drop, .02f, .6f);

        void OnWon(LevelWonEvent e) { Play(Sfx.Tick, 0); Haptics.Heavy(); }

        void OnLid(LidClosedEvent e) { Play(Sfx.Lid, .02f); Play(Sfx.Win, 0, .9f); Haptics.Medium(); }
    }
}
