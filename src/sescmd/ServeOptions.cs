using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Options for running as a standalone server
    /// </summary>
    [Verb("serve", HelpText = "Run as a standalone software entitlement server.")]
    public class ServeOptions
    {
        [Option('a', "authority", HelpText = "Certificate thumbprint used to sign the cert used for the HTTPS connection")]
        public string AuthorityThumbprint { get; set; }

        [Option('s', "signature", HelpText = "Certificate thumbprint of the certificate used to sign the token")]
        public string SignatureThumbprint { get; set; }

        //TODO: Document where this looks to find the certificate (in a cross platform way)
        [Option('e', "encrypt", HelpText = "Certificate thumbprint of the certificate used to encrypt the token")]
        public string EncryptionThumbprint { get; set; }

        [Option('x', "exit-after-request", HelpText = "Request the server exits after serving one request")]
        public bool ExitAfterRequest { get; set; }
    }
}
