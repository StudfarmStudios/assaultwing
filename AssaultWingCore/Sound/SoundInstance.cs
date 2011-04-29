using Microsoft.Xna.Framework.Audio;

namespace AW2.Sound
{
    public abstract class SoundInstance
    {
        public abstract bool IsFinished { get; }

        public abstract void Play();
        public abstract void Stop();
        public abstract void Dispose();
        public abstract void EnsureIsPlaying();
        public abstract void SetVolume(float vol);
        public abstract void UpdateSpatial(AudioListener[] listeners);
    };
}
