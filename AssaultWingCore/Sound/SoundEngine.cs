using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game;

namespace AW2.Sound
{
    public abstract class SoundEngine : AWGameComponent
    {
        public SoundEngine(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
        }

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
                float userMusicVolume = Game.Settings.Sound.MusicVolume;
                return userMusicVolume * RelativeMusicVolume * InternalMusicVolume;
            }
        }

        /// <summary>
        /// Starts playing a music track with the given volume, between 0 and 1.
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
        /// Creates, starts and returns a sound.
        /// </summary>
        public abstract SoundInstance PlaySound(string soundName, Gob parentGob);
        
        /// <summary>
        /// Creates and returns a sound without starting it.
        /// </summary>
        public abstract SoundInstance CreateSound(string soundName, Gob parentGob);
        
        public SoundInstance CreateSound(string soundName)
        {
            return CreateSound(soundName, null);
        }

        public SoundInstance PlaySound(string soundName)
        {
            return PlaySound(soundName, null);
        }
    }
}
