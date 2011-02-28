using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;
using AW2.Core;
using System.Diagnostics;

namespace AW2.Sound
{
    public class SoundInstanceXNA : SoundInstance
    {
        public SoundInstanceXNA(SoundEffectInstance effect)
        {
            _instance = effect;
        }  
        
        public SoundInstanceXNA(SoundEffectInstance effect, AW2.Game.Gob gob, float baseVolume, float distanceScale)
        {
            _instance = effect;
            _gob = gob;
            _emitter = new AudioEmitter();
            _baseVolume = baseVolume;
            
            SetVolume(1);
            UpdateSpatial(DEFAULT_LISTENERS);

            _distanceScale = distanceScale;
        }

        public override void SetVolume(float vol)
        {
            Trace.Assert(!_instance.IsDisposed);

            _instance.Volume = _baseVolume * vol * AssaultWingCore.Instance.Settings.Sound.SoundVolume;
        }

        public override void Play()
        {
            Trace.Assert(!_instance.IsDisposed);

            if (_emitter != null)
            {
                _instance.Apply3D(new AudioListener(), _emitter);
            }
            _instance.Play();
            
        }
        public override void Stop()
        {
            Trace.Assert(!_instance.IsDisposed);
            _instance.Stop();
        }

        public AW2.Game.Gob Gob { get; set; }

        public override void Dispose()
        {
            _instance.Dispose();
            _gob = null;
            _emitter = null;
        }
        public bool IsFinished()
        {
            return _instance.IsDisposed || _instance.State == SoundState.Stopped;
        }

        public void UpdateSpatial(AudioListener[] listeners)
        {
            if (_gob != null && !_instance.IsDisposed)
            {
                _emitter.Position = new Vector3(_gob.Pos.X, _gob.Pos.Y, 0);
                _emitter.Velocity = new Vector3(_gob.Move.X, _gob.Move.Y, 0);

                SoundEffect.DistanceScale = _distanceScale;
                SoundEffect.DopplerScale = 0.05f;
                _instance.Apply3D(listeners, _emitter);

            }
        }
                    
        public override void EnsureIsPlaying()
        {
            Trace.Assert(!_instance.IsDisposed);
            if (_instance.State != SoundState.Playing)
            {
                _instance.Play();
            }
        }

        private SoundEffectInstance _instance;   
        private AW2.Game.Gob _gob;
        private AudioEmitter _emitter;
        private float _baseVolume;
        private float _distanceScale;
        private static AudioListener[] DEFAULT_LISTENERS = new AudioListener[] { new AudioListener() };
        
    }
}
