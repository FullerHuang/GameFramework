using UnityEngine;

namespace GameFramework
{
    public class AudioService : IAudioService
    {
        AudioSource _sfxSource;
        AudioSource _bgmSource;

        public AudioService()
        {
            var go = new GameObject("AudioService");
            Object.DontDestroyOnLoad(go);
            _sfxSource = go.AddComponent<AudioSource>();
            _bgmSource = go.AddComponent<AudioSource>();
            _bgmSource.loop = true;
        }

        public void PlaySFX(string clipName)
        {
            var clip = Resources.Load<AudioClip>($"Audio/SFX/{clipName}");
            if (clip != null) _sfxSource.PlayOneShot(clip);
        }

        public void PlayBGM(string clipName)
        {
            var clip = Resources.Load<AudioClip>($"Audio/BGM/{clipName}");
            if (clip != null && _bgmSource.clip?.name != clipName)
            {
                _bgmSource.clip = clip;
                _bgmSource.Play();
            }
        }

        public void SetVolume(float volume)
        {
            _sfxSource.volume = volume;
            _bgmSource.volume = volume;
        }
    }
}
