using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Supplies all the property values required to build a <see cref="NodeEntitlements"/> object.
    /// Responsible for performing validation and returning error results for properties which cannot
    /// be populated.
    /// </summary>
    public interface IEntitlementPropertyProvider
    {
        /// <summary>
        /// Gets the moment at which the entitlement is issued
        /// </summary>
        Errorable<DateTimeOffset> IssuedAt();

        /// <summary>
        /// Gets the earliest moment at which the entitlement is active
        /// </summary>
        Errorable<DateTimeOffset> NotBefore();

        /// <summary>
        /// Gets the latest moment at which the entitlement is active
        /// </summary>
        Errorable<DateTimeOffset> NotAfter();

        /// <summary>
        /// Gets the audience for whom the entitlement is intended
        /// </summary>
        Errorable<string> Audience();

        /// <summary>
        /// Gets the issuer who hands out entitlement tokens
        /// </summary>
        Errorable<string> Issuer();

        /// <summary>
        /// Gets the set of applications that are entitled to run
        /// </summary>
        Errorable<IEnumerable<string>> ApplicationIds();

        /// <summary>
        /// Gets the IP addresses of the machine authorized to use this entitlement
        /// </summary>
        Errorable<IEnumerable<IPAddress>> IpAddresses();

        /// <summary>
        /// Gets the number of CPU cores configured for the selected VM SKU
        /// </summary>
        Errorable<int> CpuCoreCount();

        /// <summary>
        /// The unique identifier of the batch account that owns the pool
        /// </summary>
        Errorable<string> BatchAccountId();

        /// <summary>
        /// The unique identifier for the pool on which the application is expected to be running
        /// </summary>
        Errorable<string> PoolId();

        /// <summary>
        /// A unique identifier for the job within which the task is running
        /// </summary>
        Errorable<string> JobId();

        /// <summary>
        /// A unique identifier for the task itself
        /// </summary>
        Errorable<string> TaskId();

        /// <summary>
        /// Gets the virtual machine identifier for the machine entitled to use the specified packages
        /// </summary>
        Errorable<string> VirtualMachineId();

        /// <summary>
        /// Gets the unique identifier for the entitlement
        /// </summary>
        Errorable<string> EntitlementId();
    }
}
