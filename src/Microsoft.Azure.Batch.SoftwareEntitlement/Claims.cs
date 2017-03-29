using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Values used to define the claims used in our software entitlement token
    /// </summary>
    public class Claims
    {
        /// <summary>
        /// The audience for each software entitlement token (essentially we self sign)
        /// </summary>
        public const string Audience = "https://batch.azure.com/software-entitlement";

        /// <summary>
        /// The issuer of each software entitlement token
        /// </summary>
        public const string Issuer = "https://batch.azure.com/software-entitlement";

        /// <summary>
        /// Identifier use for the claim specifying the permitted virtual machine
        /// </summary>
        public const string VirtualMachineId = "vmid";


    }
}
