namespace KoreanGame.Core
{
    /// <summary>Đã nạp một level (index tính từ 0).</summary>
    public struct LevelLoadedEvent
    {
        public int index;
        public int total;
    }

    /// <summary>Người chơi vừa thắng level.</summary>
    public struct LevelWonEvent
    {
        public int index;
        public bool isLast;
        /// <summary>Thắng lần đầu màn có món đặc trưng: vừa mở khoá món mới (hiện popup giới thiệu).</summary>
        public bool newDish;
    }

    /// <summary>Trạng thái thớt / tay bài đổi (cắt, kéo, hoàn tác...).</summary>
    public struct BoardChangedEvent { }

    public enum CardEffect { Cut, Glue, Transform, Copy }

    /// <summary>Một thẻ vừa được dùng thành công.</summary>
    public struct CardUsedEvent
    {
        public CardEffect effect;
    }

    /// <summary>Chạm thẻ để xoay thẻ.</summary>
    public struct CardRotatedEvent { }

    /// <summary>Thả miếng cơm / khay thành công. intoTray = miếng vừa vào nằm gọn trong khay.</summary>
    public struct PiecePlacedEvent
    {
        public bool intoTray;
        public bool movedTray;
    }

    /// <summary>Thao tác bị từ chối (thả thẻ hỏng, thả miếng sai chỗ).</summary>
    public struct MoveRejectedEvent { }

    /// <summary>Khay đã kín nhưng có miếng cơm vắt qua vách ngăn (chưa thắng).</summary>
    public struct WallBlockedEvent { }

    /// <summary>Main Menu vừa đóng, người chơi nhìn thấy màn chơi.</summary>
    public struct MenuClosedEvent { }

    /// <summary>Hoàn tác một bước.</summary>
    public struct UndoEvent { }

    /// <summary>Nắp hộp vừa đóng trong hiệu ứng thắng.</summary>
    public struct LidClosedEvent { }
}
