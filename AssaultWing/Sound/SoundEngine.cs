using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using AW2.Events;
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

        // Audio API components.
        AudioEngine audioEngine;
        WaveBank waveBank;
        SoundBank soundBank;
        SoundEffectInstance musicInstance;

        #endregion

        /// <summary>
        /// Creates a sound engine for the given game.
        /// </summary>
        public SoundEngine(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
        }

        /// <summary>
        /// Loads audio engine and sound banks.
        /// </summary>
        public override void Initialize()
        {
            audioEngine = new AudioEngine(System.IO.Path.Combine(Paths.Sounds, "assaultwingsounds.xgs"));
            waveBank = new WaveBank(audioEngine, System.IO.Path.Combine(Paths.Sounds, "Wave Bank.xwb"));
            soundBank = new SoundBank(audioEngine, System.IO.Path.Combine(Paths.Sounds, "Sound Bank.xsb"));
            Log.Write("Sound engine initialized.");
        }

        /// <summary>
        /// Main loop of audio processing. Checks for sound events and plays sounds accordingly.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            EventEngine eventEngine = (EventEngine)Game.Services.GetService(typeof(EventEngine));
            SoundEffectEvent eventti;
            while ((eventti = (SoundEffectEvent)eventEngine.GetEvent(typeof(SoundEffectEvent))) != null)
            {
                PlaySound(eventti.getAction(), eventti.getEffect(), Vector2.Zero);
            }
            audioEngine.Update();
        }

        #region Public interface

        /// <summary>
        /// Sound effect volume, between 0 and 1.
        /// </summary>
        public float SoundVolume
        {
            set {
                value = MathHelper.Clamp(value, 0, 1);
                audioEngine.GetCategory("Default").SetVolume(value);
            }
        }

        /// <summary>
        /// Music volume, between 0 and 1.
        /// </summary>
        public float MusicVolume { get; set; }

        /// <summary>
        /// Starts playing a random track from a tracklist.
        /// </summary>
        public void PlayMusic(IList<BackgroundMusic> musics)
        {
            if (musics.Count > 0)
            {
                BackgroundMusic track = musics[RandomHelper.GetRandomInt(musics.Count)];
                MusicVolume = track.Volume;
                PlayMusic(track.FileName);
            }
        }

        /// <summary>
        /// Starts playing set track from game music playlist
        /// </summary>
        public void PlayMusic(String trackName)
        {
            var music = AssaultWing.Instance.Content.Load<SoundEffect>(trackName);
            StopMusic();
            musicInstance = music.Play(MusicVolume, 0, 0, true);
        }

        /// <summary>
        /// Stops music playback.
        /// </summary>
        public void StopMusic()
        {
            if (musicInstance == null) return;
            musicInstance.Stop();
            musicInstance = null;
        }

        /// <summary>
        /// Plays a sound effect sample with given effects and location.
        /// </summary>
        public void PlaySound(SoundOptions.Action actionType, SoundOptions.Effect effectType, Microsoft.Xna.Framework.Vector2 location)
        {
            soundBank.PlayCue(actionType.ToString());
        }

        #endregion
    }
}
