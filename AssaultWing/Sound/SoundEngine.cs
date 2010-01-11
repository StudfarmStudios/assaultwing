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
    public class SoundEngine : GameComponent
    {
        #region Private fields

        AudioEngine audioEngine;
        WaveBank waveBank;
        SoundBank soundBank;
        SoundEffectInstance musicInstance;
        Action volumeFadeAction;

        #endregion

        /// <summary>
        /// Creates a sound engine for the given game.
        /// </summary>
        public SoundEngine(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
        }

        #region Overridden GameComponent methods

        public override void Initialize()
        {
            audioEngine = new AudioEngine(System.IO.Path.Combine(Paths.Sounds, "assaultwingsounds.xgs"));
            waveBank = new WaveBank(audioEngine, System.IO.Path.Combine(Paths.Sounds, "Wave Bank.xwb"));
            soundBank = new SoundBank(audioEngine, System.IO.Path.Combine(Paths.Sounds, "Sound Bank.xsb"));
            Log.Write("Sound engine initialized.");
        }

        public override void Update(GameTime gameTime)
        {
            if (volumeFadeAction != null) volumeFadeAction();
            if (musicInstance != null) musicInstance.Volume = ActualMusicVolume;
            audioEngine.Update();
        }

        #endregion

        #region Public interface

        /// <summary>
        /// Sound effect volume, between 0 and 1.
        /// </summary>
        public float SoundVolume
        {
            set
            {
                value = MathHelper.Clamp(value, 0, 1);
                audioEngine.GetCategory("Default").SetVolume(value);
            }
        }

        /// <summary>
        /// General music volume as set by player, between 0 and 1.
        /// </summary>
        public float UserMusicVolume { get; set; }

        /// <summary>
        /// Music volume of current track relative to other tracks, as set by sound engineer, between 0 and 1.
        /// </summary>
        public float RelativeMusicVolume { get; set; }

        /// <summary>
        /// Internal music volume, as set by program logic, between 0 and 1.
        /// </summary>
        private float InternalMusicVolume { get; set; }

        private float ActualMusicVolume { get { return UserMusicVolume * RelativeMusicVolume * InternalMusicVolume; } }

        /// <summary>
        /// Starts playing a random track from a tracklist.
        /// </summary>
        public void PlayMusic(IList<BackgroundMusic> musics)
        {
            if (musics.Count > 0)
            {
                BackgroundMusic track = musics[RandomHelper.GetRandomInt(musics.Count)];
                PlayMusic(track.FileName, track.Volume);
            }
        }

        /// <summary>
        /// Starts playing set track from game music playlist
        /// </summary>
        public void PlayMusic(String trackName, float trackVolume)
        {
            var music = AssaultWing.Instance.Content.Load<SoundEffect>(trackName);
            StopMusic();
            RelativeMusicVolume = trackVolume;
            InternalMusicVolume = 1;
            musicInstance = music.CreateInstance();
            musicInstance.Volume = ActualMusicVolume;
            musicInstance.Pitch = 0;
            musicInstance.Pan = 0;
            musicInstance.IsLooped = true;
            musicInstance.Play();
        }

        /// <summary>
        /// Stops music playback immediately.
        /// </summary>
        public void StopMusic()
        {
            if (musicInstance == null) return;
            musicInstance.Stop();
            musicInstance = null;
            volumeFadeAction = null;
        }

        /// <summary>
        /// Stops music playback with a fadeout.
        /// </summary>
        public void StopMusic(TimeSpan fadeoutTime)
        {
            if (musicInstance == null) return;
            var now = AssaultWing.Instance.GameTime.TotalRealTime;
            float fadeoutSeconds = (float)fadeoutTime.TotalSeconds;
            volumeFadeAction = () =>
            {
                float volume = MathHelper.Clamp(1 - now.SecondsAgoRealTime() / fadeoutSeconds, 0, 1);
                InternalMusicVolume = volume;
                if (volume == 0) StopMusic();
            };
        }

        public void PlaySound(string soundName)
        {
            soundBank.PlayCue(soundName);
        }

        #endregion
    }
}
