using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    [Verb("verify", HelpText = "Verify a provided token by calling into the software entitlement service.")]
    public sealed class VerifyCommandLine : CommandLineBase
    {
        [Option("token", HelpText = "The text of the JWT token to verify.")]
        public string Token { get; set; }

        [Option("token-file", HelpText = "A text file containing the text of the JWT token to verify.")]
        public string TokenFile { get; set; }

        [Option("entitlement-id", HelpText = "Unique identifier for the entitlement to check.")]
        public string ApplicationId { get; set; }

        [Option("vmid", HelpText = "Unique identifier for the Azure virtual machine.")]
        public string VirtualMachineId { get; set; }

        [Option("batch-service-url", HelpText = "URL of the Azure Batch service endpoint for a particular account to contact for verification.")]
        public string BatchServiceUrl { get; set; }

        // TODO: This needs a much better name
        [Option('a', "authority", HelpText = "Certificate thumbprint used to sign the cert used for the HTTPS connection.")]
        public string AuthorityThumbprint { get; set; }
    }
}
