using System;
using System.Linq;

namespace AW2.Core
{
    /// <summary>
    /// A collection of options that you can set as command line arguments.
    /// </summary>
    public class CommandLineOptions
    {
        public bool PerformanceCounters { get; set; }
        public bool SaveTemplates { get; set; }
        public bool DeleteTemplates { get; set; }

        public CommandLineOptions(string[] commandLineArgs)
        {
            PerformanceCounters = commandLineArgs.Contains("--performance_counters");
            SaveTemplates = commandLineArgs.Contains("--save_templates");
            DeleteTemplates = commandLineArgs.Contains("--delete_templates");
        }
    }
}
