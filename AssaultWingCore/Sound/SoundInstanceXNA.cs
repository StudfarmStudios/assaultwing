using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;

namespace AW2.Sound
{
    public class SoundInstanceXNA : SoundInstance
    {
        public SoundInstanceXNA(SoundEffectInstance effect)
        {
            _instance = effect;
        }

        public override void Play()
        {
            _instance.Play();
        }
        public override void Stop()
        {
            _instance.Stop();
        }

        public override void SetGob(AW2.Game.Gob gob)
        {
            _gob = gob;
        }

        public override void Dispose()
        {
            _instance.Dispose();
        }

        public override void EnsureIsPlaying()
        {
            if (_instance.State != SoundState.Playing)
            {
                _instance.Play();
            }
        }

        private SoundEffectInstance _instance;
        private AW2.Game.Gob _gob;
    }
}
