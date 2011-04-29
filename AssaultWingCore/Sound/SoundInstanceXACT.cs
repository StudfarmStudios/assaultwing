using System;
using Microsoft.Xna.Framework.Audio;

namespace AW2.Sound
{
    public class SoundInstanceXACT : SoundInstance
    {
        private Cue _cue;
        private bool _isDisposed;
        private string _cueName;
        private SoundBank _soundBank;

        public override bool IsFinished { get { return _cue == null || !_cue.IsPlaying; } }

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

            if (!IsFinished) return;

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

        public override void UpdateSpatial(AudioListener[] listeners)
        {
            // TODO
        }
    };
}
