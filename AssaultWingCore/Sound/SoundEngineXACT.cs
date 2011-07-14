using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Sound
{
    /// <summary>
    /// Sound engine based on XACT.
    /// </summary>
    public class SoundEngineXACT : SoundEngine
    {
        private AudioEngine _audioEngine;
        private WaveBank _waveBank;
        private SoundBank _soundBank;
        private AudioCategory _soundEffectCategory;
        private AWMusic _music;

        public SoundEngineXACT(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
        }

        public override void Initialize()
        {
            try
            {
                _audioEngine = new AudioEngine(System.IO.Path.Combine(Paths.CONTENT, "assaultwingsounds.xgs"));
                _waveBank = new WaveBank(_audioEngine, System.IO.Path.Combine(Paths.CONTENT, "Wave Bank.xwb"));
                _soundBank = new SoundBank(_audioEngine, System.IO.Path.Combine(Paths.CONTENT, "Sound Bank.xsb"));
                _soundEffectCategory = _audioEngine.GetCategory("Default");
                Log.Write("Sound engine initialized.");
            }
            catch (InvalidOperationException e)
            {
                Log.Write("ERROR: There will be no sound. Sound engine initialization failed. Exception details: " + e);
                Enabled = false;
            }
        }

        public override void Update()
        {
            if (_music != null) _music.Volume = ActualMusicVolume;
            _audioEngine.Update();
            _soundEffectCategory.SetVolume(Game.Settings.Sound.SoundVolume);
        }

        public override void Dispose()
        {
            if (_music != null)
            {
                _music.Dispose();
                _music = null;
            }
            if (_audioEngine != null)
            {
                _audioEngine.Dispose();
                _audioEngine = null;
            }
            base.Dispose();
        }

        public override void PlayMusic(string trackName, float trackVolume)
        {
            if (!Enabled) return;
            var changeTrack = _music == null || _music.TrackName != trackName;
            if (changeTrack) StopMusic();
            RelativeMusicVolume = trackVolume;
            InternalMusicVolume = 1;
            if (changeTrack)
            {
                _music = new AWMusic(Game.Content, trackName) { Volume = ActualMusicVolume };
                _music.EnsureIsPlaying();
            }
        }

        public override void StopMusic()
        {
            if (!Enabled) return;
            if (_music == null) return;
            _music.EnsureIsStopped();
        }

        public override SoundInstance CreateSound(string soundName, Gob gob)
        {
            if (!Enabled) return new SoundInstanceDummy();
            var cue = _soundBank.GetCue(soundName);
            return new SoundInstanceXACT(cue, _soundBank);
        }

        public override SoundInstance PlaySound(string soundName, Gob gob)
        {
            var instance = CreateSound(soundName);
            instance.Play();
            return instance;
        }
    }
}
