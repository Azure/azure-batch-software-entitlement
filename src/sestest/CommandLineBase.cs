using CommandLine;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Base class with common options shared across command line modes
    /// </summary>
    public class CommandLineBase
    {
        [Option("log-level", HelpText = "Specify the level of logging output (one of error, warning, information or debug; defaults to information)")]
        public string LogLevel { get; set; }

        [Option("log-file", HelpText = "Specify a file into which log messages should be written.")]
        public string LogFile { get; set; }
    }
}
