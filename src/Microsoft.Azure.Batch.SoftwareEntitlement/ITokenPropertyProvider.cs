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
        Result<DateTimeOffset, ErrorCollection> IssuedAt();

        /// <summary>
        /// Gets the earliest moment at which the token is active
        /// </summary>
        Result<DateTimeOffset, ErrorCollection> NotBefore();

        /// <summary>
        /// Gets the latest moment at which the token is active
        /// </summary>
        Result<DateTimeOffset, ErrorCollection> NotAfter();

        /// <summary>
        /// Gets the audience for whom the token is intended
        /// </summary>
        Result<string, ErrorCollection> Audience();

        /// <summary>
        /// Gets the issuer who hands out entitlement tokens
        /// </summary>
        Result<string, ErrorCollection> Issuer();

        /// <summary>
        /// Gets the set of applications that are entitled to run
        /// </summary>
        Result<IEnumerable<string>, ErrorCollection> ApplicationIds();

        /// <summary>
        /// Gets the IP addresses of the machine authorized to use this token
        /// </summary>
        Result<IEnumerable<IPAddress>, ErrorCollection> IpAddresses();

        /// <summary>
        /// Gets the virtual machine identifier for the machine entitled to use the specified packages
        /// </summary>
        Result<string, ErrorCollection> VirtualMachineId();

        /// <summary>
        /// Gets the unique identifier for the token
        /// </summary>
        Result<string, ErrorCollection> TokenId();
    }
}
