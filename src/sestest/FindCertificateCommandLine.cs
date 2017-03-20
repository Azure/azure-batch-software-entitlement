using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    [Verb("find-certificate", HelpText = "Show the details of one particular certificate.")]
    public sealed class FindCertificateCommandLine : CommandLineBase
    {
        [Option("thumbprint", HelpText = "Thumbprint of a certificate to display (mandatory).")]
        public string Thumbprint { get; set; }
    }
}
