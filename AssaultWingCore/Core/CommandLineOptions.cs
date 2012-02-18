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
                    || new Regex("^" + flag + "( *|=).*$", RegexOptions.Multiline).IsMatch(_argumentText);
            }

            public string GetValue(string key)
            {
                int index = Array.IndexOf(_commandLineArgs, "--" + key);
                if (index >= 0) return _commandLineArgs[index + 1];
                if (_queryStringParams[key] != null) return _queryStringParams[key];
                var match = new Regex("^" + key + " *(= *)?(.*)", RegexOptions.Multiline).Match(_argumentText);
                if (match.Success) return match.Groups[2].Captures[0].Value;
                return null;
            }
        }

        public bool DedicatedServer { get; set; }
        public bool SaveTemplates { get; set; }
        public bool DeleteTemplates { get; set; }
        public string ArenaFilename { get; set; }

        public CommandLineOptions(string[] commandLineArgs, NameValueCollection queryParams, string argumentText)
        {
            var args = new ProgramArgs(commandLineArgs, queryParams, argumentText);
            DedicatedServer = args.IsSet("dedicated_server");
            SaveTemplates = args.IsSet("save_templates");
            DeleteTemplates = args.IsSet("delete_templates");
            ArenaFilename = args.GetValue("arena");
        }
    }
}
