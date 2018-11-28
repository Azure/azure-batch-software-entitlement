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
        Errorable<DateTimeOffset> IssuedAt();

        /// <summary>
        /// Gets the earliest moment at which the token is active
        /// </summary>
        Errorable<DateTimeOffset> NotBefore();

        /// <summary>
        /// Gets the latest moment at which the token is active
        /// </summary>
        Errorable<DateTimeOffset> NotAfter();

        /// <summary>
        /// Gets the audience for whom the token is intended
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
        /// Gets the IP addresses of the machine authorized to use this token
        /// </summary>
        Errorable<IEnumerable<IPAddress>> IpAddresses();

        /// <summary>
        /// Gets the virtual machine identifier for the machine entitled to use the specified packages
        /// </summary>
        Errorable<string> VirtualMachineId();

        /// <summary>
        /// Gets the unique identifier for the token
        /// </summary>
        Errorable<string> TokenId();
    }
}
