using System;
using System.Collections.Generic;
using System.Text;
using AW2.Game;

namespace AW2.Sound
{
    /// <summary>
    /// Sound engine. Works as an extra abstraction for XACT audio engine.
    /// </summary>
    public interface SoundEngine
    {
        /// <summary>
        /// Sets music playback volume.
        /// </summary>
        /// <param name="volume">a value between 0 and 1</param>
        void SetMusicVolume(float volume);

        /// <summary>
        /// Starts playing random track from game music playlist
        /// </summary>
        void PlayMusic();

        /// <summary>
        /// Starts playing set track from game music playlist
        /// </summary>
        void PlayMusic(String songName);

        /// <summary>
        /// Starts playing random track from Arena tracklist.
        /// </summary>
        public void PlayMusic(Arena arena);

        /// <summary>
        /// Stops music playback.
        /// </summary>
        void StopMusic();

        /// <summary>
        /// Selects another random game track to play.
        /// </summary>
        void SkipTrack();

        /// <summary>
        /// Sets global volume of all sound effects.
        /// </summary>
        /// <param name="volume">a value between 0 and 1</param>
        void SetSoundVolume(float volume);

        /// <summary>
        /// Plays a sound effect sample with given effects and location.
        /// </summary>
        /// <param name="actionType"></param>
        /// <param name="effectType"></param>
        /// <param name="location"></param>
        void PlaySound(SoundOptions.Action actionType, 
            SoundOptions.Effect effectType, Microsoft.Xna.Framework.Vector2 location);
    }
}
