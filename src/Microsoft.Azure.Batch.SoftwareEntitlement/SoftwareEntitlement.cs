using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

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
        /// The moment at which the entitlement was created
        /// </summary>
        public DateTimeOffset Created { get; }

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
        public SoftwareEntitlement()
        {
            var now = DateTimeOffset.Now;

            VirtualMachineId = string.Empty;
            Created = now;
            NotBefore = now;
            NotAfter = now + TimeSpan.FromDays(7);
        }

        /// <summary>
        /// Specify the virtual machine Id of the machine 
        /// </summary>
        /// <param name="virtualMachineId"></param>
        /// <returns></returns>
        public SoftwareEntitlement WithVirtualMachineId(string virtualMachineId)
        {
            return new SoftwareEntitlement(this, virtualMachineId: virtualMachineId);
        }

        /// <summary>
        /// Specify an instant before which the token will not be valid
        /// </summary>
        /// <param name="notBefore">Earliest instant of availability.</param>
        /// <returns></returns>
        public SoftwareEntitlement FromInstant(DateTimeOffset notBefore)
        {
            return new SoftwareEntitlement(this, notBefore: notBefore);
        }

        /// <summary>
        /// Specify an instant before which the token will not be valid
        /// </summary>
        /// <param name="notAfter">Earliest instant of availability.</param>
        /// <returns></returns>
        public SoftwareEntitlement UntilInstant(DateTimeOffset notAfter)
        {
            return new SoftwareEntitlement(this, notAfter: notAfter);
        }

        /// <summary>
        /// Cloning constructor to initialize a new instance of the <see cref="SoftwareEntitlement"/> 
        /// class as a (near) copy of an existing one.
        /// </summary>
        /// <remarks>Specify any of the optional parameters to modify the clone from the original.</remarks>
        /// <param name="original">Original entitlement to clone.</param>
        /// <param name="notBefore">Optionally specify a new value for <see cref="NotBefore"/>.</param>
        /// <param name="notAfter">Optionally specify a new value for <see cref="NotAfter"/>.</param>
        /// <param name="virtualMachineId">Optionally specify a new value for <see cref="VirtualMachineId"/>.</param>
        public SoftwareEntitlement(
            SoftwareEntitlement original,
            DateTimeOffset? notBefore = null,
            DateTimeOffset? notAfter = null,
            string virtualMachineId = null)
        {
            Created = original.Created;

            NotBefore = notBefore ?? original.NotBefore;
            NotAfter = notAfter ?? original.NotAfter;
            VirtualMachineId = virtualMachineId ?? original.VirtualMachineId;
        }
    }
}
