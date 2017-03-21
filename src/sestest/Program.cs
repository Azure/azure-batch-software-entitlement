using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public static class Program
    {
        static int Main(string[] args)
        {
            var parser = new Parser(ConfigureParser);

            var parseResult = parser
                .ParseArguments<GenerateCommandLine, ServerCommandLine, ListCertificatesCommandLine, FindCertificateCommandLine>(args);

            parseResult.WithParsed((CommandLineBase options) => SetUpLogging(options));

            var exitCode = parseResult.MapResult(
                (GenerateCommandLine options) => RunMode(options, Generate),
                (ServerCommandLine options) => RunMode(options, Serve),
                (ListCertificatesCommandLine options) => RunMode(options, ListCertificates),
                (FindCertificateCommandLine options) => RunMode(options, FindCertificate),
                errors => 1);

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
            }

            return exitCode;
        }

        /// <summary>
        /// Generation mode - create a new token for testing
        /// </summary>
        /// <param name="commandLine">Options from the command line.</param>
        /// <param name="logger">Logger to use for diagnostics.</param>
        /// <returns>Exit code to return from this process.</returns>
        public static int Generate(GenerateCommandLine commandLine, ILogger logger)
        {
            var entitlement = new SoftwareEntitlement(logger)
                .WithVirtualMachineId(commandLine.VirtualMachineId)
                .ForTimeRange(commandLine.NotBefore, commandLine.NotAfter);

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

        /// <summary>
        /// Serve mode - run as a standalone webapp  
        /// </summary>
        /// <param name="commandLine">Options from the commandline.</param>
        /// <param name="logger">Logger to use for diagnostics.</param>
        /// <returns>Exit code to return from this process.</returns>
        public static int Serve(ServerCommandLine commandLine, ILogger logger)
        {
            var options = ServerOptionBuilder.Build(commandLine);

            if (!options.HasValue)
            {
                foreach (var e in options.Errors)
                {
                    logger.LogError(e);
                }

                return -1;
            }

            var server = new SoftwareEntitlementServer(options.Value, GlobalLogger.Logger);
            server.Run();
            return 0;
        }

        /// <summary>
        /// List available certificates
        /// </summary>
        /// <param name="commandLine">Options from the command line.</param>
        /// <param name="logger">Logger to use for diagnostics.</param>
        /// <returns>Exit code for process.</returns>
        private static int ListCertificates(ListCertificatesCommandLine commandLine, ILogger logger)
        {
            var certificateStore = new CertificateStore();
            var allCertificates = certificateStore.FindAll();
            var withPrivateKey = allCertificates.Where(c => c.HasPrivateKey).ToList();
            logger.LogInformation("Found {count} certificates with private keys", withPrivateKey.Count);

            foreach (var cert in withPrivateKey)
            {
                var name = string.IsNullOrEmpty(cert.FriendlyName)
                        ? cert.SubjectName.Name
                        : cert.FriendlyName;

                logger.LogInformation(
                    "{Name} - {Thumbprint}",
                    name,
                    cert.Thumbprint);
            }

            return 0;
        }

        /// <summary>
        /// Show details of one particular certificate
        /// </summary>
        /// <param name="commandLine">Options from the command line.</param>
        /// <param name="logger">Logger to use for diagnostics.</param>
        /// <returns>Exit code for process.</returns>
        private static int FindCertificate(FindCertificateCommandLine commandLine, ILogger logger)
        {
            var thumbprint = new CertificateThumbprint(commandLine.Thumbprint);
            var certificateStore = new CertificateStore();
            var certificate = certificateStore.FindByThumbprint("cert", thumbprint);
            if (!certificate.HasValue)
            {
                logger.LogError("Failed to find certificate {Thumbprint}", thumbprint);
                foreach (var error in certificate.Errors)
                {
                    logger.LogError(error);
                }

                return -1;
            }

            var certDetails = certificate.Value.ToString()
                    .Split(new[] { Environment.NewLine, "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var line in certDetails)
            {
                logger.LogInformation(line);
            }

            return 0;
        }

        /// <summary>
        /// Ensure our logging is properly initialized
        /// </summary>
        /// <param name="commandLine">Options selected by the user (if any).</param>
        private static void SetUpLogging(CommandLineBase commandLine)
        {
            ILogger logger;
            if (string.IsNullOrEmpty(commandLine.LogFile))
            {
                logger = GlobalLogger.CreateLogger(commandLine.LogLevel);
            }
            else
            {
                var file = new FileInfo(commandLine.LogFile);
                logger = GlobalLogger.CreateLogger(commandLine.LogLevel, file);
            }

            logger.LogInformation("Software Entitlement Service Command Line Utility");
        }

        /// <summary>
        /// Configure parsing of our commandline options
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

        private static int RunMode<T>(T commandLine, Func<T, ILogger, int> mode)
            where T : CommandLineBase
        {
            var logger = GlobalLogger.Logger;
            try
            {
                return mode(commandLine, logger);
            }
            catch (Exception ex)
            {
                logger.LogError("Exception", ex, ex.Message);
                return -1;
            }
        }
    }
}
