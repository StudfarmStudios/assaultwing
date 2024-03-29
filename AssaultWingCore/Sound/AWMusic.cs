using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using AW2.Graphics.Content;

namespace AW2.Sound
{
    /// <summary>
    /// Music track. <see cref="AWMusic"/> is reusable; you can stop and replay
    /// the same <see cref="AWMusic"/> object. <see cref="AWMusic"/> also keeps
    /// the music instance alive even if XNA content is suddenly disposed.
    /// </summary>
    public class AWMusic
    {
        private AWContentManager _content;
        private SoundEffect _musicTrack;
        private SoundEffectInstance _musicInstance;
        private bool _isDisposed;

        public bool IsPlaying { get; private set; }
        public float Volume { get { return MusicInstance.Volume; } set { MusicInstance.Volume = value; } }
        public string TrackName { get; private set; }

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

        public AWMusic(AWContentManager content, string trackName)
        {
            _content = content;
            TrackName = trackName;
        }

        public void EnsureIsPlaying()
        {
            if (_isDisposed) throw new InvalidOperationException("The music is disposed");
            if (MusicInstanceAlive && IsPlaying) return;
            var dummy = MusicInstance;
            if (!IsPlaying) MusicInstance.Play();
            IsPlaying = true;
        }

        public void EnsureIsStopped()
        {
            if (_isDisposed) throw new InvalidOperationException("The music is disposed");
            if (MusicInstanceAlive && IsPlaying) _musicInstance.Stop();
            IsPlaying = false;
        }

        public void Dispose()
        {
            if (_musicInstance != null)
            {
                _musicInstance.Dispose();
                _musicInstance = null;
            }
            _musicTrack = null;
            _isDisposed = true;
        }
    }
}
