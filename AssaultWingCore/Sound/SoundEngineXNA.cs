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
    public class SoundEngineXNA : AWGameComponent
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

        private const float MAX_LISTENER_SPEED = 350; // to avoid extreme Doppler effect on fast moving viewports

        private Dictionary<string, SoundCue> _soundCues = new Dictionary<string, SoundCue>();
        private List<SoundInstance> _playingInstances = new List<SoundInstance>(); // One-off sounds
        private List<WeakReference> _createdInstances = new List<WeakReference>(); // Sound instances with owner
        private List<Tuple<int, SoundInstance>> _finishedInstances = new List<Tuple<int, SoundInstance>>();
        private object _lock = new object();
        private AudioListener _listener = new AudioListener();
        private AWMusic _music;

        /// <summary>
        /// Music volume of current track relative to other tracks, as set by sound engineer, between 0 and 1.
        /// </summary>
        public float RelativeMusicVolume { get; set; }

        /// <summary>
        /// Internal music volume, as set by program logic, between 0 and 1.
        /// </summary>
        protected float InternalMusicVolume { get; set; }

        private float ActualMusicVolume
        {
            get
            {
                float userMusicVolume = Game.Settings.Sound.MusicVolume;
                return userMusicVolume * RelativeMusicVolume * InternalMusicVolume;
            }
        }

        public SoundEngineXNA(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
        }

        public override void Initialize()
        {
            try
            {
                string filePath = Game.Content.ResolveContentPath(Paths.SOUND_DEFS);
                var document = new XmlDocument();
                document.Load(filePath);
                var soundNodes = document.SelectNodes("group/sound");
                var errors = false;
                foreach (XmlNode sound in soundNodes)
                {
                    var baseName = sound.Attributes["name"].Value.ToLower(CultureInfo.InvariantCulture);

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
                        errors = true;
                        Log.Write("Error loading sound " + baseName);
                    }
                    var cue = new SoundCue(effects, volume, dist, loop);
                    _soundCues.Add(baseName, cue);
                }
                if (errors) throw new ApplicationException("Couldn't load some sounds");
                Log.Write("Sound engine initialized.");
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
                if (_music != null) _music.Volume = ActualMusicVolume;

                // Remove expired instances
                int ticks = Environment.TickCount;
                _finishedInstances.RemoveAll(instance => instance.Item1 < ticks);

                // Move finished instances to separate list
                foreach (var instance in _playingInstances)
                {
                    if (instance.IsFinished)
                    {
                        instance.Dispose();
                        _finishedInstances.Add(Tuple.Create(ticks + RELEASE_DELAY, (SoundInstance)instance));
                    }
                }

                _playingInstances.RemoveAll(instance => instance.IsFinished);
                _createdInstances.RemoveAll(instance => instance.Target == null);

                var listeners =
                    from viewport in Game.DataEngine.Viewports
                    select new AudioListener
                    {
                        Position = new Vector3(viewport.CurrentLookAt, 0),
                        Velocity = new Vector3(viewport.Move.Clamp(0, MAX_LISTENER_SPEED), 0),
                    };
                if (listeners.Any())
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
            if (_music != null) _music.Dispose();
            _music = null;
            base.Dispose();
        }

        public void PlayMusic(string trackName, float trackVolume)
        {
            if (!Enabled) return;
            var changeTrack = _music == null || _music.TrackName != trackName;
            if (changeTrack) StopMusic();
            RelativeMusicVolume = trackVolume;
            InternalMusicVolume = 1;
            if (changeTrack)
            {
                if (_music != null) _music.Dispose();
                _music = new AWMusic(Game.Content, trackName) { Volume = ActualMusicVolume };
                _music.EnsureIsPlaying();
            }
        }

        public void StopMusic()
        {
            if (!Enabled) return;
            if (_music == null) return;
            _music.EnsureIsStopped();
        }

        private SoundInstance CreateSoundInternal(string soundName, Func<Vector2?> getEmitterPos, Func<Vector2?> getEmitterMove)
        {
            if (!Enabled) return new SoundInstanceDummy();
            soundName = soundName.ToLower(CultureInfo.InvariantCulture);
            if (!_soundCues.ContainsKey(soundName))
            {
                throw new ArgumentException("Sound " + soundName + " does not exist!");
            }
            var cue = _soundCues[soundName];
            var soundEffect = cue.GetEffect();
            var instance = soundEffect.CreateInstance();
            instance.IsLooped = cue._loop;
            return new SoundInstanceXNA(instance, getEmitterPos, getEmitterMove, cue._volume, cue._distanceScale);
        }

        public SoundInstance CreateSound(string soundName, Func<Vector2?> getSoundPos, Func<Vector2?> getSoundMove)
        {
            lock (_lock)
            {
                var instance = CreateSoundInternal(soundName, getSoundPos, getSoundMove);
                _createdInstances.Add(new WeakReference(instance));
                return instance;
            }
        }

        public SoundInstance PlaySound(string soundName, Func<Vector2?> getSoundPos, Func<Vector2?> getSoundMove)
        {
            lock (_lock)
            {
                var instance = CreateSoundInternal(soundName, getSoundPos, getSoundMove);
                instance.Play();
                _playingInstances.Add(instance);
                return instance;
            }
        }

        public SoundInstance CreateSound(string soundName, Gob parentGob)
        {
            return CreateSound(soundName, () => GetPos(parentGob), () => GetMove(parentGob));
        }

        public SoundInstance CreateSound(string soundName)
        {
            return CreateSound(soundName, () => null, () => null);
        }

        public SoundInstance PlaySound(string soundName, Gob parentGob)
        {
            return PlaySound(soundName, () => GetPos(parentGob), () => GetMove(parentGob));
        }

        public SoundInstance PlaySound(string soundName)
        {
            return PlaySound(soundName, () => null, () => null);
        }

        private Vector2? GetPos(Gob gob) { return gob == null ? (Vector2?)null : gob.Pos; }
        private Vector2? GetMove(Gob gob) { return gob == null ? (Vector2?)null : gob.Move; }
    }
}
