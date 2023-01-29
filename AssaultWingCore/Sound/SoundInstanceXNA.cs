using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;

namespace AW2.Sound
{
    public class SoundInstanceXNA : SoundInstance
    {
        private static AudioListener[] DEFAULT_LISTENERS = new[] { new AudioListener() };

        private SoundEffectInstance _instance;
        private Func<Vector2?> _getEmitterPos;
        private Func<Vector2?> _getEmitterMove;
        private AudioEmitter _emitter;
        private float _baseVolume;
        private float _distanceScale;

        public override bool IsFinished { get { return _instance.IsDisposed || _instance.State == SoundState.Stopped; } }

        public SoundInstanceXNA(SoundEffectInstance effect)
        {
            _instance = effect;
        }

        public SoundInstanceXNA(SoundEffectInstance effect, Func<Vector2?> getEmitterPos, Func<Vector2?> getEmitterMove, float baseVolume, float distanceScale)
        {
            _instance = effect;
            _getEmitterPos = getEmitterPos;
            _getEmitterMove = getEmitterMove;
            _emitter = new AudioEmitter();
            _baseVolume = baseVolume;
            _distanceScale = distanceScale;
            SetVolume(1);
            UpdateSpatial(DEFAULT_LISTENERS);
        }

        public override void SetVolume(float vol)
        {
            if (_instance.IsDisposed) return;
            _instance.Volume = _baseVolume * vol * AW2.Core.AssaultWingCore.Instance.Settings.Sound.SoundVolume;
        }

        public override void Play()
        {
            Trace.Assert(!_instance.IsDisposed);
            if (_emitter != null) _instance.Apply3D(new AudioListener(), _emitter);
            _instance.Play();

        }
        public override void Stop()
        {
            if (_instance.IsDisposed) return;
            _instance.Stop();
        }

        public AW2.Game.Gob Gob { get; set; }

        public override void Dispose()
        {
            _instance.Dispose();
            _getEmitterPos = null;
            _getEmitterMove = null;
            _emitter = null;
        }

        public override void UpdateSpatial(AudioListener[] listeners)
        {
            if (_instance.IsDisposed) return;
            var pos = _getEmitterPos();
            var move = _getEmitterMove();
            if (!pos.HasValue || !move.HasValue) return;
            _emitter.Position = new Vector3(pos.Value, 0);
            _emitter.Velocity = new Vector3(move.Value, 0);
            SoundEffect.DistanceScale = _distanceScale;
            SoundEffect.DopplerScale = 0.05f;
            _instance.Apply3D(listeners, _emitter);
        }

        public override void EnsureIsPlaying()
        {
            Trace.Assert(!_instance.IsDisposed);
            if (_instance.State != SoundState.Playing) _instance.Play();
        }
    }
}
