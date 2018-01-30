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

            _logger.LogDebug(
                "Not Before: {NotBefore}",
                entitlements.NotBefore.ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture));
            _logger.LogDebug(
                "Not After: {NotAfter}",
                entitlements.NotAfter.ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture));

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

            _logger.LogDebug("Issued by {Issuer}", entitlements.Issuer);
            _logger.LogDebug("Audience is {Audience}", entitlements.Audience);

            var claims = CreateClaims(entitlements);
            var claimsIdentity = new ClaimsIdentity(claims);
            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                NotBefore = entitlements.NotBefore.UtcDateTime,
                Expires = entitlements.NotAfter.UtcDateTime,
                IssuedAt = entitlements.IssuedAt.UtcDateTime,
                Issuer = entitlements.Issuer,
                Audience = entitlements.Audience,
                SigningCredentials = SigningCredentials,
                EncryptingCredentials = EncryptingCredentials
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(securityTokenDescriptor);

            _logger.LogDebug("Raw token: {Token}", token);

            return handler.WriteToken(token);
        }

        private List<Claim> CreateClaims(NodeEntitlements entitlements)
        {
            var claims = new List<Claim>();

            AddClaim(claims, Claims.VirtualMachineId, "Virtual machine id", entitlements.VirtualMachineId);
            AddClaim(claims, Claims.CpuCoreCount, "CPU core count", entitlements.CpuCoreCount?.ToString(CultureInfo.InvariantCulture));
            AddClaim(claims, Claims.BatchAccountId, "Batch account id", entitlements.BatchAccountId);
            AddClaim(claims, Claims.PoolId, "Pool id", entitlements.PoolId);
            AddClaim(claims, Claims.JobId, "Job id", entitlements.JobId);
            AddClaim(claims, Claims.TaskId, "Task id", entitlements.TaskId);
            AddClaim(claims, Claims.EntitlementId, "Entitlement id", entitlements.Identifier);

            foreach (var ip in entitlements.IpAddresses)
            {
                AddClaim(claims, Claims.IpAddress, "IP address", ip.ToString());
            }

            foreach (var app in entitlements.Applications)
            {
                AddClaim(claims, Claims.Application, "Application id", app);
            }

            return claims;
        }

        private void AddClaim(IList<Claim> claims, string claimId, string claimName, string claimValue)
        {
            if (!string.IsNullOrEmpty(claimValue))
            {
                _logger.LogDebug($"{claimName}: {{{claimId}}}", claimValue);
                claims.Add(new Claim(claimId, claimValue));
            }
        }
    }
}
