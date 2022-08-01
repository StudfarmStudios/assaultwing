using System;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Graphics
{
    /// <summary>
    /// Wrapper for XNA video player interface. Resistant to disposable XNA content.
    /// The video will not loop.
    /// </summary>
    public class AWVideo : IDisposable
    {
        //private VideoPlayer _videoPlayer;
        private bool _beginPassed;
        private bool _isFinished;

        public bool IsFinished { get { CheckState(); return _isFinished; } }

        //public AWVideo(Video video, float volume)
        //{
            //_videoPlayer = new VideoPlayer{ Volume = volume };
            //_videoPlayer.Play(video);
            //_videoPlayer.Stop();
        //}

        public void Play()
        {
            //_videoPlayer.Resume();
        }

        public void Stop()
        {
            //if (!_videoPlayer.IsDisposed) _videoPlayer.Stop();
        }

        /// <summary>
        /// Returns the current frame of the video, or null if playback has finished.
        /// </summary>
        public Texture2D GetTexture()
        {
            CheckState();
            //if (IsFinished) return null;
            //return _videoPlayer.GetTexture();
            return null;
        }

        /// <summary>
        /// Disposes the content of the <see cref="AWVideo"/> instance. Note that
        /// the instance itself is not disposed and is still fully operational.
        /// </summary>
        public void Dispose()
        {
            //_videoPlayer.Dispose();
        }

        private void CheckState()
        {
            // When XNA content is disposed, the video player loses its video.
            // The video player may also skip to the beginning of the video if a long
            // time passes between frame updates.
            //if (_videoPlayer.IsDisposed || _videoPlayer.Video == null)
            //{
            //    _isFinished = true;
            //    return;
            //}
            //if (_videoPlayer.PlayPosition == _videoPlayer.Video.Duration) _isFinished = true;
            //if (_videoPlayer.PlayPosition > TimeSpan.Zero) _beginPassed = true;
            //if (_videoPlayer.PlayPosition == TimeSpan.Zero && _beginPassed) _isFinished = true;
        }
    }
}
