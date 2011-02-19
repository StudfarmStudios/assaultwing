using System;
using System.IO;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Settings
{
    public class AWSettings
    {
        private SoundSettings _sound;
        private NetSettings _net;
        private GraphicsSettings _graphics;
        private ControlsSettings _controls;

        public SoundSettings Sound { get { return _sound; } private set { _sound = value; } }
        public NetSettings Net { get { return _net; } private set { _net = value; } }
        public GraphicsSettings Graphics { get { return _graphics; } private set { _graphics = value; } }
        public ControlsSettings Controls { get { return _controls; } private set { _controls = value; } }

        public static string SettingsDirectory { get; set; }

        private static string SettingsFilename { get { return Path.Combine(SettingsDirectory, "AssaultWing.config"); } }

        public static AWSettings FromFile()
        {
            if (File.Exists(SettingsFilename))
            {
                var settings = (AWSettings)TypeLoader.LoadTemplate(SettingsFilename, typeof(AWSettings), null, true);
                if (settings != null) return settings;
                Log.Write("Errors while reading settings from " + SettingsFilename);
            }

            // Create a new settings file
            Log.Write("Creating a new settings file " + SettingsFilename);
            var newSettings = new AWSettings();
            newSettings.ToFile();
            return newSettings;
        }

        static AWSettings()
        {
            SettingsDirectory = Environment.CurrentDirectory;
        }

        public AWSettings()
        {
            Sound = new SoundSettings();
            Net = new NetSettings();
            Graphics = new GraphicsSettings();
            Controls = new ControlsSettings();
        }

        public void ToFile()
        {
            TypeLoader.SaveTemplate(this, SettingsFilename, typeof(AWSettings), null);
        }

        public void Reset()
        {
            Sound.Reset();
            Net.Reset();
            Graphics.Reset();
            Controls.Reset();
        }
    }
}
