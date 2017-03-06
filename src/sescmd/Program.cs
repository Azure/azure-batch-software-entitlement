using System;
using System.Diagnostics;
using CommandLine;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Serilog;
using Serilog.Events;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public class Program
    {
        static int Main(string[] args)
        {
            var result = Parser.Default
                .ParseArguments<GenerateOptions, VerifyOptions, ServerOptions>(args)
                .MapResult(
                    (GenerateOptions options) => Generate(options),
                    (VerifyOptions options) => Verify(options),
                    (ServerOptions options) => Serve(options),
                    errors => 1);

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
            }

            return result;
        }

        public static int Generate(GenerateOptions options)
        {
            var logger = CreateLogger(options);

            return 0;
        }

        public static int Verify(VerifyOptions options)
        {
            var logger = CreateLogger(options);
            return 0;
        }

        public static int Serve(ServerOptions options)
        {
            var logger = CreateLogger(options);
            return 0;
        }

        public static ISimpleLogger CreateLogger(OptionsBase options)
        {
            var level = SelectLogEventLevel(options);
            var logger = CreateLogger(level);
            WarnAboutInactiveOptions(options, level, logger);
            return logger;
        }

        private static ISimpleLogger CreateLogger(LogEventLevel level)
        {
            var serilogLogger = new LoggerConfiguration()
                            .WriteTo.LiterateConsole()
                            .MinimumLevel.Is(level)
                            .CreateLogger();

            serilogLogger.Information("Software Entitlement Service Command Line Utility");
            return new SerilogLogger(serilogLogger);
        }

        public static LogEventLevel SelectLogEventLevel(OptionsBase options)
        {
            if (options.Debug)
            {
                return LogEventLevel.Debug;
            }

            if (options.Verbose)
            {
                return LogEventLevel.Verbose;
            }

            if (options.Quiet)
            {
                return LogEventLevel.Warning;
            }

            return LogEventLevel.Information;
        }

        private static void WarnAboutInactiveOptions(OptionsBase options, LogEventLevel level, ISimpleLogger logger)
        {
            if (options.Quiet && level < LogEventLevel.Warning)
            {
                logger.Warning("Logging at {Level}; ignoring --quiet", level);
            }

            if (options.Debug && level > LogEventLevel.Debug)
            {
                logger.Warning("Logging at {Level}; ignoring --debug", level);
            }

            if (options.Verbose && level > LogEventLevel.Verbose)
            {
                logger.Warning("Logging at {Level}; ignoring --verbose", level);
            }
        }
    }
}
