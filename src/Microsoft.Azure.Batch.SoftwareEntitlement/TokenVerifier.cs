using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
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
        /// <returns>Either a software entitlement describing the approved entitlement, or errors 
        /// explaining why it wasn't approved.</returns>
        public Errorable<NodeEntitlements> Verify(string tokenString, string application)
        {
            var validationParameters = new TokenValidationParameters()
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = true,
                IssuerSigningKey = SigningKey
            };

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(tokenString, validationParameters, out var token);

                var applicationsClaim = principal.FindAll(Claims.Application);                
                if (!applicationsClaim.Any(c => string.Equals(c.Value, application)))
                {
                    return Errorable.Failure<NodeEntitlements>(
                        CreateApplicationNotEntitledMessage(application));
                }

                var virtualMachineIdClaim = principal.FindFirst(Claims.VirtualMachineId);
                var result = new NodeEntitlements()
                    .WithVirtualMachineId(virtualMachineIdClaim.Value)
                    .FromInstant(new DateTimeOffset(token.ValidFrom))
                    .UntilInstant(new DateTimeOffset(token.ValidTo))
                    .AddApplication(application);

                return Errorable.Success(result);
            }
            catch (SecurityTokenNotYetValidException exception)
            {
                return Errorable.Failure<NodeEntitlements>(
                    CreateTokenNotYetValidMessage(exception.NotBefore));
            }
            catch (SecurityTokenExpiredException exception)
            {
                return Errorable.Failure<NodeEntitlements>(
                    CreateTokenExpiredMessage(exception.Expires));
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

        private static string CreateTokenNotYetValidMessage(DateTime exceptionNotBefore)
        {
            var template = $"Token will not be valid until {{0:{TimestampParser.ExpectedFormat}}}";
            return string.Format(template, exceptionNotBefore);
        }

        private static string CreateTokenExpiredMessage(DateTime expires)
        {
            var template = $"Token expired at {{0:{TimestampParser.ExpectedFormat}}}";
            return string.Format(template, expires);
        }

        private string CreateApplicationNotEntitledMessage(string application)
        {
            return $"Token does not grant entitlement for {application}";
        }


    }
}
