using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    [Verb("list-certificates", HelpText = "List all available certificates.")]
    public sealed class ListCertificatesCommandLine : CommandLineBase
    {
        [Option(HelpText = "Show expired certificates in list")]
        public bool ShowExpired { get; set; }
    }
}
