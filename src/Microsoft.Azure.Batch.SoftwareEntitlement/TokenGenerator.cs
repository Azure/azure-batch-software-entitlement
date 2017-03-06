using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public class TokenGenerator
    {
        // Reference to our logger
        private readonly ISimpleLogger _logger;

        // list of claims we'll include in the token
        private readonly List<Claim> _claims = new List<Claim>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenGenerator"/> class
        /// </summary>
        /// <param name="logger">Logger to use for progress and status messages.</param>
        public TokenGenerator(ISimpleLogger logger)
        {
            _logger = logger;
        }

        public SecurityToken Generate(SoftwareEntitlement entitlement)
        {
            _logger.Warning("Incomplete implementation of Generate");

            var claimsIdentity = new ClaimsIdentity(_claims);
            claimsIdentity.AddClaim(new Claim("vid", entitlement.VirtualMachineId));

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
            return token;
        }

    }
}
