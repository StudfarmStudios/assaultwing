using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Sound
{
    /// <summary>
    /// A piece of background music.
    /// </summary>
    [LimitedSerialization]
    public class BackgroundMusic
    {
        /// <summary>
        /// File name of the piece.
        /// </summary>
        [TypeParameter]
        string fileName;

        /// <summary>
        /// Playing volume of the piece.
        /// </summary>
        [TypeParameter]
        float volume;

        /// <summary>
        /// File name of the piece.
        /// </summary>
        public string FileName { get { return fileName; } set { fileName = value; } }

        /// <summary>
        /// Playing volume of the piece.
        /// </summary>
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
