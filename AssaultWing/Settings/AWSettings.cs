using System;
using System.IO;
using AW2.Helpers;

namespace AW2.Settings
{
    public class AWSettings
    {
        private static readonly string SETTINGS_FILENAME = "AssaultWing.config";
        private SoundSettings _sound;
        private NetSettings _net;

        public SoundSettings Sound { get { return _sound; } private set { _sound = value; } }
        public NetSettings Net { get { return _net; } private set { _net = value; } }

        public static AWSettings FromFile()
        {
            if (File.Exists(SETTINGS_FILENAME))
                return (AWSettings)TypeLoader.LoadTemplate(SETTINGS_FILENAME, typeof(AWSettings), null);

            // Create a new settings file
            var settings = new AWSettings();
            settings.ToFile();
            return settings;
        }

        public AWSettings()
        {
            Sound = new SoundSettings();
            Net = new NetSettings();
        }

        public void ToFile()
        {
            TypeLoader.SaveTemplate(this, SETTINGS_FILENAME, typeof(AWSettings), null);
        }
    }
}
