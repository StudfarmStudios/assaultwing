using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;
using AW2.Events;
using AW2.Helpers;
using AW2.Game;

namespace AW2.Sound
{
    /// <summary>
    /// Implementation of sound engine. Works as an extra abstraction for XACT audio engine.
    /// </summary>
    /// <see cref="SoundEngine"/>
    public class SoundEngineImpl : GameComponent, SoundEngine
    {
        #region Private variables

        // Audio API components.
        AudioEngine audioEngine;
        WaveBank waveBank;
        SoundBank soundBank;
        private Dictionary<string, AudioCategory> categories =
            new Dictionary<string, AudioCategory>();

        private Cue backgroundCue;
        #endregion

        /// <summary>
        /// Creates a sound engine for the given game.
        /// </summary>
        /// <param name="game">The game.</param>
        public SoundEngineImpl(Microsoft.Xna.Framework.Game game) : base(game)
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

            //PlayMusic(); // for testing, remove when not needed
        }

        /// <summary>
        /// Main loop of audio processing. Checks for sound events and plays sounds accordingly.
        /// </summary>
        /// <param name="gameTime"></param>
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

        /// <summary>
        /// Sets sound volume for given sample category.
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="volumeAmount"></param>
        private void SetVolume(string categoryName, float volumeAmount)
        {
            volumeAmount = MathHelper.Clamp(volumeAmount, 0, 1);
            CheckCategory(categoryName);
            categories[categoryName].SetVolume(volumeAmount);
        }

        /// <summary>
        /// Checks if category list already contains the given key, and adds it if necessary.
        /// </summary>
        /// <param name="categoryName">Must be a valid sample category in XACT.</param>
        private void CheckCategory(string categoryName)
        {
            if (!categories.ContainsKey(categoryName))
                categories.Add(categoryName, audioEngine.GetCategory(categoryName));
        }

        #region SoundEngine Members

        /// <summary>
        /// Sets music playback volume.
        /// </summary>
        /// <param name="volume">a value between 0 and 1</param>
        public void SetMusicVolume(float volume)
        {
            Log.Write("volume:" + volume);
            SetVolume("Music", volume);
        }

        /// <summary>
        /// Starts playing random track from game music playlist
        /// </summary>
        public void PlayMusic()
        {
            soundBank.PlayCue("BG_Dark");
        }

        /// <summary>
        /// Starts playing random track from Arena tracklist.
        /// </summary>
        public void PlayMusic(Arena arena)
        {
            List<BackgroundMusic> musics = arena.BackgroundMusic;
            if (musics.Count > 0)
            {
                BackgroundMusic track = musics[RandomHelper.GetRandomInt(musics.Count)];
                SetMusicVolume(track.Volume);
                PlayMusic(track.FileName);
            }
        }
        

        /// <summary>
        /// Starts playing set track from game music playlist
        /// </summary>
        public void PlayMusic(String trackName)
        {
            backgroundCue = soundBank.GetCue(trackName);
            backgroundCue.Play();
        }


        /// <summary>
        /// Stops music playback.
        /// </summary>
        public void StopMusic()
        {
            if (backgroundCue != null)
            {
                Log.Write("Stopping Track:" + backgroundCue.Name);
                backgroundCue.Stop(AudioStopOptions.AsAuthored);
                backgroundCue = null;
                
            }
            //throw new Exception("The method or operation is not implemented.");
            // soundBank.GetCue("Pelimusat").Stop(AudioStopOptions.AsAuthored); // !!!fixme
        }

        /// <summary>
        /// Selects another random game track to play.
        /// </summary>
        public void SkipTrack()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Sets global volume of all sound effects.
        /// </summary>
        /// <param name="volume">a value between 0 and 1</param>
        public void SetSoundVolume(float volume)
        {
            SetVolume("Default", volume);
        }

        /// <summary>
        /// Plays a sound effect sample with given effects and location.
        /// </summary>
        /// <param name="actionType"></param>
        /// <param name="effectType"></param>
        /// <param name="location"></param>
        public void PlaySound(SoundOptions.Action actionType, SoundOptions.Effect effectType, Microsoft.Xna.Framework.Vector2 location)
        {
            soundBank.PlayCue(actionType.ToString());
        }

        #endregion
    }
}
