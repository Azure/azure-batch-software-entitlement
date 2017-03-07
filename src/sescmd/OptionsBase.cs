using CommandLine;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Serilog.Events;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public class OptionsBase
    {
        [Option("quiet", HelpText = "Suppress most logging output (errors and warnings will still show).")]
        public bool Quiet { get; set; }

        [Option("verbose", HelpText = "Show verbose logging.")]
        public bool Verbose { get; set; }

        [Option("debug", HelpText = "Show debug logging as well (maximum information).")]
        public bool Debug { get; set; }

        /// <summary>
        /// Work out our actual log level based on the arguments used on the command line
        /// </summary>
        /// <returns>Selected log level to use for this execution.</returns>
        public LogEventLevel SelectLogEventLevel()
        {
            if (Debug)
            {
                return LogEventLevel.Debug;
            }

            if (Verbose)
            {
                return LogEventLevel.Verbose;
            }

            if (Quiet)
            {
                return LogEventLevel.Warning;
            }

            return LogEventLevel.Information;
        }

        /// <summary>
        /// Generate warnings if any of our log level options on the command line are being ignored
        /// </summary>
        /// <param name="actualLevel"></param>
        /// <param name="logger"></param>
        public void WarnAboutInactiveOptions(LogEventLevel actualLevel, ISimpleLogger logger)
        {
            if (Quiet && actualLevel < LogEventLevel.Warning)
            {
                logger.Warning("Logging at {Level}; ignoring --quiet", actualLevel);
            }

            if (Debug && actualLevel > LogEventLevel.Debug)
            {
                logger.Warning("Logging at {Level}; ignoring --debug", actualLevel);
            }

            if (Verbose && actualLevel > LogEventLevel.Verbose)
            {
                logger.Warning("Logging at {Level}; ignoring --verbose", actualLevel);
            }
        }
    }
}
