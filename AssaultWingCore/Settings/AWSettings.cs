using System;
using System.IO;
using AW2.Core;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Settings
{
    public class AWSettings
    {
        public SoundSettings Sound { get; private set; }
        public NetSettings Net { get; private set; }
        public GraphicsSettings Graphics { get; private set; }
        public PlayerSettings Players { get; private set; }
        public ControlsSettings Controls { get; private set; }
        public SystemSettings System { get; private set; }

        private string Filename { get; set; }
        private static string GetSettingsFilename(string directory) { return Path.Combine(directory, "AssaultWing_config.xml"); }

        public static AWSettings FromFile(AssaultWingCore game, string directory)
        {
            var filename = GetSettingsFilename(directory);
            if (File.Exists(filename))
            {
                var settings = (AWSettings)TypeLoader.LoadTemplate(filename, typeof(AWSettings), typeof(TypeParameterAttribute), true);
                if (settings != null)
                {
                    settings.Filename = filename;
                    settings.Validate(game);
                    return settings;
                }
                Log.Write("Errors while reading settings from " + filename);
            }
            Log.Write("Creating a new settings file " + filename);
            var newSettings = new AWSettings { Filename = filename };
            newSettings.ToFile();
            return newSettings;
        }

        public AWSettings()
        {
            Sound = new SoundSettings();
            Net = new NetSettings();
            Graphics = new GraphicsSettings();
            Players = new PlayerSettings();
            Controls = new ControlsSettings();
            System = new SystemSettings();
        }

        public void ToFile()
        {
            Log.Write("Saving settings to file");
            TypeLoader.SaveTemplate(this, Filename, typeof(AWSettings), typeof(TypeParameterAttribute));
        }

        public void Reset()
        {
            Sound.Reset();
            Net.Reset();
            Graphics.Reset();
            Players.Reset();
            Controls.Reset();
            System.Reset();
        }

        public void Validate(AssaultWingCore game)
        {
            Graphics.Validate();
            Players.Validate(game);
        }
    }
}
