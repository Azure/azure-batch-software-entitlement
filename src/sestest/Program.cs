using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using CommandLine;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public static class Program
    {
        // Logger used for program output
        private static ILogger _logger;

        // Store used to scan for and obtain certificates
        private static readonly CertificateStore _certificateStore = new CertificateStore();

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
            var signingCert = FindCertificate("signing", commandLine.SignatureThumbprint);
            var encryptionCert = FindCertificate("encryption", commandLine.EncryptionThumbprint);

            var result = entitlement.Combine(signingCert, encryptionCert, GenerateToken);

            if (!result.HasValue)
            {
                return LogErrors(result.Errors);
            }

            var token = result.Value;
            if (string.IsNullOrEmpty(commandLine.TokenFile))
            {
                _logger.LogInformation("Token: {JWT}", token);
                return 0;
            }

            var fileInfo = new FileInfo(commandLine.TokenFile);
            _logger.LogInformation("Token file: {filename}", fileInfo.FullName);
            try
            {
                File.WriteAllText(fileInfo.FullName, token);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Generate a token
        /// </summary>
        /// <param name="entitlements">Details of the entitlements to encode into the token.</param>
        /// <param name="signingCert">Certificate to use when signing the token (optional).</param>
        /// <param name="encryptionCert">Certificate to use when encrypting the token (optional).</param>
        /// <returns>Generated token, if any; otherwise all related errors.</returns>
        private static string GenerateToken(
            NodeEntitlements entitlements,
            X509Certificate2 signingCert = null,
            X509Certificate2 encryptionCert = null)
        {
            SigningCredentials signingCredentials = null;
            if (signingCert != null)
            {
                var signingKey = new X509SecurityKey(signingCert);
                signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha512Signature);
            }

            EncryptingCredentials encryptingCredentials = null;
            if (encryptionCert != null)
            {
                var encryptionKey = new X509SecurityKey(encryptionCert);
                encryptingCredentials = new EncryptingCredentials(
                    encryptionKey, SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256CbcHmacSha512);
            }

            var generator = new TokenGenerator(GlobalLogger.Logger, signingCredentials, encryptingCredentials);
            return generator.Generate(entitlements);
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
            var server = new SoftwareEntitlementServer(options);
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

            _logger.LogTable(
                LogLevel.Information,
                withPrivateKey.Select(DescribeCertificate).ToList());

            return 0;
        }

        private static IList<string> DescribeCertificate(X509Certificate2 cert)
        {
            return new List<string>
            {
                cert.SubjectName.Name,
                cert.FriendlyName,
                cert.Thumbprint
            };
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
            var level = LogLevel.Information;
            var parseFailed = false;

            if (!string.IsNullOrEmpty(commandLine.LogLevel))
            {
                if (!Enum.TryParse(commandLine.LogLevel, true, out level))
                {
                    level = LogLevel.Information;
                    parseFailed = true;
                }
            }

            if (string.IsNullOrEmpty(commandLine.LogFile))
            {
                _logger = GlobalLogger.CreateLogger(level);
            }
            else
            {
                var file = new FileInfo(commandLine.LogFile);
                _logger = GlobalLogger.CreateLogger(level, file);
            }

            const string header = "Software Entitlement Service Test Utility";
            _logger.LogInformation(new string('-', header.Length));
            _logger.LogInformation(header);
            _logger.LogInformation(new string('-', header.Length));

            if (parseFailed)
            {
                _logger.LogWarning("Failed to recognise log level '{level}'; defaulting to {default}", level, "Information");
            }
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

        private static Errorable<X509Certificate2> FindCertificate(string purpose, string thumbprint)
        {
            if (string.IsNullOrEmpty(thumbprint))
            {
                // No certificate requested, so we successfully return null
                return Errorable.Success<X509Certificate2>(null);
            }

            var t = new CertificateThumbprint(thumbprint);
            return _certificateStore.FindByThumbprint(purpose, t);
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
