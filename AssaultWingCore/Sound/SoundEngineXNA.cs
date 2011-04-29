using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Sound
{
    /// <summary>
    /// Sound engine based on XNA's <see cref="SoundEffect"/>.
    /// </summary>
    public class SoundEngineXNA : SoundEngine
    {
        public class SoundCue
        {
            public SoundCue(SoundEffect[] effects, float volume, float distanceScale, bool loop)
            {
                _effects = effects;
                _volume = volume;
                _loop = loop;
                _distanceScale = distanceScale;
            }

            public SoundEffect GetEffect()
            {
                int index = RandomHelper.GetRandomInt(_effects.Length);
                return _effects[index];
            }

            public bool _loop;
            public float _volume;
            public float _distanceScale;
            public SoundEffect[] _effects;
        }

        private Dictionary<string, SoundCue> _soundCues = new Dictionary<string, SoundCue>();
        private List<SoundInstance> _playingInstances = new List<SoundInstance>(); // One-off sounds
        private List<WeakReference> _createdInstances = new List<WeakReference>(); // Sound instances with owner
        private List<Tuple<int, SoundInstance>> _finishedInstances = new List<Tuple<int, SoundInstance>>();
        private object _lock = new object();
        private AudioListener _listener = new AudioListener();
        private AWMusic _music;
        private Action _volumeFadeAction;

        public SoundEngineXNA(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
        }

        public override void Initialize()
        {
            try
            {
                Log.Write("Sound engine initialized.");
                string filePath = Game.Content.RootDirectory + "\\corecontent\\sounds\\sounddefs.xml";

                var allSounds = new List<string>();

                var document = new XmlDocument();
                document.Load(filePath);
                var soundNodes = document.SelectNodes("group/sound");
                foreach (XmlNode sound in soundNodes)
                {
                    var baseName = sound.Attributes["name"].Value.ToLower();

                    var loopAttribute = sound.Attributes["loop"];
                    bool loop = (loopAttribute != null ? Boolean.Parse(loopAttribute.Value) : false);

                    var spatialAttribute = sound.Attributes["spatial"];
                    bool spatial = (spatialAttribute != null ? Boolean.Parse(spatialAttribute.Value) : true);

                    var volumeAttribute = sound.Attributes["volume"];
                    float volume = (volumeAttribute != null ? (float)Double.Parse(volumeAttribute.Value, CultureInfo.InvariantCulture) : 1.0f);

                    var distAttribute = sound.Attributes["distancescale"];
                    float dist = (distAttribute != null ? (float)Double.Parse(distAttribute.Value, CultureInfo.InvariantCulture) : 200.0f);

                    // Find all variations for a sound
                    var effects = Enumerable.Range(1, 99)
                        .Select(i => string.Format("{0}{1:00}", baseName, i))
                        .TakeWhile(name => Game.Content.Exists<SoundEffect>(name))
                        .Select(name => Game.Content.Load<SoundEffect>(name))
                        .ToArray();
                    if (!effects.Any())
                    {
                        Console.WriteLine("Error loading sound " + baseName + " (missing from project?)");
                    }
                    var cue = new SoundCue(effects, volume, dist, loop);
                    _soundCues.Add(baseName, cue);
                }
            }
            catch (NoAudioHardwareException e)
            {
                Log.Write("ERROR: There will be no sound. Sound engine initialization failed. Exception details: " + e);
                Enabled = false;
            }
        }

        private const int RELEASE_DELAY = 500; // 0.5 sec

        public override void Update()
        {
            lock (_lock)
            {
                if (_volumeFadeAction != null) _volumeFadeAction();
                if (_music != null) _music.Volume = ActualMusicVolume;

                // Remove expired instances
                int ticks = Environment.TickCount;
                _finishedInstances.RemoveAll(instance => instance.Item1 < ticks);

                // Move finished instances to separate list
                foreach (var instance in _playingInstances)
                {
                    if (instance.IsFinished)
                    {
                        _finishedInstances.Add(Tuple.Create(ticks + RELEASE_DELAY, (SoundInstance)instance));
                    }
                }

                _playingInstances.RemoveAll(instance => instance.IsFinished);
                _createdInstances.RemoveAll(instance => instance.Target == null);

                var listeners =
                    from player in Game.DataEngine.Players
                    where !player.IsRemote
                    let move = player.Ship != null ? player.Ship.Move : Vector2.Zero
                    select new AudioListener
                    {
                        Position = new Vector3(player.LookAtPos, 0),
                        Velocity = new Vector3(move, 0),
                    };
                if (listeners.Count() > 0)
                {
                    var listenerArray = listeners.ToArray();
                    foreach (var instance in _playingInstances)
                    {
                        instance.UpdateSpatial(listenerArray);
                    }
                    foreach (var instance in _createdInstances)
                    {
                        var soundInstance = (SoundInstance)instance.Target;
                        if (soundInstance != null)
                        {
                            soundInstance.UpdateSpatial(listenerArray);
                        }
                    }
                }
            }
        }

        public override void Dispose()
        {
            foreach (var sound in _playingInstances) sound.Dispose();
            foreach (var sound in _createdInstances.Select(x => x.Target).Where(x => x != null).Cast<SoundInstance>()) sound.Dispose();
            if (_music != null)
            {
                _music.Dispose();
                _music = null;
            }
            base.Dispose();
        }

        public override void PlayMusic(IList<BackgroundMusic> musics)
        {
            if (!Enabled) return;
            if (musics.Count > 0)
            {
                BackgroundMusic track = musics[RandomHelper.GetRandomInt(musics.Count)];
                PlayMusic(track.FileName, track.Volume);
            }
        }

        public override void PlayMusic(string trackName, float trackVolume)
        {
            if (!Enabled) return;
            StopMusic();
            RelativeMusicVolume = trackVolume;
            InternalMusicVolume = 1;
            _music = new AWMusic(Game.Content, trackName) { Volume = ActualMusicVolume };
            _music.EnsureIsPlaying();
        }

        public override void StopMusic()
        {
            if (!Enabled) return;
            if (_music == null) return;
            _music.EnsureIsStopped();
            _volumeFadeAction = null;
        }

        public override void StopMusic(TimeSpan fadeoutTime)
        {
            if (!Enabled) return;
            if (_music == null || !_music.IsPlaying) return;
            var now = Game.GameTime.TotalRealTime;
            float fadeoutSeconds = (float)fadeoutTime.TotalSeconds;
            _volumeFadeAction = () =>
            {
                float volume = MathHelper.Clamp(1 - now.SecondsAgoRealTime() / fadeoutSeconds, 0, 1);
                InternalMusicVolume = volume;
                if (volume == 0) StopMusic();
            };
        }

        private SoundInstance CreateSoundInternal(string soundName, Gob parentGob)
        {
            if (!Enabled) return new SoundInstanceDummy();
            soundName = soundName.ToLower();
            if (!_soundCues.ContainsKey(soundName))
            {
                throw new ArgumentException("Sound " + soundName + " does not exist!");
            }
            var cue = _soundCues[soundName];
            var soundEffect = cue.GetEffect();
            var instance = soundEffect.CreateInstance();
            instance.IsLooped = cue._loop;
            return new SoundInstanceXNA(instance, parentGob, cue._volume, cue._distanceScale);
        }

        public override SoundInstance CreateSound(string soundName, Gob parentGob)
        {
            lock (_lock)
            {
                var instance = CreateSoundInternal(soundName, parentGob);
                _createdInstances.Add(new WeakReference(instance));
                return instance;
            }
        }

        public override SoundInstance PlaySound(string soundName, Gob parentGob)
        {
            lock (_lock)
            {
                var instance = CreateSoundInternal(soundName, parentGob);
                instance.Play();
                _playingInstances.Add(instance);
                return instance;
            }
        }
    }
}
