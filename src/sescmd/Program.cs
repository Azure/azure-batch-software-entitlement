﻿using System;
using System.Diagnostics;
using CommandLine;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

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
            var entitlement = new SoftwareEntitlement(logger)
                .WithVirtualMachineId(options.VirtualMachineId)
                .ForTimeRange(options.NotBefore, options.NotAfter);

            if (entitlement.HasErrors)
            {
                logger.LogError("Unable to generate template; please address the reported errors and try again.");
                return -1;
            }

            var generator = new TokenGenerator(logger);
            var token = generator.Generate(entitlement);
            if (token == null)
            {
                return -1;
            }

            logger.LogInformation("Token: {JWT}", token);

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

        public static ILogger CreateLogger(OptionsBase options)
        {
            var level = SelectLogEventLevel(options);
            var logger = CreateLogger(level);
            WarnAboutInactiveOptions(options, level, logger);
            return logger;
        }

        private static ILogger CreateLogger(LogEventLevel level)
        {
            var serilogLogger = new LoggerConfiguration()
                            .WriteTo.LiterateConsole()
                            .MinimumLevel.Is(level)
                            .CreateLogger();
            var provider = new SerilogLoggerProvider(serilogLogger);
            return provider.CreateLogger(string.Empty);
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

        private static void WarnAboutInactiveOptions(OptionsBase options, LogEventLevel level, ILogger logger)
        {
            if (options.Quiet && level < LogEventLevel.Warning)
            {
                logger.LogWarning("Logging at {Level}; ignoring --quiet", level);
            }

            if (options.Debug && level > LogEventLevel.Debug)
            {
                logger.LogWarning("Logging at {Level}; ignoring --debug", level);
            }

            if (options.Verbose && level > LogEventLevel.Verbose)
            {
                logger.LogWarning("Logging at {Level}; ignoring --verbose", level);
            }
        }
    }
}
