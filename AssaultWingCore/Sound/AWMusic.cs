using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using AW2.Graphics.Content;
using AW2.Helpers;

namespace AW2.Sound
{
    /// <summary>
    /// Music track. <see cref="AWMusic"/> is reusable; you can stop and replay
    /// the same <see cref="AWMusic"/> object. <see cref="AWMusic"/> also keeps
    /// the music instance alive even if XNA content is suddenly disposed.
    /// </summary>
    public class AWMusic
    {
#if BROWSER
        private Song _musicTrack;
        private float _volume = 1;
#else
        private AWContentManager _content;
        private SoundEffect _musicTrack;
        private SoundEffectInstance _musicInstance;
#endif
        private bool _isDisposed;

        public bool IsPlaying { get; private set; }
#if BROWSER
        public float Volume
        {
            get { return _volume; }
            set
            {
                _volume = Math.Clamp(value, 0, 1);
                if (IsPlaying) MediaPlayer.Volume = _volume;
            }
        }
#else
        public float Volume { get { return MusicInstance.Volume; } set { MusicInstance.Volume = value; } }
#endif
        public string TrackName { get; private set; }

#if !BROWSER
        private bool MusicInstanceAlive { get { return _musicInstance != null && !_musicInstance.IsDisposed; } }
        private bool MusicTrackAlive { get { return _musicTrack != null && !_musicTrack.IsDisposed; } }

        private SoundEffectInstance MusicInstance
        {
            get
            {
                if (!MusicInstanceAlive)
                {
                    _musicInstance = MusicTrack.CreateInstance();
                    _musicInstance.Pitch = 0;
                    _musicInstance.Pan = 0;
                    _musicInstance.IsLooped = true;
                    if (IsPlaying) _musicInstance.Play();
                }
                return _musicInstance;
            }
        }

        private SoundEffect MusicTrack
        {
            get
            {
                if (!MusicTrackAlive) _musicTrack = _content.Load<SoundEffect>(TrackName);
                return _musicTrack;
            }
        }
#endif

        public AWMusic(AWContentManager content, string trackName)
        {
#if !BROWSER
            _content = content;
#endif
            TrackName = trackName;
        }

        public void EnsureIsPlaying()
        {
            if (_isDisposed) throw new InvalidOperationException("The music is disposed");
#if BROWSER
            if (IsPlaying) return;
            _musicTrack ??= Song.FromUri(TrackName,
                new Uri(Path.Combine(Paths.MUSIC, TrackName + ".mp3").Replace('\\', '/'), UriKind.Relative));
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = _volume;
            MediaPlayer.Play(_musicTrack);
            IsPlaying = true;
            Log.Write("Browser music playback requested ({0}).", TrackName);
#else
            if (MusicInstanceAlive && IsPlaying) return;
            var dummy = MusicInstance;
            if (!IsPlaying) MusicInstance.Play();
            IsPlaying = true;
#endif
        }

        public void EnsureIsStopped()
        {
            if (_isDisposed) throw new InvalidOperationException("The music is disposed");
#if BROWSER
            if (IsPlaying) MediaPlayer.Stop();
#else
            if (MusicInstanceAlive && IsPlaying) _musicInstance.Stop();
#endif
            IsPlaying = false;
        }

        public void Dispose()
        {
#if BROWSER
            if (IsPlaying) MediaPlayer.Stop();
            _musicTrack?.Dispose();
            _musicTrack = null;
#else
            if (_musicInstance != null)
            {
                _musicInstance.Dispose();
                _musicInstance = null;
            }
            _musicTrack = null;
#endif
            IsPlaying = false;
            _isDisposed = true;
        }
    }
}
