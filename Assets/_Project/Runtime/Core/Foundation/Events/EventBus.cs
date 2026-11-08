using System;

namespace KoreanGame.Core
{
    /// <summary>Bus sự kiện tĩnh theo kiểu: các mảng giao tiếp mà không cần biết nhau.</summary>
    public static class EventBus<T> where T : struct
    {
        static event Action<T> Handlers;

        public static void Subscribe(Action<T> h) => Handlers += h;
        public static void Unsubscribe(Action<T> h) => Handlers -= h;
        public static void Raise(T e) => Handlers?.Invoke(e);
    }
}
