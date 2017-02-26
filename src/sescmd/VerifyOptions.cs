using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace sescmd
{
    [Verb("verify", HelpText = "Verify a provided token by calling into the software entitlement service")]
    public class VerifyOptions
    {
        [Option("token", HelpText = "The token to verify")]
        public string Token { get; set; }

        [Option("application-id", HelpText = "Unique identifier for the application package")]
        public string ApplicationId { get; set; }

        [Option("vmid", HelpText = "Unique identifier for the azure virtual machine")]
        public string VirtualMachineId { get; set; }

        [Option("batch-account-url", HelpText = "URL of the batch account server")]
        public string BatchAccountServer { get; set; }

        [Option('a', "authority", HelpText = "Certificate thumbprint used to sign the cert used for the HTTPS connection")]
        public string AuthorityThumbprint { get; set; }
    }
}
