using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Definition of a software entitlement
    /// </summary>
    public class SoftwareEntitlement
    {
        /// <summary>
        /// Gets the virtual machine identifier for the machine entitled to use the specified packages
        /// </summary>
        public string VirtualMachineId { get; }

        /// <summary>
        /// The earliest moment at which the entitlement is active
        /// </summary>
        public DateTimeOffset NotBefore { get; }

        /// <summary>
        /// The latest moment at which the entitlement is active
        /// </summary>
        public DateTimeOffset NotAfter { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareEntitlement"/> class
        /// </summary>
        /// <param name="virtualMachineId">Unique identifier of the virtual machine.</param>
        /// <param name="notBefore">Earliest instant the entitlement is valid.</param>
        /// <param name="notAfter">Latest instance the entitlement is valid.</param>
        public SoftwareEntitlement(string virtualMachineId, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            VirtualMachineId = virtualMachineId;
            NotBefore = notBefore;
            NotAfter = notAfter;
        }
    }
}
