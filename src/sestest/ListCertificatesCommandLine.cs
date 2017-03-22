using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    [Verb("list-certificates", HelpText = "List all available certificates.")]
    public sealed class ListCertificatesCommandLine : CommandLineBase
    {
    }
}
