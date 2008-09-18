using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Sound
{
    [LimitedSerialization]
    public class BackgroundMusic
    {
        [TypeParameter]
        string fileName;

        [TypeParameter]
        float volume;

        public string FileName { get { return fileName; } set { fileName = value; } }
        public float Volume { get { return volume; } set { volume = value; } }

                /// <summary>
        /// Creates an uninitialised BGMusic.
        /// </summary>
        /// This constructor is only for serialisation.
        public BackgroundMusic()
        {
            this.fileName = "";
            this.volume = 50;
        }
    }
}
