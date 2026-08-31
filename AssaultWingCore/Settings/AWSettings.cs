using System;
using System.IO;
using System.Text;
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
#if BROWSER
            var storedSettings = BrowserInterop.LoadSettings();
            if (!string.IsNullOrWhiteSpace(storedSettings))
            {
                // StringWriter declares UTF-16 in the XML header. Recreate the
                // matching byte stream, including its byte-order mark.
                var preamble = Encoding.Unicode.GetPreamble();
                var payload = Encoding.Unicode.GetBytes(storedSettings);
                var bytes = new byte[preamble.Length + payload.Length];
                Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
                Buffer.BlockCopy(payload, 0, bytes, preamble.Length, payload.Length);
                var stream = new MemoryStream(bytes);
                var settings = (AWSettings)TypeLoader.LoadTemplate(stream, "browser local storage",
                    typeof(AWSettings), typeof(TypeParameterAttribute), true);
                if (settings != null)
                {
                    settings.Filename = filename;
                    settings.Validate(game);
                    Log.Write("Loaded settings from browser local storage.");
                    return settings;
                }
                Log.Write("Errors while reading settings from browser local storage.");
            }
#else
            if (ContentFileSystem.Exists(filename))
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
#endif
            var graphicsEnabled = !game.CommandLineOptions.DedicatedServer;
            Log.Write("Creating a new settings file " + filename);
            var newSettings = new AWSettings(graphicsEnabled: graphicsEnabled) { Filename = filename };
            newSettings.Reset(game);
#if !BROWSER
            newSettings.ToFile();
#endif
            return newSettings;
        }

        public AWSettings(bool graphicsEnabled)
        {
            Sound = new SoundSettings();
            Net = new NetSettings();
            Graphics = new GraphicsSettings(graphicsEnabled: graphicsEnabled);
            Players = new PlayerSettings();
            Controls = new ControlsSettings();
            System = new SystemSettings();
        }

        public void ToFile()
        {
#if BROWSER
            var writer = new StringWriter();
            TypeLoader.SaveTemplate(this, writer, typeof(AWSettings), typeof(TypeParameterAttribute));
            BrowserInterop.SaveSettings(writer.ToString());
            Log.Write("Saved settings to browser local storage.");
#else
            Log.Write("Saving settings to file");
            TypeLoader.SaveTemplate(this, Filename, typeof(AWSettings), typeof(TypeParameterAttribute));
#endif
        }

        public void Reset(AssaultWingCore game)
        {
            Sound.Reset();
            Net.Reset();
            Graphics.Reset();
            Players.Reset(game);
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
