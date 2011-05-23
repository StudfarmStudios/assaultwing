using System;
using System.Linq;

namespace AW2.Core
{
    /// <summary>
    /// A collection of options that you can set as command line arguments.
    /// </summary>
    public class CommandLineOptions
    {
        public bool DedicatedServer { get; set; }
        public bool SaveTemplates { get; set; }
        public bool DeleteTemplates { get; set; }
        public string ArenaFilename { get; set; }

        public CommandLineOptions(string[] commandLineArgs)
        {
            DedicatedServer = commandLineArgs.Contains("--dedicated_server");
            SaveTemplates = commandLineArgs.Contains("--save_templates");
            DeleteTemplates = commandLineArgs.Contains("--delete_templates");
            ArenaFilename = GetArgValue(commandLineArgs, "--arena");
        }

        private string GetArgValue(string[] commandLineArgs, string argName)
        {
            int index = Array.IndexOf(commandLineArgs, argName);
            return index >= 0 ? commandLineArgs[index + 1] : null;
        }
    }
}
