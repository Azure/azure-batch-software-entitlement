using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    [Verb("generate", HelpText = "Generate a token with specified parameters")]
    public class GenerateOptions
    {
        [Option("application-id",HelpText = "Unique identifier for the application")]
        public string ApplicationId { get; set; }

        [Option("vmid", HelpText = "Unique identifier for the azure virtual machine")]
        public string VirtualMachineId { get; set; }

        [Option("not-before", HelpText = "The moment at which the token becomes active and the application is entitled to execute.")]
        public DateTimeOffset NotBefore { get; set; }

        [Option("expires", HelpText = "The moment at which the token expires and the application is no longer entitled to execute.")]
        public DateTimeOffset Expires { get; set; }

        [Option("address", HelpText = "The externally visible IP address of the machine entitled to execute the application.")]
        public IPAddress Address { get; set; }

        [Option('s', "signature", HelpText = "Certificate thumbprint of the certificate used to sign the token")]
        public string SignatureThumbprint { get; set; }

        [Option('e', "encrypt", HelpText = "Certificate thumbprint of the certificate used to encrypt the token")]
        public string EncryptionThumbprint { get; set; }

    }
}
