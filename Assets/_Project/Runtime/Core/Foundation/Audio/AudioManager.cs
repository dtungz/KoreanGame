using DG.Tweening;
using UnityEngine;

namespace KoreanGame.Core
{
    /// <summary>Thứ tự = thứ tự mảng clips. Chỉ thêm vào cuối. Tên file: sfx_ + tên viết thường, nối bằng "_" (PopupOpen → sfx_popup_open).</summary>
    public enum Sfx { Cut, Tick, Drop, Bad, Rotate, Pop, Lid, Win, Click, Tab, PopupOpen, PopupClose, Coin, Buy, Hint, NewDish, NewSide }

    public enum Music { None, Menu, Game }

    /// <summary>
    /// Phát hiệu ứng âm thanh qua một nhóm AudioSource đặt sẵn trong scene (xoay vòng) và nhạc nền (1 nguồn riêng, chuyển bài thì mờ dần).
    /// Clip gán bằng menu Tools/KoreanGame/Audio/Relink Clips (tìm theo tên file trong Source/Audio). Thiếu clip nào thì tiếng đó im.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] AudioSource[] sources;
        [SerializeField] AudioClip[] clips = new AudioClip[8];
        [SerializeField, Range(0, 1)] float volume = .8f;

        [Header("Nhạc nền")]
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip menuMusic;
        [SerializeField] AudioClip gameMusic;
        [SerializeField, Range(0, 1)] float musicVolume = .2f;
        [SerializeField] float musicFade = .6f;

        static AudioManager instance;
        int next;
        bool muted;
        Music current;
        Tween musicTween;

        public static AudioManager Instance => instance != null ? instance : (instance = FindObjectOfType<AudioManager>());

        /// <summary>Phát tiếng UI từ bất kỳ đâu (nút, shop, gợi ý…). Không có AudioManager trong scene thì bỏ qua.</summary>
        public static void PlayUi(Sfx sfx, float volumeScale = 1f)
        {
            var a = Instance;
            if (a != null) a.Play(sfx, .02f, volumeScale);
        }

        public static string FileName(Sfx s)
        {
            var name = s.ToString();
            var sb = new System.Text.StringBuilder("sfx_");
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i])) sb.Append('_');
                sb.Append(char.ToLowerInvariant(name[i]));
            }
            return sb.ToString();
        }

        public bool Muted
        {
            get => muted;
            set { muted = value; if (musicSource != null) musicSource.mute = value; }
        }

        void Awake() { if (instance == null) instance = this; }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            musicTween?.Kill();
        }

        public void Play(Sfx sfx, float pitchJitter = .04f, float volumeScale = 1f)
        {
            if (Muted || sources == null || sources.Length == 0) return;
            int i = (int)sfx;
            if (clips == null || i >= clips.Length || clips[i] == null) return;
            var src = sources[next];
            next = (next + 1) % sources.Length;
            if (src == null) return;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(clips[i], volume * volumeScale);
        }

        /// <summary>Hạ nhỏ nhạc nền một lúc (tiếng thắng màn nổi lên) rồi to dần lại.</summary>
        public void Duck(float seconds, float to = .25f)
        {
            if (musicSource == null || !musicSource.isPlaying) return;
            musicTween?.Kill();
            var seq = DOTween.Sequence().SetUpdate(true);
            seq.Append(DOTween.To(() => musicSource.volume, v => musicSource.volume = v, musicVolume * to, .15f));
            seq.AppendInterval(seconds);
            seq.Append(DOTween.To(() => musicSource.volume, v => musicSource.volume = v, musicVolume, .8f));
            musicTween = seq;
        }

        /// <summary>Đổi nhạc nền: bài cũ mờ dần, bài mới to dần. Cùng bài thì giữ nguyên (không phát lại từ đầu).</summary>
        public void PlayMusic(Music m)
        {
            if (musicSource == null || m == current) return;
            current = m;
            var clip = m == Music.Menu ? menuMusic : m == Music.Game ? gameMusic : null;
            musicTween?.Kill();
            var seq = DOTween.Sequence().SetUpdate(true);
            if (musicSource.isPlaying)
                seq.Append(DOTween.To(() => musicSource.volume, v => musicSource.volume = v, 0f, musicFade * .5f));
            seq.AppendCallback(() =>
            {
                musicSource.Stop();
                musicSource.clip = clip;
                musicSource.volume = 0;
                if (clip != null) musicSource.Play();
            });
            if (clip != null)
                seq.Append(DOTween.To(() => musicSource.volume, v => musicSource.volume = v, musicVolume, musicFade));
            musicTween = seq;
        }
    }
}
