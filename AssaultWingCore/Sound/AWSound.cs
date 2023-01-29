using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Audio;

namespace AW2.Sound
{
    /// <summary>
    /// Sound effect. Each instance of <see cref="AWSound"/> can play only one
    /// simultaneous sound effect instance. <see cref="AWSound"/> is reusable;
    /// you can stop and replay the sound.
    /// </summary>
   /* public class AWSound
    {
        private string _cueName;
        private SoundEngine _cue;
        private bool _isDisposed;

        public bool IsPlaying { get { return _cue != null && _cue.IsPlaying; } }

        public AWSound(string cueName)
        {
            _cueName = cueName;
        }
    }*/
}
