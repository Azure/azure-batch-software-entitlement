using System.Collections.Generic;
using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    [Verb("list-certificates", HelpText = "List all available certificates.")]
    public sealed class ListCertificatesCommandLine : CommandLineBase
    {
        [Option(HelpText = "Which certificates should be shown? (one of `nonexpired` (default), 'forsigning', 'forencrypting', 'expired', 'forserverauth', and 'all').")]
        public string Show { get; set; }

        [Option("extra-columns", HelpText = "Which extra columns should be shown? (comma separated, one or more of 'subjectname', and 'friendlyname')", Separator = ',')]
        public IList<string> ExtraColumns { get; set; } = new List<string>();
    }
}
