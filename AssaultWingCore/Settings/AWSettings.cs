using System;
using System.IO;
using AW2.Core;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Settings
{
    [LimitedSerialization]
    public class AWSettings
    {
        [TypeParameter]
        private SoundSettings _sound;
        [TypeParameter]
        private NetSettings _net;
        [TypeParameter]
        private GraphicsSettings _graphics;
        [TypeParameter]
        private PlayerSettings _players;
        [TypeParameter]
        private ControlsSettings _controls;
        [TypeParameter]
        private SystemSettings _system;

        public SoundSettings Sound { get { return _sound; } private set { _sound = value; } }
        public NetSettings Net { get { return _net; } private set { _net = value; } }
        public GraphicsSettings Graphics { get { return _graphics; } private set { _graphics = value; } }
        public PlayerSettings Players { get { return _players; } private set { _players = value; } }
        public ControlsSettings Controls { get { return _controls; } private set { _controls = value; } }
        public SystemSettings System { get { return _system; } private set { _system = value; } }

        private string Filename { get; set; }

        private static string GetSettingsFilename(string directory) { return Path.Combine(directory, "AssaultWing_config.xml"); }

        public static AWSettings FromFile(AssaultWingCore game, string directory)
        {
            var filename = GetSettingsFilename(directory);
            if (File.Exists(filename))
            {
                var settings = (AWSettings)TypeLoader.LoadTemplate(filename, typeof(AWSettings), null, true);
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
