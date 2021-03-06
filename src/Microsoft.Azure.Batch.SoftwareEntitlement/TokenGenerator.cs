using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Generator to create a token given a fully configured <see cref="EntitlementTokenProperties"/>
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
        /// Generate a token from a <see cref="EntitlementTokenProperties"/>
        /// </summary>
        /// <param name="tokenProperties">Software entitlements to use when populating the token.</param>
        /// <returns>The generated token.</returns>
        public string Generate(EntitlementTokenProperties tokenProperties)
        {
            if (tokenProperties == null)
            {
                throw new ArgumentNullException(nameof(tokenProperties));
            }

            _logger.LogDebug(
                "Not Before: {NotBefore}",
                tokenProperties.NotBefore.ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture));
            _logger.LogDebug(
                "Not After: {NotAfter}",
                tokenProperties.NotAfter.ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture));

            if (SigningCredentials != null)
            {
                _logger.LogDebug(
                    "Signing with {Credentials}",
                    SigningCredentials.Kid);
            }

            if (EncryptingCredentials != null)
            {
                _logger.LogDebug(
                    "Encrypting with {Algorithm}/{Credentials}",
                    EncryptingCredentials.Alg,
                    EncryptingCredentials.Key.KeyId);
            }

            _logger.LogDebug("Issued by {Issuer}", tokenProperties.Issuer);
            _logger.LogDebug("Audience is {Audience}", tokenProperties.Audience);

            var claims = CreateClaims(tokenProperties);
            var claimsIdentity = new ClaimsIdentity(claims);
            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                NotBefore = tokenProperties.NotBefore.UtcDateTime,
                Expires = tokenProperties.NotAfter.UtcDateTime,
                IssuedAt = tokenProperties.IssuedAt.UtcDateTime,
                Issuer = tokenProperties.Issuer,
                Audience = tokenProperties.Audience,
                SigningCredentials = SigningCredentials,
                EncryptingCredentials = EncryptingCredentials
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(securityTokenDescriptor);

            _logger.LogDebug("Raw token: {Token}", token);

            return handler.WriteToken(token);
        }

        private List<Claim> CreateClaims(EntitlementTokenProperties tokenProperties)
        {
            var claims = new List<Claim>();

            if (!string.IsNullOrEmpty(tokenProperties.VirtualMachineId))
            {
                _logger.LogDebug("Virtual machine Id: {VirtualMachineId}", tokenProperties.VirtualMachineId);
                claims.Add(new Claim(Claims.VirtualMachineId, tokenProperties.VirtualMachineId));
            }

            if (!string.IsNullOrEmpty(tokenProperties.Identifier))
            {
                _logger.LogDebug("Token Id: {TokenId}", tokenProperties.Identifier);
                claims.Add(new Claim(Claims.TokenId, tokenProperties.Identifier));
            }

            foreach (var ip in tokenProperties.IpAddresses)
            {
                _logger.LogDebug("IP Address: {IP}", ip);
                claims.Add(new Claim(Claims.IpAddress, ip.ToString()));
            }

            foreach (var app in tokenProperties.Applications)
            {
                _logger.LogDebug("Application Id: {ApplicationId}", app);
                var claim = new Claim(Claims.Application, app);
                claims.Add(claim);
            }

            return claims;
        }
    }
}
