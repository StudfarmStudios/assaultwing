namespace AW2.Sound
{
    public class SoundInstanceDummy : SoundInstance
    {
        public override bool IsFinished { get { return true; } }

        public override void Play() { }
        public override void Stop() { }
        public override void Dispose() { }
        public override void EnsureIsPlaying() { }
        public override void SetVolume(float vol) { }
        public override void UpdateSpatial(Microsoft.Xna.Framework.Audio.AudioListener[] listeners) { }
    }
}
