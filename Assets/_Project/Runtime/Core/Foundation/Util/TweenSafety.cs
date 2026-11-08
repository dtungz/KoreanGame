using DG.Tweening;
using UnityEngine;

namespace KoreanGame.Core
{
    /// <summary>
    /// Quy tắc DOTween của dự án (xem .claude/CodingConventions.md):
    /// mọi tween phải gọi Guard(owner) để có target (DOTween.Kill(owner) dừng được) và tự Kill khi owner bị Destroy;
    /// mỗi view Kill tween của mình trong OnDisable / OnDestroy. Tránh warning "target destroyed" / tween chạy trên object đã huỷ.
    /// </summary>
    public static class TweenSafety
    {
        /// <summary>Gắn tween vào owner: target = owner, tự Kill khi GameObject của owner bị huỷ. An toàn với tween / owner null.</summary>
        public static T Guard<T>(this T tween, Component owner) where T : Tween
        {
            if (tween == null) return null;
            if (owner == null) { tween.Kill(); return null; }
            tween.SetTarget(owner);
            tween.SetLink(owner.gameObject, LinkBehaviour.KillOnDestroy);
            return tween;
        }

        /// <summary>Kill mọi tween có target là owner (bỏ qua nếu owner đã null / bị huỷ).</summary>
        public static void KillTweens(this Component owner, bool complete = false)
        {
            if (owner == null) return;
            DOTween.Kill(owner, complete);
        }
    }
}
