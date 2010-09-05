using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Sound
{
    /// <summary>
    /// Sound engine. Works as an extra abstraction for XACT audio engine.
    /// </summary>
    public class SoundEngineXACT : SoundEngine
    {
        #region Private fields

        AudioEngine _audioEngine;
        WaveBank _waveBank;
        SoundBank _soundBank;
        AudioCategory _soundEffectCategory;
        AWMusic _music;
        Action _volumeFadeAction;

        #endregion

        public SoundEngineXACT()
        {
        }

        #region Overridden GameComponent methods

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
                Log.Write("ERROR: There will be no sound. Sound engine initialization failed. Exception details: " + e.ToString());
                Enabled = false;
            }
        }

        public override void Update()
        {
            if (_volumeFadeAction != null) _volumeFadeAction();
            if (_music != null) _music.Volume = ActualMusicVolume;
            _audioEngine.Update();
            _soundEffectCategory.SetVolume(AssaultWing.Instance.Settings.Sound.SoundVolume);
        }

        #endregion

        #region Public interface

        /// <summary>
        /// Starts playing a random track from a tracklist.
        /// </summary>
        public override void PlayMusic(IList<BackgroundMusic> musics)
        {
            if (!Enabled) return;
            if (musics.Count > 0)
            {
                BackgroundMusic track = musics[RandomHelper.GetRandomInt(musics.Count)];
                PlayMusic(track.FileName, track.Volume);
            }
        }

        /// <summary>
        /// Starts playing set track from game music playlist
        /// </summary>
        public override void PlayMusic(string trackName, float trackVolume)
        {
            if (!Enabled) return;
            StopMusic();
            RelativeMusicVolume = trackVolume;
            InternalMusicVolume = 1;
            _music = new AWMusic(trackName) { Volume = ActualMusicVolume };
            _music.EnsureIsPlaying();
        }

        /// <summary>
        /// Stops music playback immediately.
        /// </summary>
        public override void StopMusic()
        {
            if (!Enabled) return;
            if (_music == null) return;
            _music.EnsureIsStopped();
            _volumeFadeAction = null;
        }

        /// <summary>
        /// Stops music playback with a fadeout.
        /// </summary>
        public override void StopMusic(TimeSpan fadeoutTime)
        {
            if (!Enabled) return;
            if (_music == null || !_music.IsPlaying) return;
            var now = AssaultWing.Instance.GameTime.TotalRealTime;
            float fadeoutSeconds = (float)fadeoutTime.TotalSeconds;
            _volumeFadeAction = () =>
            {
                float volume = MathHelper.Clamp(1 - now.SecondsAgoRealTime() / fadeoutSeconds, 0, 1);
                InternalMusicVolume = volume;
                if (volume == 0) StopMusic();
            };
        }

        /// <summary>
        /// Returns the named cue or <code>null</code> if sounds are disabled.
        /// </summary>
        public override SoundInstance CreateSound(string soundName)
        {
            if (!Enabled) return null;
            Cue cue = _soundBank.GetCue(soundName);
            return new SoundInstanceXACT(cue, _soundBank);
        }

  
        #endregion
    }
}
