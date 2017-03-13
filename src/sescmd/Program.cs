using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public class Program
    {
        static int Main(string[] args)
        {
            var parser = new Parser(ConfigureParser);

            var parseResult = parser
                .ParseArguments<GenerateOptions, VerifyOptions, ServerOptions, ListCertificatesOptions, FindCertificateOptions>(args);

            parseResult.WithParsed((OptionsBase options) => SetUpLogging(options));

            var exitCode = parseResult.MapResult(
                (GenerateOptions options) => Generate(options),
                (VerifyOptions options) => Verify(options),
                (ServerOptions options) => Serve(options),
                (ListCertificatesOptions options) => ListCertificates(options),
                (FindCertificateOptions options) => FindCertificate(options),
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
        /// <param name="options">Options from the command line.</param>
        /// <returns>Exit code to return from this process.</returns>
        public static int Generate(GenerateOptions options)
        {
            var logger = GlobalLogger.Logger;
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

        /// <summary>
        /// Verify mode - check that a token is valid
        /// </summary>
        /// <param name="options">Options from the command line.</param>
        /// <returns>Exit code to return from this process.</returns>
        public static int Verify(VerifyOptions options)
        {
            return 0;
        }

        /// <summary>
        /// Serve mode - run as a standalone webapp  
        /// </summary>
        /// <param name="options">Options from the commandline.</param>
        /// <returns>Exit code to return from this process.</returns>
        public static int Serve(ServerOptions options)
        {
            var server = new SoftwareEntitlementServer(options, GlobalLogger.Logger);
            server.Run();

            return 0;
        }

        /// <summary>
        /// List available certificates
        /// </summary>
        /// <param name="options">Options from the command line.</param>
        /// <returns>Exit code for process.</returns>
        private static int ListCertificates(ListCertificatesOptions options)
        {
            var logger = GlobalLogger.Logger;
            var certificateStore = new CertificateStore(logger);
            foreach (var cert in certificateStore.FindAll())
            {
                var name
                    = string.IsNullOrEmpty(cert.FriendlyName)
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
        /// <param name="options">Options from the command line.</param>
        /// <returns>Exit code for process.</returns>

        private static int FindCertificate(FindCertificateOptions options)
        {
            var logger = GlobalLogger.Logger;
            var thumbprint = new CertificateThumbprint(options.Thumbprint);
            var certificateStore = new CertificateStore(logger);
            var certificate = certificateStore.FindByThumbprint(thumbprint);
            if (certificate == null)
            {
                return -1;
            }

            var certDetails
                = certificate.ToString()
                    .Split(new[] {Environment.NewLine, "\r\n", "\n"}, StringSplitOptions.None);
            foreach (var line in certDetails)
            {
                logger.LogInformation(line);
            }

            return 0;
        }

        /// <summary>
        /// Ensure our logging is properly initialized
        /// </summary>
        /// <param name="options">Options selected by the user (if any).</param>
        private static void SetUpLogging(OptionsBase options)
        {
            var logger = GlobalLogger.CreateLogger(options.LogLevel);
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


    }
}
