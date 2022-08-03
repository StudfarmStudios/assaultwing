using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;

namespace AW2.Core
{
    /// <summary>
    /// A collection of options that the user can pass to the program.
    /// </summary>
    public class CommandLineOptions
    {
        public class QuickStartOptions
        {
            public string[] GameServerEndPoints { get; set; }
            public string GameServerName { get; set; }
            public string ShipName { get; set; }
            public string Weapon2Name { get; set; }
            public string ExtraDeviceName { get; set; }
        }

        private class ProgramArgs
        {
            private string[] _commandLineArgs;
            private NameValueCollection _queryStringParams;
            private string _argumentText;

            public ProgramArgs(string[] commandLineArgs, NameValueCollection queryStringParams, string argumentText)
            {
                _commandLineArgs = commandLineArgs;
                _queryStringParams = queryStringParams;
                _argumentText = argumentText;
            }

            public bool IsSet(string flag)
            {
                return _commandLineArgs.Contains("--" + flag)
                    || _queryStringParams[flag] != null
                    || new Regex("^" + flag + "( *|=).*$", RegexOptions.Multiline | RegexOptions.CultureInvariant).IsMatch(_argumentText);
            }

            public string GetValue(string key)
            {
                int index = Array.IndexOf(_commandLineArgs, "--" + key);
                if (index >= 0) return _commandLineArgs[index + 1];
                if (_queryStringParams[key] != null) return _queryStringParams[key];
                var match = new Regex("^" + key + " *(= *)?(.*)", RegexOptions.Multiline | RegexOptions.CultureInvariant).Match(_argumentText);
                if (match.Success) return match.Groups[2].Captures[0].Value;
                return null;
            }

            public string[] GetValues(string key)
            {
                var catenated = GetValue(key);
                if (catenated == null) return new string[0];
                return catenated.Split(',');
            }
        }

        public bool DedicatedServer { get; private set; }
        public bool SaveTemplates { get; private set; }
        public bool DeleteTemplates { get; private set; }
        public string ArenaFilename { get; private set; }

        /// <summary>
        /// Null or options for quick start.
        /// </summary>
        public QuickStartOptions QuickStart { get; private set; }

        public CommandLineOptions(string[] commandLineArgs, NameValueCollection queryParams, string argumentText)
        {
            var args = new ProgramArgs(commandLineArgs, queryParams, argumentText);
            DedicatedServer = args.IsSet("dedicated_server");
            SaveTemplates = args.IsSet("save_templates");
            DeleteTemplates = args.IsSet("delete_templates");
            ArenaFilename = args.GetValue("arena");
            if (args.IsSet("quickstart")) QuickStart = new QuickStartOptions
            {
                GameServerEndPoints = args.GetValues("server"),
                GameServerName = args.GetValue("server_name") ?? "Some Server",
                ShipName = args.GetValue("ship"),
                Weapon2Name = args.GetValue("weapon"),
                ExtraDeviceName = args.GetValue("mod"),
            };
        }

        // Dedicated server command line
        public CommandLineOptions(string[] commandLineArgs)
        {
            var args = new ProgramArgs(commandLineArgs, new NameValueCollection(), "");
            DedicatedServer = true;
            SaveTemplates = args.IsSet("save_templates");
            DeleteTemplates = args.IsSet("delete_templates");
            ArenaFilename = args.GetValue("arena");
            if (args.IsSet("quickstart")) QuickStart = new QuickStartOptions
            {
                GameServerEndPoints = args.GetValues("server"),
                GameServerName = args.GetValue("server_name") ?? "Some Server",
                ShipName = args.GetValue("ship"),
                Weapon2Name = args.GetValue("weapon"),
                ExtraDeviceName = args.GetValue("mod"),
            };
        }
    }
}
