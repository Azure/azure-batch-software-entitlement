using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Shared code between different commands
    /// </summary>
    public abstract class CommandBase
    {
        // Reference to our logger
        protected readonly ILogger Logger;

        protected CommandBase(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
    }
}
