using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public class OptionsBase
    {
        [Option("quiet", HelpText = "Suppress most logging output (errors and warnings will still show).")]
        public bool Quiet { get; set; }

        [Option("verbose", HelpText = "Show verbose logging.")]
        public bool Verbose { get; set; }

        [Option("debug", HelpText = "Show debug logging as well (maximum information).")]
        public bool Debug { get; set; }
    }
}
