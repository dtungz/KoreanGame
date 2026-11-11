using DG.Tweening;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Home
{
    /// <summary>Chip vàng (đồng xu + số) trên Main Menu. Tự cập nhật khi vàng đổi; tăng thì nảy nhẹ.</summary>
    public class GoldView : MonoBehaviour
    {
        [SerializeField] Text amount;
        [SerializeField] RectTransform chip;

        int shown = -1;

        void OnEnable()
        {
            EventBus<GoldChangedEvent>.Subscribe(OnChanged);
            Set(GoldSave.Gold, false);
        }

        void OnDisable()
        {
            EventBus<GoldChangedEvent>.Unsubscribe(OnChanged);
            this.KillTweens(true);
        }

        void OnChanged(GoldChangedEvent e) => Set(e.gold, true);

        void Set(int gold, bool animate)
        {
            bool up = shown >= 0 && gold > shown;
            shown = gold;
            amount.text = gold.ToString();
            if (animate && up && chip != null)
            {
                chip.DOKill(true);
                chip.DOPunchScale(Vector3.one * .15f, .35f, 6, .5f).Guard(this);
            }
        }
    }
}
