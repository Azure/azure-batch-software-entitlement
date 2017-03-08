using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Generator to create a token given a fully configured software entitlement
    /// </summary>
    public class TokenGenerator
    {
        // Reference to our logger
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenGenerator"/> class
        /// </summary>
        /// <param name="logger">Logger to use for progress and status messages.</param>
        public TokenGenerator(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generate a token from a software entitlement
        /// </summary>
        /// <param name="entitlement">Software entitlement to use when populating the token.</param>
        /// <returns></returns>
        public string Generate(SoftwareEntitlement entitlement)
        {
            _logger.LogWarning("Incomplete implementation of Generate");

            var claims = new List<Claim>
            {
                new Claim("vid", entitlement.VirtualMachineId)
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
                NotBefore = entitlement.NotBefore.UtcDateTime,
                Expires = entitlement.NotAfter.UtcDateTime,
                IssuedAt = DateTimeOffset.Now.UtcDateTime,
                Issuer = "https://batch.azure.com/software-entitlement"
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(securityTokenDescriptor);
            return handler.WriteToken(token);
        }
    }
}
