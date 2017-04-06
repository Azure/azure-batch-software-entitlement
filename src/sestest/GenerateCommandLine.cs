using System;
using System.Collections.Generic;
using System.Net;
using CommandLine;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    [Verb("generate", HelpText = "Generate a token with specified parameters")]
    public sealed class GenerateCommandLine : CommandLineBase
    {
        [Option("application-id", HelpText = "Unique identifier(s) for the applications(s) to include in the entitlement (comma separated).", Separator = ',')]
        public IEnumerable<string> ApplicationIds { get; set; }

        [Option("vmid", HelpText = "Unique identifier for the Azure virtual machine (mandatory).")]
        public string VirtualMachineId { get; set; }

        [Option("not-before",
            HelpText = "The moment at which the token becomes active and the application is entitled to execute (format 'hh:mm d-mmm-yyyy'; 24 hour clock; local time; defaults to now).")]
        public string NotBefore { get; set; }

        [Option("not-after", HelpText = "The moment at which the token expires and the application is no longer entitled to execute (format 'hh:mm d-mmm/-yyyy'; 24 hour clock; local time; defaults to 7 days).")]
        public string NotAfter { get; set; }

        [Option("address", HelpText = "The externally visible IP address of the machine entitled to execute the application.")]
        public string Address { get; set; }

        //TODO: Document where this looks to find the certificate (in a cross platform way)
        [Option('s', "sign", HelpText = "Certificate thumbprint of the certificate used to sign the token.")]
        public string SignatureThumbprint { get; set; }

        //TODO: Document where this looks to find the certificate (in a cross platform way)
        [Option("encrypt", HelpText = "Certificate thumbprint of the certificate used to encrypt the token.")]
        public string EncryptionThumbprint { get; set; }

        [Option('f', "token-file", HelpText = "The name of a file into which the token will be written (token will be written to stdout otherwise).")]
        public string TokenFile { get; set; }
    }
}
