using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Generator to create a token given a fully configured software entitlement
    /// </summary>
    public class TokenGenerator
    {
        // Reference to a logger for diagnostics
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the key that will be used to sign the token
        /// </summary>
        public SecurityKey SigningKey { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenGenerator"/> class
        /// </summary>
        /// <param name="signingKey">Key to use to sign the token.</param>
        /// <param name="logger">Logger to use for diagnostics</param>
        public TokenGenerator(SecurityKey signingKey, ILogger logger)
        {
            _logger = logger;
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

            _logger.LogDebug($"Virtual machine Id: {entitlements.VirtualMachineId}");
            var claims = new List<Claim>
            {
                new Claim(Claims.VirtualMachineId, entitlements.VirtualMachineId)
            };

            foreach (var app in entitlements.Applications)
            {
                _logger.LogDebug($"Application Id: {app}");
                var claim = new Claim(Claims.Application, app);
                claims.Add(claim);
            }

            _logger.LogDebug($"Not Before: {entitlements.NotBefore}");
            _logger.LogDebug($"Not After: {entitlements.NotAfter}");

            var signingCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256Signature);
            var claimsIdentity = new ClaimsIdentity(claims);
            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
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

            _logger.LogDebug($"Raw token: {token}");

            return handler.WriteToken(token);
        }
    }
}
