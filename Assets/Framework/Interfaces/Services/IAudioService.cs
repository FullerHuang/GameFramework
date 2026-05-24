namespace GameFramework
{
    public interface IAudioService : IService
    {
        void PlaySFX(string clipName);
        void PlayBGM(string clipName);
        void SetVolume(float volume);
    }
}
