using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Core;

namespace AW2.Sound
{
    public abstract class SoundEngine : AWGameComponent
    {
        public SoundEngine()
        {
        }

        #region Public interface

        /// <summary>
        /// Music volume of current track relative to other tracks, as set by sound engineer, between 0 and 1.
        /// </summary>
        public float RelativeMusicVolume { get; set; }

        /// <summary>
        /// Internal music volume, as set by program logic, between 0 and 1.
        /// </summary>
        protected float InternalMusicVolume { get; set; }

        protected float ActualMusicVolume
        {
            get
            {
                float userMusicVolume = AssaultWingCore.Instance.Settings.Sound.MusicVolume;
                return userMusicVolume * RelativeMusicVolume * InternalMusicVolume;
            }
        }

        /// <summary>
        /// Starts playing a random track from a tracklist.
        /// </summary>
        public abstract void PlayMusic(IList<BackgroundMusic> musics);
        
        /// <summary>
        /// Starts playing set track from game music playlist
        /// </summary>
        public abstract void PlayMusic(string trackName, float trackVolume);
        
        /// <summary>
        /// Stops music playback immediately.
        /// </summary>
        public abstract void StopMusic();
        
        /// <summary>
        /// Stops music playback with a fadeout.
        /// </summary>
        public abstract void StopMusic(TimeSpan fadeoutTime);
        
        /// <summary>
        /// Start a sound. Return a SoundInstance object to it, or null if sounds are disabled.
        /// </summary>
        public SoundInstance PlaySound(string soundName)
        {
            SoundInstance instance = CreateSound(soundName);
            
            if (instance != null)
            {
                instance.Play();
            }
            return instance;
        }
        
        /// <summary>
        /// Create a sound without starting it. Return a SoundInstance object, or null if sounds are disabled.
        /// </summary>
        public abstract SoundInstance CreateSound(string soundName);
        

        #endregion
    }
}
