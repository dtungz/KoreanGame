using DG.Tweening;
using KoreanGame.Core;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Nghe sự kiện gameplay → phát âm thanh + rung. Không có chữ trên màn hình (GDD 9.2).
    /// Thêm: vàng giảm (mua đồ / gợi ý trả phí) → Buy; thắng → Coin + NewDish lúc popup hiện; nhạc nền Menu / Game theo menu đóng mở.
    /// </summary>
    public class GameFeedback : MonoBehaviour
    {
        AudioManager audioManager;
        int gold = -1;
        bool menuOpen;

        public void Init(AudioManager audio)
        {
            audioManager = audio;
            EventBus<CardUsedEvent>.Subscribe(OnCardUsed);
            EventBus<CardRotatedEvent>.Subscribe(OnRotated);
            EventBus<PiecePlacedEvent>.Subscribe(OnPlaced);
            EventBus<MoveRejectedEvent>.Subscribe(OnRejected);
            EventBus<WallBlockedEvent>.Subscribe(OnWallBlocked);
            EventBus<UndoEvent>.Subscribe(OnUndo);
            EventBus<LevelWonEvent>.Subscribe(OnWon);
            EventBus<LidClosedEvent>.Subscribe(OnLid);
            EventBus<GoldChangedEvent>.Subscribe(OnGold);
            EventBus<MenuOpenedEvent>.Subscribe(OnMenuOpened);
            EventBus<MenuClosedEvent>.Subscribe(OnMenuClosed);
            EventBus<LevelLoadedEvent>.Subscribe(OnLoaded);
            gold = GoldSave.Gold;
        }

        void OnDestroy()
        {
            EventBus<CardUsedEvent>.Unsubscribe(OnCardUsed);
            EventBus<CardRotatedEvent>.Unsubscribe(OnRotated);
            EventBus<PiecePlacedEvent>.Unsubscribe(OnPlaced);
            EventBus<MoveRejectedEvent>.Unsubscribe(OnRejected);
            EventBus<WallBlockedEvent>.Unsubscribe(OnWallBlocked);
            EventBus<UndoEvent>.Unsubscribe(OnUndo);
            EventBus<LevelWonEvent>.Unsubscribe(OnWon);
            EventBus<LidClosedEvent>.Unsubscribe(OnLid);
            EventBus<GoldChangedEvent>.Unsubscribe(OnGold);
            EventBus<MenuOpenedEvent>.Unsubscribe(OnMenuOpened);
            EventBus<MenuClosedEvent>.Unsubscribe(OnMenuClosed);
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLoaded);
            this.KillTweens();
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

        void OnWallBlocked(WallBlockedEvent e) { Play(Sfx.Bad, .03f, .9f); Haptics.Medium(); }

        void OnUndo(UndoEvent e) => Play(Sfx.Drop, .02f, .6f);

        void OnWon(LevelWonEvent e)
        {
            Play(Sfx.Tick, 0);
            Haptics.Heavy();
            float at = e.popupDelay > 0 ? e.popupDelay : 1f;   // lúc popup Hoàn thành / Món mới hiện
            if (e.newDish) DOVirtual.DelayedCall(at, () => Play(Sfx.NewDish, 0)).Guard(this);
            if (e.gold > 0) DOVirtual.DelayedCall(at + (e.newDish ? .6f : .25f), () => Play(Sfx.Coin, .03f)).Guard(this);
        }

        // vàng tăng khi thắng đã có Coin ở trên; ở đây chỉ bắt vàng giảm (mua đồ trong shop, gợi ý trả phí)
        void OnGold(GoldChangedEvent e)
        {
            if (gold >= 0 && e.gold < gold) Play(Sfx.Buy, .02f, .7f);
            gold = e.gold;
        }

        void OnMenuOpened(MenuOpenedEvent e) { menuOpen = true; SetMusic(Music.Menu); }

        void OnMenuClosed(MenuClosedEvent e) { menuOpen = false; SetMusic(Music.Game); }

        // scene không có menu (Main) hoặc vào thẳng màn: nhạc chơi bắt đầu khi màn được nạp
        void OnLoaded(LevelLoadedEvent e) { if (!menuOpen) SetMusic(Music.Game); }

        void SetMusic(Music m) { if (audioManager != null) audioManager.PlayMusic(m); }

        void OnLid(LidClosedEvent e) { Play(Sfx.Lid, .02f); Play(Sfx.Win, 0); if (audioManager != null) audioManager.Duck(2f); Haptics.Medium(); }
    }
}
