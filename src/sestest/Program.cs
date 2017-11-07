using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CommandLine;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public static class Program
    {
        // Logger used for program output
        private static ILogger _logger;

        // Provider used for ASP.NET logging
        private static ILoggerProvider _provider;

        public static int Main(string[] args)
        {
            var parser = new Parser(ConfigureParser);

            var parseResult = parser
                .ParseArguments<GenerateCommandLine, ServerCommandLine, ListCertificatesCommandLine, FindCertificateCommandLine>(args);

            parseResult.WithParsed((CommandLineBase options) => ConfigureLogging(options));

            var exitCode = -1;
            if (_logger != null)
            {
                // Logging is ready for use, can try other things

                exitCode = parseResult.MapResult(
                    (GenerateCommandLine commandLine) => RunCommand(Generate, commandLine),
                    (ServerCommandLine commandLine) => RunCommand(Serve, commandLine),
                    (ListCertificatesCommandLine commandLine) => RunCommand(ListCertificates, commandLine),
                    (FindCertificateCommandLine commandLine) => RunCommand(FindCertificate, commandLine),
                    errors => 1);

                if (Debugger.IsAttached)
                {
                    Console.WriteLine("Press enter to exit.");
                    Console.ReadLine();
                }
            }

            return exitCode;
        }

        /// <summary>
        /// Generation mode - create a new token for testing
        /// </summary>
        /// <param name="commandLine">Options from the command line.</param>
        /// <returns>Exit code to return from this process.</returns>
        public static int Generate(GenerateCommandLine commandLine)
        {
            var command = new GenerateCommand(_logger);
            return command.Execute(commandLine);
        }

        /// <summary>
        /// Serve mode - run as a standalone web server
        /// </summary>
        /// <param name="commandLine">Options from the command-line.</param>
        /// <returns>Exit code to return from this process.</returns>
        public static int Serve(ServerCommandLine commandLine)
        {
            var options = ServerOptionBuilder.Build(commandLine);
            return options.Match(RunServer, LogErrors);
        }

        private static int RunServer(ServerOptions options)
        {
            var server = new SoftwareEntitlementServer(options, _provider);
            server.Run();
            return 0;
        }

        /// <summary>
        /// List available certificates
        /// </summary>
        /// <param name="commandLine">Options from the command line.</param>
        /// <returns>Exit code for process.</returns>
        private static int ListCertificates(ListCertificatesCommandLine commandLine)
        {
            var command = new ListCertificatesCommand(_logger);
            return command.Execute(commandLine);
        }

        /// <summary>
        /// Show details of one particular certificate
        /// </summary>
        /// <param name="commandLine">Options from the command line.</param>
        /// <returns>Exit code for process.</returns>
        private static int FindCertificate(FindCertificateCommandLine commandLine)
        {
            var command = new FindCertificateCommand(_logger);
            return command.Execute(commandLine);
        }

        /// <summary>
        /// Configure our logging as requested by the user
        /// </summary>
        /// <param name="commandLine">Options selected by the user (if any).</param>
        private static void ConfigureLogging(CommandLineBase commandLine)
        {
            var logSetup = new LoggerSetup();

            var consoleLevel = TryParseLogLevel(commandLine.LogLevel, "console", LogLevel.Information);
            var actualLevel = consoleLevel.HasValue ? consoleLevel.Value : LogLevel.Information;

            logSetup.SendToConsole(actualLevel);

            var fileLevel = TryParseLogLevel(commandLine.LogFileLevel, "file", actualLevel);
            if (!string.IsNullOrEmpty(commandLine.LogFile))
            {
                if (fileLevel.HasValue)
                {
                    var file = new FileInfo(commandLine.LogFile);
                    logSetup.SendToFile(file, fileLevel.Value);
                }
            }

            var logger = logSetup.Logger;
            logger.LogHeader("Software Entitlement Service Test Utility");
            logger.LogErrors(consoleLevel.Errors);
            logger.LogErrors(fileLevel.Errors);

            if (!consoleLevel.HasValue || !fileLevel.HasValue)
            {
                // A problem during setup, don't share our config
                return;
            }

            _logger = logger;
            _provider = logSetup.Provider;
        }

        /// <summary>
        /// Configure parsing of our command-line options
        /// </summary>
        /// <param name="settings">Settings instance to update.</param>
        private static void ConfigureParser(ParserSettings settings)
        {
            settings.CaseInsensitiveEnumValues = true;
            settings.CaseSensitive = false;
            settings.EnableDashDash = true;
            settings.IgnoreUnknownArguments = false;
            settings.HelpWriter = Console.Error;
        }

        /// <summary>
        /// Run a command with full logging
        /// </summary>
        /// <typeparam name="T">Type of parameters provided for this command to run</typeparam>
        /// <param name="command">Actual command to run.</param>
        /// <param name="commandLine">Parameters provided on the command line.</param>
        /// <returns>Exit code for this command.</returns>
        private static int RunCommand<T>(Func<T, int> command, T commandLine)
            where T : CommandLineBase
        {
            try
            {
                return command(commandLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, ex.Message);
                return -1;
            }
        }

        private static int LogErrors(IEnumerable<string> errors)
        {
            _logger.LogErrors(errors);
            return -1;
        }

        private static Errorable<LogLevel> TryParseLogLevel(string level, string purpose, LogLevel defaultLevel)
        {
            if (string.IsNullOrEmpty(level))
            {
                return Errorable.Success(defaultLevel);
            }

            if (Enum.TryParse<LogLevel>(level, true, out var result))
            {
                // Successfully parsed the string
                return Errorable.Success(result);
            }

            return Errorable.Failure<LogLevel>(
                $"Failed to recognize {purpose} log level '{level}'; valid choices are: error, warning, information, and debug.");
        }
    }
}
