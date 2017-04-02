using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CommandLine;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public static class Program
    {
        private static ILogger _logger;

        public static int Main(string[] args)
        {
            var parser = new Parser(ConfigureParser);

            var parseResult = parser
                .ParseArguments<GenerateCommandLine, ServerCommandLine, ListCertificatesCommandLine, FindCertificateCommandLine>(args);

            parseResult.WithParsed((CommandLineBase options) => ConfigureLogging(options));

            var exitCode = parseResult.MapResult(
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

            return exitCode;
        }

        /// <summary>
        /// Generation mode - create a new token for testing
        /// </summary>
        /// <param name="commandLine">Options from the command line.</param>
        /// 
        /// <returns>Exit code to return from this process.</returns>
        public static int Generate(GenerateCommandLine commandLine)
        {
            var entitlement = NodeEntitlementsBuilder.Build(commandLine);
            return entitlement.Match(GenerateToken, LogErrors);
        }

        private static int GenerateToken(NodeEntitlements entitlements)
        {
            // Hard coded for now, will use certificates later on
            var plainTextSecurityKey = "This is my shared, not so secret, secret!";
            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(plainTextSecurityKey));

            var generator = new TokenGenerator(signingKey, GlobalLogger.Logger);
            var token = generator.Generate(entitlements);
            if (token == null)
            {
                return -1;
            }

            _logger.LogInformation("Token: {JWT}", token);
            return 0;
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
            var server = new SoftwareEntitlementServer(options, GlobalLogger.Logger);
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
            var certificateStore = new CertificateStore();
            var allCertificates = certificateStore.FindAll();
            var withPrivateKey = allCertificates.Where(c => c.HasPrivateKey).ToList();
            _logger.LogInformation("Found {count} certificates with private keys", withPrivateKey.Count);

            foreach (var cert in withPrivateKey)
            {
                var name = string.IsNullOrEmpty(cert.FriendlyName)
                        ? cert.SubjectName.Name
                        : cert.FriendlyName;

                _logger.LogInformation(
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
        /// <returns>Exit code for process.</returns>
        private static int FindCertificate(FindCertificateCommandLine commandLine)
        {
            var thumbprint = new CertificateThumbprint(commandLine.Thumbprint);
            var certificateStore = new CertificateStore();
            var certificate = certificateStore.FindByThumbprint("cert", thumbprint);
            return certificate.Match(ShowCertificate, LogErrors);
        }

        private static int ShowCertificate(X509Certificate2 certificate)
        {
            foreach (var line in certificate.ToString().AsLines())
            {
                _logger.LogInformation(line);
            }

            return 0;
        }

        /// <summary>
        /// Configure our logging as requested by the user
        /// </summary>
        /// <param name="commandLine">Options selected by the user (if any).</param>
        private static void ConfigureLogging(CommandLineBase commandLine)
        {
            if (string.IsNullOrEmpty(commandLine.LogFile))
            {
                _logger = GlobalLogger.CreateLogger(commandLine.LogLevel);
            }
            else
            {
                var file = new FileInfo(commandLine.LogFile);
                _logger = GlobalLogger.CreateLogger(commandLine.LogLevel, file);
            }

            _logger.LogInformation("Software Entitlement Service Command Line Utility");
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
        /// <typeparam name="T">Type of parametrs provided for this command to run</typeparam>
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
                _logger.LogError("Exception", ex, ex.Message);
                return -1;
            }
        }

        private static int LogErrors(IEnumerable<string> errors)
        {
            foreach (var e in errors)
            {
                _logger.LogError(e);
            }

            return -1;
        }
    }
}
