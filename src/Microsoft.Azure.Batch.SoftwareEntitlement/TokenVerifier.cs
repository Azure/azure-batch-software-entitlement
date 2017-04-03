using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Verifies whether a specified software entitlement token is valid or not
    /// </summary>
    public class TokenVerifier
    {
        /// <summary>
        /// The virtual machine we're expecting to be specified in the token
        /// </summary>
        public string VirtualMachineId { get; }

        /// <summary>
        /// Gets the current instant against which the token will be verified
        /// </summary>
        public DateTimeOffset CurrentInstant { get; }

        /// <summary>
        /// Gets the key that should have been used to sign the token
        /// </summary>
        public SecurityKey SigningKey { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenVerifier"/> class
        /// </summary>
        public TokenVerifier(SecurityKey signingKey)
        {
            SigningKey = signingKey ?? throw new ArgumentNullException(nameof(signingKey));
            CurrentInstant = DateTimeOffset.Now;
        }

        /// <summary>
        /// Configure the token verifier to expect a particular virtual machine identifier within 
        /// the software entitlement token
        /// </summary>
        /// <param name="virtualMachineId">Required virtual machine id for the token to be valid.</param>
        /// <returns>An updated token verifier that requires the specified virtual machine id.</returns>
        public TokenVerifier WithVirtualMachineId(string virtualMachineId)
        {
            return new TokenVerifier(this, virtualMachineId: virtualMachineId);
        }

        /// <summary>
        /// Configure the token verifier to check the token as at a specified instant in time
        /// </summary>
        /// <param name="instant">Instant for which to check the token.</param>
        /// <returns>An updated token verifier that requires the specified instant.</returns>
        public TokenVerifier WithCurrentInstant(DateTimeOffset instant)
        {
            return new TokenVerifier(this, currentInstant: instant);
        }

        /// <summary>
        /// Verify the provided software entitlement token
        /// </summary>
        /// <param name="tokenString">A software entitlement token to verify.</param>
        /// <param name="application">The specific application id of the application </param>
        /// <param name="ipAddress"></param>
        /// <returns>Either a software entitlement describing the approved entitlement, or errors 
        /// explaining why it wasn't approved.</returns>
        public Errorable<NodeEntitlements> Verify(string tokenString, string application, IPAddress ipAddress)
        {
            var validationParameters = new TokenValidationParameters()
            {
                ValidateAudience = true,
                ValidAudience = Claims.Audience,
                ValidateIssuer = true,
                ValidIssuer = Claims.Issuer,
                ValidateLifetime = true,
                IssuerSigningKey = SigningKey,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.FromSeconds(60)
            };

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(tokenString, validationParameters, out var token);

                if (!VerifyApplication(principal, application))
                {
                    return ApplicationNotEntitledError(application);
                }

                if (!VerifyIpAddress(principal, ipAddress))
                {
                    return MachineNotEntitledError(ipAddress);
                }

                var entitlementIdClaim = principal.FindFirst(Claims.EntitlementId);
                if (entitlementIdClaim == null)
                {
                    return IdentifierNotPresentError();
                }

                var virtualMachineIdClaim = principal.FindFirst(Claims.VirtualMachineId);
                if (virtualMachineIdClaim == null)
                {
                    return VirtualMachineIdentifierNotPresentError();
                }

                var result = new NodeEntitlements()
                    .WithVirtualMachineId(virtualMachineIdClaim.Value)
                    .FromInstant(new DateTimeOffset(token.ValidFrom))
                    .UntilInstant(new DateTimeOffset(token.ValidTo))
                    .AddApplication(application)
                    .WithIdentifier(entitlementIdClaim.Value)
                    .WithIpAddress(ipAddress);

                return Errorable.Success(result);
            }
            catch (SecurityTokenNotYetValidException exception)
            {
                return TokenNotYetValidError(exception.NotBefore);
            }
            catch (SecurityTokenExpiredException exception)
            {
                return TokenExpiredError(exception.Expires);
            }
            catch (SecurityTokenException exception)
            {
                return InvalidTokenError(exception.Message);
            }
        }

        private TokenVerifier(
            TokenVerifier original,
            string virtualMachineId = null,
            DateTimeOffset? currentInstant = null,
            SecurityKey signingKey = null)
        {
            VirtualMachineId = virtualMachineId ?? original.VirtualMachineId;
            CurrentInstant = currentInstant ?? original.CurrentInstant;
            SigningKey = signingKey ?? original.SigningKey;
        }

        /// <summary>
        /// Verify the application requesting the entitlement is one specified
        /// </summary>
        /// <param name="principal">Principal from the decoded token.</param>
        /// <param name="application">Application that desires to use the entitlement.</param>
        /// <returns>True if the entitlement specifies the passed application, false otherwise.</returns>
        private bool VerifyApplication(ClaimsPrincipal principal, string application)
        {
            var applicationsClaim = principal.FindAll(Claims.Application);
            if (!applicationsClaim.Any(c => string.Equals(c.Value, application, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verify that the IP address requesting the entitlement is the one the entitlement was 
        /// issued to
        /// </summary>
        /// <param name="principal">Principal from the decoded token.</param>
        /// <param name="address">IpAddress requesting verification of the token.</param>
        /// <returns>True if the entitlement was issued to the specified IP address, false otherwise.</returns>
        private bool VerifyIpAddress(ClaimsPrincipal principal, IPAddress address)
        {
            var ipAddressClaim = principal.FindFirst(Claims.IpAddress);
            if (ipAddressClaim == null)
            {
                return false;
            }

            if (!IPAddress.TryParse(ipAddressClaim.Value, out var parsedAddress))
            {
                return false;
            }

            if (!address.Equals(parsedAddress))
            {
                return false;
            }

            return true;
        }

        private static Errorable<NodeEntitlements> TokenNotYetValidError(DateTime exceptionNotBefore)
        {
            var template = $"Token will not be valid until {{0:{TimestampParser.ExpectedFormat}}}";
            return Errorable.Failure<NodeEntitlements>(
                string.Format(template, exceptionNotBefore));
        }

        private static Errorable<NodeEntitlements> TokenExpiredError(DateTime expires)
        {
            var template = $"Token expired at {{0:{TimestampParser.ExpectedFormat}}}";
            return Errorable.Failure<NodeEntitlements>(
                string.Format(template, expires));
        }

        private static Errorable<NodeEntitlements> InvalidTokenError(string reason)
        {
            return Errorable.Failure<NodeEntitlements>(
                $"Invalid token ({reason})");
        }

        private Errorable<NodeEntitlements> ApplicationNotEntitledError(string application)
        {
            return Errorable.Failure<NodeEntitlements>(
                $"Token does not grant entitlement for {application}");
        }

        private Errorable<NodeEntitlements> MachineNotEntitledError(IPAddress address)
        {
            return Errorable.Failure<NodeEntitlements>(
                $"Token does not grant entitlement for {address}");
        }

        private Errorable<NodeEntitlements> IdentifierNotPresentError()
        {
            return Errorable.Failure<NodeEntitlements>(
                "Entitlement identifier missing from entitlement token.");
        }

        private Errorable<NodeEntitlements> VirtualMachineIdentifierNotPresentError()
        {
            return Errorable.Failure<NodeEntitlements>(
                "Virtual machine identifier missing from entitlement token.");
        }
    }
}
