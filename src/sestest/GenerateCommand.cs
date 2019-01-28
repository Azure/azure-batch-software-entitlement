using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Functionality for the <c>generate</c> command
    /// </summary>
    public sealed class GenerateCommand : CommandBase
    {
        // Store used to scan for and obtain certificates
        private static readonly CertificateStore CertificateStore = new CertificateStore();

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateCommand"/> class
        /// </summary>
        /// <param name="logger">A logger to use while executing.</param>
        public GenerateCommand(ILogger logger) : base(logger)
        {
        }

        /// <summary>
        /// Generate a new token
        /// </summary>
        /// <param name="commandLine">Configuration from the command line.</param>
        /// <returns>Results of execution (0 = success).</returns>
        public int Execute(GenerateCommandLine commandLine)
        {
            var provider = new CommandLineEntitlementPropertyProvider(commandLine);

            var resultCodeOrFailure =
                from props in EntitlementTokenProperties.Build(provider)
                join signingCert in FindCertificate("signing", commandLine.SignatureThumbprint) on true equals true
                join encryptionCert in FindCertificate("encryption", commandLine.EncryptionThumbprint) on true equals true
                let token = GenerateToken(props, signingCert, encryptionCert)
                let resultCode = ReturnToken(token, commandLine)
                select resultCode;

            return resultCodeOrFailure.LogIfFailed(Logger, ResultCodes.Failed);
        }

        private int ReturnToken(string token, GenerateCommandLine commandLine)
        {
            if (string.IsNullOrEmpty(commandLine.TokenFile))
            {
                Logger.LogInformation("Token: {JWT}", token);
                return ResultCodes.Success;
            }

            var fileInfo = new FileInfo(commandLine.TokenFile);
            Logger.LogInformation("Token file: {FileName}", fileInfo.FullName);
            try
            {
                File.WriteAllText(fileInfo.FullName, token);
                return ResultCodes.Success;
            }
            catch (Exception ex)
            {
                Logger.LogError(0, ex, ex.Message);
                return ResultCodes.InternalError;
            }
        }

        /// <summary>
        /// Generate a token
        /// </summary>
        /// <param name="tokenProperties">The properties to encode into the token.</param>
        /// <param name="signingCert">Certificate to use when signing the token (optional).</param>
        /// <param name="encryptionCert">Certificate to use when encrypting the token (optional).</param>
        /// <returns>Generated token, if any; otherwise all related errors.</returns>
        private string GenerateToken(
            EntitlementTokenProperties tokenProperties,
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

            var generator = new TokenGenerator(Logger, signingCredentials, encryptingCredentials);
            return generator.Generate(tokenProperties);
        }

        private static Result<X509Certificate2, ErrorCollection> FindCertificate(string purpose, string thumbprint)
        {
            if (string.IsNullOrEmpty(thumbprint))
            {
                // No certificate requested, so we successfully return null
                return Errorable.Success<X509Certificate2>(null);
            }

            var t = new CertificateThumbprint(thumbprint);
            return CertificateStore.FindByThumbprint(purpose, t);
        }
    }
}
