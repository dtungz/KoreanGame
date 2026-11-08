using UnityEngine;

namespace KoreanGame.Core
{
    public enum Sfx { Cut, Tick, Drop, Bad, Rotate, Pop, Lid, Win }

    /// <summary>
    /// Phát hiệu ứng âm thanh qua một nhóm AudioSource đặt sẵn trong scene (xoay vòng).
    /// Clip đánh theo thứ tự enum Sfx. Âm hiện tại là âm tạm tự tổng hợp (Source/Audio/Gameplay_SFX), thay file là xong.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] AudioSource[] sources;
        [SerializeField] AudioClip[] clips = new AudioClip[8];
        [SerializeField, Range(0, 1)] float volume = .8f;

        int next;
        public bool Muted { get; set; }

        public void Play(Sfx sfx, float pitchJitter = .04f, float volumeScale = 1f)
        {
            if (Muted || sources == null || sources.Length == 0) return;
            int i = (int)sfx;
            if (i >= clips.Length || clips[i] == null) return;
            var src = sources[next];
            next = (next + 1) % sources.Length;
            if (src == null) return;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(clips[i], volume * volumeScale);
        }
    }
}
