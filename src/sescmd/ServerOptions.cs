using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Options for running as a standalone server
    /// </summary>
    [Verb("server", HelpText = "Run as a standalone software entitlement server.")]
    public class ServerOptions : OptionsBase
    {
        [Option("signing-cert", HelpText = "Certificate thumbprint of the certificate used to sign the token (optional).")]
        public string SigningCertificateThumbprint { get; set; }

        //TODO: Document where this looks to find the certificate (in a cross platform way)
        [Option("encryption-cert", HelpText = "Certificate thumbprint of the certificate used to encrypt the token (optional).")]
        public string EncryptionCertificateThumbprint { get; set; }

        [Option("exit-after-request", HelpText = "Request the server exits after serving one request (optional).")]
        public bool ExitAfterRequest { get; set; }

        [Option("connection-cert", HelpText = "Thumbprint of the certificate to pin for use with HTTPS (mandatory).")]
        public string ConnectionCertificateThumbprint { get; set; }

        [Option("url", HelpText = "The url at which the server should process requests (defaults to https://localhost:4443; must start with 'https:').")]
        public string ServerUrl { get; set; } = "https://localhost:4443";


    }
}
