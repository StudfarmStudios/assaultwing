using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AW2.Settings
{
    public class AWSettings
    {
        private SoundSettings _sound;

        public SoundSettings Sound { get { return _sound; } private set { _sound = value; } }

        public AWSettings()
        {
            Sound = new SoundSettings();
        }
    }
}
