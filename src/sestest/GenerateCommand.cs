using System;
using System.Collections.Generic;
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
            Errorable<NodeEntitlements> entitlement = NodeEntitlementsBuilder.Build(commandLine);
            Errorable<X509Certificate2> signingCert = FindCertificate("signing", commandLine.SignatureThumbprint);
            Errorable<X509Certificate2> encryptionCert = FindCertificate("encryption", commandLine.EncryptionThumbprint);

            Errorable<string> result = entitlement.With(signingCert).With(encryptionCert)
                .Map(GenerateToken);

            if (!result.HasValue)
            {
                Logger.LogErrors(result.Errors);
                return ResultCodes.Failed;
            }

            var token = result.Value;
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
        /// <param name="entitlements">Details of the entitlements to encode into the token.</param>
        /// <param name="signingCert">Certificate to use when signing the token (optional).</param>
        /// <param name="encryptionCert">Certificate to use when encrypting the token (optional).</param>
        /// <returns>Generated token, if any; otherwise all related errors.</returns>
        private string GenerateToken(
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

            var generator = new TokenGenerator(Logger, signingCredentials, encryptingCredentials);
            return generator.Generate(entitlements);
        }

        private static Errorable<X509Certificate2> FindCertificate(string purpose, string thumbprint)
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
