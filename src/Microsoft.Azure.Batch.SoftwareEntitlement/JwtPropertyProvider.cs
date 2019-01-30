using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// An <see cref="ITokenPropertyProvider"/> implementation for extracting <see cref="EntitlementTokenProperties"/>
    /// information from a <see cref="JwtSecurityToken"/> and its corresponding <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public class JwtPropertyProvider : ITokenPropertyProvider
    {
        private readonly ClaimsPrincipal _principal;
        private readonly JwtSecurityToken _jwt;

        /// <summary>
        /// Creates a <see cref="JwtPropertyProvider"/> instance from a JWT object and claims principal.
        /// </summary>
        /// <param name="principal">A <see cref="ClaimsPrincipal"/> extracted from a JWT token.</param>
        /// <param name="jwt">A <see cref="JwtSecurityToken"/> object.</param>
        public JwtPropertyProvider(ClaimsPrincipal principal, JwtSecurityToken jwt)
        {
            _principal = principal ?? throw new ArgumentNullException(nameof(principal));
            _jwt = jwt ?? throw new ArgumentNullException(nameof(jwt));
        }

        /// <summary>
        /// Gets the moment at which the token is issued from the 'iat' claim on the JWT.
        /// </summary>
        public Result<DateTimeOffset, ErrorSet> IssuedAt()
        {
            var iat = _jwt.Payload.Iat;
            if (!iat.HasValue)
            {
                return ErrorSet.Create("Missing issued-at claim on token.");
            }

            return (DateTimeOffset)EpochTime.DateTime(iat.Value);
        }

        /// <summary>
        /// Gets the earliest moment at which the token is active from the 'nbf' claim on the JWT.
        /// </summary>
        public Result<DateTimeOffset, ErrorSet> NotBefore()
            => new DateTimeOffset(_jwt.ValidFrom);

        /// <summary>
        /// Gets the latest moment at which the token is active from the 'exp' claim on the JWT.
        /// </summary>
        public Result<DateTimeOffset, ErrorSet> NotAfter()
            => new DateTimeOffset(_jwt.ValidTo);

        /// <summary>
        /// Gets the audience for whom the token is intended from the 'aud' claim on the JWT, or
        /// an error if there are zero or many such claims.
        /// </summary>
        public Result<string, ErrorSet> Audience()
        {
            // We don't expect multiple audiences to appear in the token
            var audiences = _jwt.Audiences?.ToList();
            if (audiences == null || !audiences.Any())
            {
                return ErrorSet.Create("No audience claim found in token.");
            }

            if (audiences.Count > 1)
            {
                return ErrorSet.Create("Multiple audience claims found in token.");
            }

            return audiences.Single();
        }

        /// <summary>
        /// Gets the issuer who hands out token tokens from the 'iss' claim on the JWT.
        /// </summary>
        public Result<string, ErrorSet> Issuer()
            => _jwt.Issuer;

        /// <summary>
        /// Gets the set of applications that are entitled to run from the claims in the principal.
        /// </summary>
        public Result<IEnumerable<string>, ErrorSet> ApplicationIds()
        {
            var applicationIds = ReadAll(Claims.Application);
            if (!applicationIds.Any())
            {
                return ErrorSet.Create("No application id claims found in token.");
            }

            return applicationIds.AsOk();
        }

        /// <summary>
        /// Gets the IP addresses of the machine authorized to use this token from the claims in the principal.
        /// </summary>
        public Result<IEnumerable<IPAddress>, ErrorSet> IpAddresses()
            => ReadAll(Claims.IpAddress).Select(ParseIpAddress).Reduce();

        /// <summary>
        /// Gets the virtual machine identifier for the machine entitled to use the specified packages from a claim
        /// in the principal.
        /// </summary>
        public Result<string, ErrorSet> VirtualMachineId()
            => Read(Claims.VirtualMachineId);

        /// <summary>
        /// Gets the unique identifier for the token from a claim in the principal.
        /// </summary>
        public Result<string, ErrorSet> TokenId()
        {
            var entitlementId = Read(Claims.TokenId);
            if (string.IsNullOrEmpty(entitlementId))
            {
                return ErrorSet.Create("Missing token identifier in token.");
            }

            return entitlementId;
        }

        private static Result<IPAddress, ErrorSet> ParseIpAddress(string value)
        {
            if (!IPAddress.TryParse(value, out var parsedAddress))
            {
                return ErrorSet.Create($"Invalid IP claim: {value}");
            }

            return parsedAddress;
        }

        private string Read(string claimId)
            => _principal.FindFirst(claimId)?.Value;

        private IEnumerable<string> ReadAll(string claimId)
            => _principal.FindAll(claimId).Select(c => c.Value);
    }
}
