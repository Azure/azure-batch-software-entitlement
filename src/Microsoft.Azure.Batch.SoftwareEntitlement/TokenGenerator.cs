using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Generator to create a token given a fully configured software entitlement
    /// </summary>
    public class TokenGenerator
    {
        /// <summary>
        /// Gets the key that will be used to sign the token
        /// </summary>
        public SecurityKey SigningKey { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenGenerator"/> class
        /// </summary>
        /// <param name="signingKey">Key to use to sign the token.</param>
        public TokenGenerator(SecurityKey signingKey)
        {
            SigningKey = signingKey ?? throw new ArgumentNullException(nameof(signingKey));
        }

        /// <summary>
        /// Generate a token from a software entitlement
        /// </summary>
        /// <param name="entitlements">Software entitlement to use when populating the token.</param>
        /// <returns></returns>
        public string Generate(NodeEntitlements entitlements)
        {
            if (entitlements == null)
            {
                throw new ArgumentNullException(nameof(entitlements));
            }

            var claims = new List<Claim>
            {
                new Claim(Claims.VirtualMachineId, entitlements.VirtualMachineId)
            };

            var signingCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256Signature);
            var claimsIdentity = new ClaimsIdentity(claims);
            var securityTokenDescriptor = new SecurityTokenDescriptor()
            {
                //AppliesToAddress = "http://my.website.com",
                //TokenIssuerName = "http://my.tokenissuer.com",
                Subject = claimsIdentity,
                NotBefore = entitlements.NotBefore.UtcDateTime,
                Expires = entitlements.NotAfter.UtcDateTime,
                IssuedAt = DateTimeOffset.Now.UtcDateTime,
                Issuer = Claims.Issuer,
                Audience = Claims.Audience,
                SigningCredentials = signingCredentials
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(securityTokenDescriptor);
            return handler.WriteToken(token);
        }
    }
}
