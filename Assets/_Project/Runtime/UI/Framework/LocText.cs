using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Shared
{
    /// <summary>Nhãn cố định (tiêu đề, tên nút…): lấy chữ từ bảng dịch theo key, đổi ngôn ngữ thì tự vẽ lại.</summary>
    [RequireComponent(typeof(Text))]
    public class LocText : MonoBehaviour
    {
        [SerializeField] string key;

        Text text;

        public string Key
        {
            get => key;
            set { key = value; Apply(); }
        }

        void OnEnable()
        {
            EventBus<LanguageChangedEvent>.Subscribe(OnChanged);
            Apply();
        }

        void OnDisable() => EventBus<LanguageChangedEvent>.Unsubscribe(OnChanged);

        void OnChanged(LanguageChangedEvent e) => Apply();

        void Apply()
        {
            if (string.IsNullOrEmpty(key) || !Loc.Has(key)) return;
            if (text == null) text = GetComponent<Text>();
            text.text = Loc.T(key);
        }
    }
}
