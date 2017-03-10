using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    [Verb("find-certificate", HelpText = "List all available certificates.")]
    public class FindCertificateOptions : OptionsBase
    {
        [Option("thumbprint", HelpText = "Thumbprint of a certificate to display.")]
        public string Thumbprint { get; set; }
    }
}
