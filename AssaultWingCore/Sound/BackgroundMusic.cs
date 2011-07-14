using AW2.Helpers.Serialization;

namespace AW2.Sound
{
    /// <summary>
    /// A piece of background music.
    /// </summary>
    [LimitedSerialization]
    public class BackgroundMusic
    {
        [TypeParameter]
        string fileName;

        [TypeParameter]
        float volume;

        public string FileName { get { return fileName; } private set { fileName = value; } }
        public float Volume { get { return volume; } private set { volume = value; } }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public BackgroundMusic()
        {
            fileName = "";
            volume = 1;
        }
    }
}
