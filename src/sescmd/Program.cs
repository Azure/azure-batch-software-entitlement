using System;
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
            var parser = new Parser(settings =>
            {
                settings.CaseInsensitiveEnumValues = true;
                settings.CaseSensitive = false;
            });

            var result = parser.ParseArguments<GenerateOptions, VerifyOptions, ServerOptions>(args)
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
            var logger = CreateLogger(options.LogLevel);
            return logger;
        }

        private static ILogger CreateLogger(LogLevel level)
        {
            var serilogLogger = new LoggerConfiguration()
                            .WriteTo.LiterateConsole()
                            .MinimumLevel.Is(ConvertLevel(level))
                            .CreateLogger();
            var provider = new SerilogLoggerProvider(serilogLogger);
            return provider.CreateLogger(string.Empty);
        }

        /// <summary>
        /// Convert from LogLevel to LogEventLevel for configuring our log
        /// </summary>
        /// <remarks>This should be available from Serilog but it's private.</remarks>
        /// <param name="level">Log level to convert.</param>
        /// <returns>Serilog equivalent.</returns>
        private static LogEventLevel ConvertLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Critical:
                    return LogEventLevel.Fatal;

                case LogLevel.Error:
                    return LogEventLevel.Error;

                case LogLevel.Warning:
                    return LogEventLevel.Warning;

                case LogLevel.Information:
                    return LogEventLevel.Information;

                case LogLevel.Debug:
                    return LogEventLevel.Debug;

                case LogLevel.None:
                default:
                    return LogEventLevel.Verbose;
            }
        }
    }
}
