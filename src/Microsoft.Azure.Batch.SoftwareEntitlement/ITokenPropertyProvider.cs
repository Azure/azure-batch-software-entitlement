using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Supplies all the property values required to build a <see cref="EntitlementTokenProperties"/> object.
    /// Responsible for performing validation and returning error results for properties which cannot
    /// be populated.
    /// </summary>
    public interface ITokenPropertyProvider
    {
        /// <summary>
        /// Gets the moment at which the token is issued
        /// </summary>
        Result<DateTimeOffset, ErrorSet> IssuedAt();

        /// <summary>
        /// Gets the earliest moment at which the token is active
        /// </summary>
        Result<DateTimeOffset, ErrorSet> NotBefore();

        /// <summary>
        /// Gets the latest moment at which the token is active
        /// </summary>
        Result<DateTimeOffset, ErrorSet> NotAfter();

        /// <summary>
        /// Gets the audience for whom the token is intended
        /// </summary>
        Result<string, ErrorSet> Audience();

        /// <summary>
        /// Gets the issuer who hands out entitlement tokens
        /// </summary>
        Result<string, ErrorSet> Issuer();

        /// <summary>
        /// Gets the set of applications that are entitled to run
        /// </summary>
        Result<IEnumerable<string>, ErrorSet> ApplicationIds();

        /// <summary>
        /// Gets the IP addresses of the machine authorized to use this token
        /// </summary>
        Result<IEnumerable<IPAddress>, ErrorSet> IpAddresses();

        /// <summary>
        /// Gets the virtual machine identifier for the machine entitled to use the specified packages
        /// </summary>
        Result<string, ErrorSet> VirtualMachineId();

        /// <summary>
        /// Gets the unique identifier for the token
        /// </summary>
        Result<string, ErrorSet> TokenId();
    }
}
