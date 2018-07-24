using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    [Verb("list-certificates", HelpText = "List all available certificates.")]
    public sealed class ListCertificatesCommandLine : CommandLineBase
    {
        [Option(HelpText = "Which certificates should be shown? (one of `nonexpired` (default), 'forsigning', 'forencrypting', 'expired', and 'all').")]
        public string Show { get; set; }
    }
}
