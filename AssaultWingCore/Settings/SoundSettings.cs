using System;

namespace AW2.Settings
{
    public class SoundSettings
    {
        public float SoundVolume { get; set; }
        public float MusicVolume { get; set; }

        public SoundSettings()
        {
            Reset();
        }

        public void Reset()
        {
            SoundVolume = 0.8f;
            MusicVolume = 0.8f;
        }
    }
}
