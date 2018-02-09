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
    /// An <see cref="IEntitlementPropertyProvider"/> implementation for extracting <see cref="NodeEntitlements"/>
    /// information from a <see cref="JwtSecurityToken"/> and its corresponding <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public class JwtEntitlementPropertyProvider : IEntitlementPropertyProvider
    {
        private readonly ClaimsPrincipal _principal;
        private readonly JwtSecurityToken _jwt;

        /// <summary>
        /// Creates a <see cref="JwtEntitlementPropertyProvider"/> instance from a JWT object and claims principal.
        /// </summary>
        /// <param name="principal">A <see cref="ClaimsPrincipal"/> extracted from a JWT token.</param>
        /// <param name="jwt">A <see cref="JwtSecurityToken"/> object.</param>
        public JwtEntitlementPropertyProvider(ClaimsPrincipal principal, JwtSecurityToken jwt)
        {
            _principal = principal ?? throw new ArgumentNullException(nameof(principal));
            _jwt = jwt ?? throw new ArgumentNullException(nameof(jwt));
        }

        /// <summary>
        /// Gets the moment at which the entitlement is issued from the 'iat' claim on the JWT.
        /// </summary>
        public Errorable<DateTimeOffset> IssuedAt()
        {
            var iat = _jwt.Payload.Iat;
            if (!iat.HasValue)
            {
                return Errorable.Failure<DateTimeOffset>("Missing issued-at claim on token.");
            }

            return Errorable.Success<DateTimeOffset>(EpochTime.DateTime(iat.Value));
        }

        /// <summary>
        /// Gets the earliest moment at which the entitlement is active from the 'nbf' claim on the JWT.
        /// </summary>
        public Errorable<DateTimeOffset> NotBefore()
            => Errorable.Success(new DateTimeOffset(_jwt.ValidFrom));

        /// <summary>
        /// Gets the latest moment at which the entitlement is active from the 'exp' claim on the JWT.
        /// </summary>
        public Errorable<DateTimeOffset> NotAfter()
            => Errorable.Success(new DateTimeOffset(_jwt.ValidTo));

        /// <summary>
        /// Gets the audience for whom the entitlement is intended from the 'aud' claim on the JWT, or
        /// an error if there are zero or many such claims.
        /// </summary>
        public Errorable<string> Audience()
        {
            // We don't expect multiple audiences to appear in the token
            var audiences = _jwt.Audiences.ToList();
            if (audiences == null || !audiences.Any())
            {
                return Errorable.Failure<string>("No audience claim found in token.");
            }

            if (audiences.Count > 1)
            {
                return Errorable.Failure<string>("Multiple audience claims found in token.");
            }

            return Errorable.Success(audiences.Single());
        }

        /// <summary>
        /// Gets the issuer who hands out entitlement tokens from the 'iss' claim on the JWT.
        /// </summary>
        public Errorable<string> Issuer()
            => Errorable.Success(_jwt.Issuer);

        /// <summary>
        /// Gets the set of applications that are entitled to run from the claims in the principal.
        /// </summary>
        public Errorable<IEnumerable<string>> ApplicationIds()
        {
            var applicationIds = ReadAll(Claims.Application);
            if (!applicationIds.Any())
            {
                return Errorable.Failure<IEnumerable<string>>("No application id claims found in token.");
            }

            return Errorable.Success(applicationIds);
        }

        /// <summary>
        /// Gets the IP addresses of the machine authorized to use this entitlement from the claims in the principal.
        /// </summary>
        public Errorable<IEnumerable<IPAddress>> IpAddresses()
            => ReadAll(Claims.IpAddress).Select(ParseIpAddress).Reduce();

        /// <summary>
        /// Gets the virtual machine identifier for the machine entitled to use the specified packages from a claim
        /// in the principal.
        /// </summary>
        public Errorable<string> VirtualMachineId()
            => Errorable.Success(Read(Claims.VirtualMachineId));

        /// <summary>
        /// Gets the unique identifier for the entitlement from a claim in the principal.
        /// </summary>
        public Errorable<string> EntitlementId()
        {
            var entitlementId = Read(Claims.EntitlementId);
            if (string.IsNullOrEmpty(entitlementId))
            {
                return Errorable.Failure<string>("Missing entitlement identifier in token.");
            }

            return Errorable.Success(entitlementId);
        }

        private static Errorable<IPAddress> ParseIpAddress(string value)
        {
            return IPAddress.TryParse(value, out var parsedAddress)
                ? Errorable.Success(parsedAddress)
                : Errorable.Failure<IPAddress>($"Invalid IP claim: {value}");
        }

        private string Read(string claimId)
            => _principal.FindFirst(claimId)?.Value;

        private IEnumerable<string> ReadAll(string claimId)
            => _principal.FindAll(claimId).Select(c => c.Value);
    }
}
