using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;

namespace AW2.Sound
{
    public class SoundInstanceXACT : SoundInstance
    {
        public SoundInstanceXACT(Cue cue, SoundBank soundBank)
        {
            _cue = cue;
            _cueName = cue.Name;
            _soundBank = soundBank;
        }

        public override void Play()
        {
            _cue.Play();
        }

        public override void Stop()
        {
            _cue.Stop(AudioStopOptions.AsAuthored);
        }

        public override void SetVolume(float v)
        {            
        }


        public override void EnsureIsPlaying()
        {
            if (_isDisposed) throw new InvalidOperationException("The sound is disposed");

            if (IsPlaying) return;

            if (_cue != null)
            {
                _cue.Dispose();
            }
            _cue = _soundBank.GetCue(_cueName);
            _cue.Play();
        }

        public override void Dispose()
        {
            if (_cue != null)
            {
                _cue.Dispose();
                _cue = null;
            }
            _isDisposed = true;
        }
        public bool IsPlaying { get { return _cue != null && _cue.IsPlaying; } }

        private Cue _cue;
        private bool _isDisposed = false;
        private string _cueName;
        private SoundBank _soundBank;
    };
}
