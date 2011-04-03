using System;

namespace AW2.Settings
{
    public class SoundSettings
    {
        public enum EngineType { XNA, XACT };

        private float _soundVolume;
        private float _musicVolume;
        private EngineType _engineType;

        public float SoundVolume { get { return _soundVolume; } set { _soundVolume = value; } }
        public float MusicVolume { get { return _musicVolume; } set { _musicVolume = value; } }
        public EngineType AudioEngineType { get { return _engineType; } set { _engineType = value; } }

        public SoundSettings()
        {
            Reset();
        }

        public void Reset()
        {
            SoundVolume = 0.8f;
            MusicVolume = 0.8f;
            AudioEngineType = EngineType.XNA;
        }
    }
}
