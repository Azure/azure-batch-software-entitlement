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
        // Reference to a logger for diagnostics
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the credentials that will be used to sign generated tokens
        /// </summary>
        public SigningCredentials SigningCredentials { get; }

        /// <summary>
        /// Gets the credentials that will be used to encrypt generated tokens
        /// </summary>
        public EncryptingCredentials EncryptingCredentials { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenGenerator"/> class
        /// </summary>
        /// <param name="logger">Logger to use for diagnostics</param>
        /// <param name="signingCredentials">Credentials to use for signing tokens (optional).</param>
        /// <param name="encryptingCredentials">Credentials to use for encryption of tokens (optional).</param>
        public TokenGenerator(
            ILogger logger,
            SigningCredentials signingCredentials = null,
            EncryptingCredentials encryptingCredentials = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            SigningCredentials = signingCredentials;
            EncryptingCredentials = encryptingCredentials;
        }

        /// <summary>
        /// Generate a token from a software entitlement
        /// </summary>
        /// <param name="entitlements">Software entitlements to use when populating the token.</param>
        /// <returns>The generated token.</returns>
        public string Generate(NodeEntitlements entitlements)
        {
            if (entitlements == null)
            {
                throw new ArgumentNullException(nameof(entitlements));
            }

            var claims = CreateClaims(entitlements);

            _logger.LogDebug(
                "Not Before: {notBefore}",
                entitlements.NotBefore.ToString(TimestampParser.ExpectedFormat));
            _logger.LogDebug(
                "Not After: {notAfter}",
                entitlements.NotAfter.ToString(TimestampParser.ExpectedFormat));

            var claimsIdentity = new ClaimsIdentity(claims);
            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                NotBefore = entitlements.NotBefore.UtcDateTime,
                Expires = entitlements.NotAfter.UtcDateTime,
                IssuedAt = DateTimeOffset.Now.UtcDateTime,
                Issuer = Claims.Issuer,
                Audience = Claims.Audience,
                SigningCredentials = SigningCredentials,
                EncryptingCredentials = EncryptingCredentials
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(securityTokenDescriptor);

            _logger.LogDebug("Raw token: {token}", token);

            return handler.WriteToken(token);
        }

        private List<Claim> CreateClaims(NodeEntitlements entitlements)
        {
            var claims = new List<Claim>();

            if (!string.IsNullOrEmpty(entitlements.VirtualMachineId))
            {
                _logger.LogDebug("Virtual machine Id: {vmid}", entitlements.VirtualMachineId);
                claims.Add(new Claim(Claims.VirtualMachineId, entitlements.VirtualMachineId));
            }

            if (!string.IsNullOrEmpty(entitlements.Identifier))
            {
                _logger.LogDebug("Entitlement Id: {identifier}", entitlements.Identifier);
                claims.Add(new Claim(Claims.EntitlementId, entitlements.Identifier));
            }

            foreach (var ip in entitlements.IpAddresses)
            {
                _logger.LogDebug("IP Address: {ip}", ip);
                claims.Add(new Claim(Claims.IpAddress, ip.ToString()));
            }

            foreach (var app in entitlements.Applications)
            {
                _logger.LogDebug("Application Id: {app}", app);
                var claim = new Claim(Claims.Application, app);
                claims.Add(claim);
            }

            return claims;
        }
    }
}
