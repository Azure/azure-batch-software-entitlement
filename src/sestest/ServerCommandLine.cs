using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Command line arguments for running as a standalone server
    /// </summary>
    [Verb("server", HelpText = "Run as a standalone software entitlement server.")]
    public sealed class ServerCommandLine : CommandLineBase
    {
        public const string DefaultServerUrl = "https://localhost:4443";

        [Option("sign", HelpText = "Thumbprint of the certificate used to sign tokens (optional; if specified, all tokens must be signed).")]
        public string SigningCertificateThumbprint { get; set; }

        [Option("encrypt", HelpText = "Thumbprint of the certificate used to encrypt tokens (optional; if specified, all tokens must be encrypted).")]
        public string EncryptionCertificateThumbprint { get; set; }

        [Option("connection", HelpText = "Thumbprint of the certificate to pin for use with HTTPS (mandatory).")]
        public string ConnectionCertificateThumbprint { get; set; }

        [Option("url", HelpText = "The URL at which the server should process requests (defaults to '" + DefaultServerUrl + "'; must start with 'https:').")]
        public string ServerUrl { get; set; }

        [Option("audience", HelpText = "[Internal] Audience to which all tokens must be addressed (optional).")]
        public string Audience { get; set; }

        [Option("issuer", HelpText = "[Internal] Issuer by which all tokens must have been created (optional).")]
        public string Issuer { get; set; }

        [Option("exit-after-request", HelpText = "Server will automatically exit after processing one request.")]
        public bool ExitAfterRequest { get; set; }
    }
}
