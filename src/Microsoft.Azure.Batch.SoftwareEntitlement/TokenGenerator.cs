using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Generator to create a token given a fully configured software entitlement
    /// </summary>
    public class TokenGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TokenGenerator"/> class
        /// </summary>
        public TokenGenerator()
        {
        }

        /// <summary>
        /// Generate a token from a software entitlement
        /// </summary>
        /// <param name="entitlements">Software entitlement to use when populating the token.</param>
        /// <returns></returns>
        public string Generate(NodeEntitlements entitlements)
        {
            var claims = new List<Claim>
            {
                new Claim("vid", entitlements.VirtualMachineId)
            };

            var claimsIdentity = new ClaimsIdentity(claims);
            var securityTokenDescriptor = new SecurityTokenDescriptor()
            {
                //Audience = 
                //AppliesToAddress = "http://my.website.com",
                //TokenIssuerName = "http://my.tokenissuer.com",
                //Subject = claimsIdentity
                //SigningCredentials = signingCredentials,
                Subject = claimsIdentity,
                NotBefore = entitlements.NotBefore.UtcDateTime,
                Expires = entitlements.NotAfter.UtcDateTime,
                IssuedAt = DateTimeOffset.Now.UtcDateTime,
                Issuer = "https://batch.azure.com/software-entitlement"
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(securityTokenDescriptor);
            return handler.WriteToken(token);
        }
    }
}
