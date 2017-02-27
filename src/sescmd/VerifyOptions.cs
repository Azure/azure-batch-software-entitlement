﻿using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    [Verb("verify", HelpText = "Verify a provided token by calling into the software entitlement service")]
    public class VerifyOptions
    {
        [Option("token", HelpText = "The token to verify")]
        public string Token { get; set; }

        [Option("token-file", HelpText = "A text file containing the token to verify")]
        public string TokenFile { get; set; }

        [Option("application-id", HelpText = "Unique identifier for the application")]
        public string ApplicationId { get; set; }

        [Option("vmid", HelpText = "Unique identifier for the Azure virtual machine")]
        public string VirtualMachineId { get; set; }

        [Option("batch-account-url", HelpText = "URL of the Azure Batch account server")]
        public string BatchAccountUrl{ get; set; }

        //TODO: This needs a much better name
        //TODO: Document where this looks to find the certificate (in a cross platform way)
        [Option('a', "authority", HelpText = "Certificate thumbprint used to sign the cert used for the HTTPS connection")]
        public string AuthorityThumbprint { get; set; }
    }
}
