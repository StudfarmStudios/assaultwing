using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using AW2.Game;
using AW2.Helpers;
using System.Xml;
using NUnit.Framework;
using AW2.Graphics;

namespace AW2.Sound
{
    /// <summary>
    /// Sound engine. Works as an extra abstraction for XACT audio engine.
    /// </summary>
    public class SoundEngineXNA : SoundEngine
    {
        public class SoundCue
        {
            public SoundCue(SoundEffect[] effects, float volume, bool loop)
            {
                _effects = effects;
                _volume = volume;
                _loop = loop;
            }

            public SoundEffect GetEffect()
            {
                int index = RandomHelper.GetRandomInt(_effects.Length);
                return _effects[index];
            }

            public bool _loop;
            public float _volume;
            public SoundEffect[] _effects;
        }

        Dictionary<string, SoundCue> _soundCues = new Dictionary<string, SoundCue>();

        #region Private fields
        AWMusic _music;
        Action _volumeFadeAction;

        /*
        AudioEngine _audioEngine;
        WaveBank _waveBank;
        SoundBank _soundBank;
        AudioCategory _soundEffectCategory;
        */

        #endregion

        public SoundEngineXNA(AW2.Core.AWGame game)
            : base(game)
        {
        }

        #region Overridden GameComponent methods

        public override void Initialize()
        {
            try
            {
                Log.Write("Sound engine initialized.");
                string filePath = AssaultWingCore.Instance.Content.RootDirectory + "\\content\\sounds\\sounddefs.xml";

                List<string> allSounds = new List<string>();
                
                XmlDocument document = new XmlDocument();
                document.Load(filePath);
                XmlNodeList soundNodes = document.SelectNodes("group/sound");
                foreach(XmlNode sound in soundNodes)
                {
                    string baseName = sound.Attributes["name"].Value.ToLower();
                    
                    XmlAttribute loopAttribute = sound.Attributes["loop"];
                    bool loop = (loopAttribute != null ? Boolean.Parse(loopAttribute.Value) : false);
                    
                    XmlAttribute volumeAttribute = sound.Attributes["volume"];
                    float volume = (volumeAttribute != null ? (float)Double.Parse(volumeAttribute.Value) : 1.0f);

                    // Find all variations for a sound
                    List<SoundEffect> effects = new List<SoundEffect>();
                    AWContentManager manager = (AWContentManager)AssaultWingCore.Instance.Content;
                    
                    for (int i = 1; i <= 99; i++)
                    {
                        string name = string.Format("{0}{1:00}", baseName, i);
                        if (!manager.Exists<SoundEffect>(name))
                        {
                            break;
                        }

                        SoundEffect effect = manager.Load<SoundEffect>(name);
                        effects.Add(effect);
                    }
                    SoundCue cue = new SoundCue(effects.ToArray(), volume, loop);
                    _soundCues.Add(baseName, cue);
                }
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

         /*   
            
            _audioEngine.Update();
            _soundEffectCategory.SetVolume(AssaultWing.Instance.Settings.Sound.SoundVolume);*/
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
            var now = AssaultWingCore.Instance.GameTime.TotalRealTime;
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

            soundName = soundName.ToLower();

            if (!_soundCues.ContainsKey(soundName))
            {
                throw new ArgumentException("Sound " + soundName + " does not exist!");                
            }
            
            SoundCue cue = _soundCues[soundName.ToLower()];

            SoundEffect soundEffect = cue.GetEffect();
            
            SoundEffectInstance instance = soundEffect.CreateInstance();
            instance.Volume = cue._volume;
            instance.IsLooped = cue._loop;

            return new SoundInstanceXNA(soundEffect.CreateInstance());
        }
        // Instance gc'd -> sound ends?

        #endregion
    }
}
