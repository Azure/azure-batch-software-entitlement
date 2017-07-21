using System;
using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Command line arguments for running as a standalone server
    /// </summary>
    [Verb("server", HelpText = "Run as a standalone software entitlement server.")]
    public sealed class ServerCommandLine : CommandLineBase
    {
        [Option("sign", HelpText = "Thumbprint of the certificate used to sign tokens (optional; if specified, all tokens must be signed).")]
        public string SigningCertificateThumbprint { get; set; }

        [Option("encrypt", HelpText = "Thumbprint of the certificate used to encrypt tokens (optional; if specified, all tokens must be encrypted).")]
        public string EncryptionCertificateThumbprint { get; set; }

        [Option("connection", HelpText = "Thumbprint of the certificate to pin for use with HTTPS (mandatory).")]
        public string ConnectionCertificateThumbprint { get; set; }

        [Option("url", HelpText = "The URL at which the server should process requests (defaults to 'https://localhost:4443'; must start with 'https:').")]
        public string ServerUrl { get; set; } = "https://localhost:4443";

        [Option("audience", HelpText = "Audience to which all tokens must be addressed (optional; defaults to 'https://batch.azure.test/software-entitlement').")]
        public string Audience { get; set; }
    }
}
