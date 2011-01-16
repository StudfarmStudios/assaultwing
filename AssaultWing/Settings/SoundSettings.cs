using System;

namespace AW2.Settings
{
    public class SoundSettings
    {
        private float _soundVolume;
        private float _musicVolume;

        public float SoundVolume { get { return _soundVolume; } set { _soundVolume = value; } }
        public float MusicVolume { get { return _musicVolume; } set { _musicVolume = value; } }

        public SoundSettings()
        {
            SoundVolume = 0.25f;
            MusicVolume = 0.35f;
        }
    }
}
