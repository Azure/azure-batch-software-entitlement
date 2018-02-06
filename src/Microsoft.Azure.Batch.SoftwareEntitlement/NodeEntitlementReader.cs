using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public class NodeEntitlementReader
    {
        private readonly string _expectedAudience;
        private readonly string _expectedIssuer;
        private readonly SecurityKey _signingKey;
        private readonly SecurityKey _encryptionKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenReader"/> class
        /// </summary>
        /// <param name="expectedAudience">The audience claim that tokens to be read are expected to have.</param>
        /// <param name="expectedIssuer">The issuer claim that tokens to be read are expected to have.</param>
        /// <param name="signingKey">Optional key to use when verifying the signature of the token.</param>
        /// <param name="encryptingKey">Optional key to use when decrypting the token.</param>
        public NodeEntitlementReader(
            string expectedAudience,
            string expectedIssuer,
            SecurityKey signingKey = null,
            SecurityKey encryptingKey = null)
        {
            _expectedAudience = expectedAudience;
            _expectedIssuer = expectedIssuer;
            _signingKey = signingKey;
            _encryptionKey = encryptingKey;
        }

        public Errorable<NodeEntitlements> ReadFromToken(string tokenString)
        {
            return ParseToken(tokenString)
                .Bind(parsed => ReadClaims(parsed.Principal, parsed.Token));
        }

        private Errorable<(ClaimsPrincipal Principal, SecurityToken Token)> ParseToken(string tokenString)
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = _expectedAudience,
                ValidateIssuer = true,
                ValidIssuer = _expectedIssuer,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = _signingKey != null,
                ClockSkew = TimeSpan.FromSeconds(60),
                IssuerSigningKey = _signingKey,
                ValidateIssuerSigningKey = true,
                TokenDecryptionKey = _encryptionKey
            };

            Errorable<(ClaimsPrincipal Principal, SecurityToken Token)> Failure(string error)
                => Errorable.Failure<(ClaimsPrincipal Principal, SecurityToken Token)>(error);

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(tokenString, validationParameters, out var token);

                return Errorable.Success((Principal: principal, Token: token));
            }
            catch (SecurityTokenNotYetValidException exception)
            {
                return Failure(TokenNotYetValidError(exception.NotBefore));
            }
            catch (SecurityTokenExpiredException exception)
            {
                return Failure(TokenExpiredError(exception.Expires));
            }
            catch (SecurityTokenException exception)
            {
                return Failure(InvalidTokenError(exception.Message));
            }
        }

        private static Errorable<NodeEntitlements> ReadClaims(ClaimsPrincipal principal, SecurityToken token)
        {
            var reader = new ClaimsReader(principal, token);

            return Errorable.Success(new NodeEntitlements())
                .With(reader.NotBefore()).Map((e, val) => e.FromInstant(val))
                .With(reader.NotAfter()).Map((e, val) => e.UntilInstant(val))
                .With(reader.IssuedAt()).Map((e, val) => e.WithIssuedAt(val))
                .With(reader.Issuer()).Map((e, val) => e.WithIssuer(val))
                .With(reader.Audience()).Map((e, val) => e.WithAudience(val))
                .With(reader.ApplicationIds()).Map((e, vals) => e.WithApplications(vals))
                .With(reader.IpAddresses()).Map((e, vals) => e.WithIpAddresses(vals))
                .With(reader.VirtualMachineId()).Map((e, val) => e.WithVirtualMachineId(val))
                .With(reader.EntitlementId()).Map((e, val) => e.WithIdentifier(val));
        }

        private static string TokenNotYetValidError(DateTime notBefore)
        {
            var timestamp = notBefore.ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture);
            return $"Token will not be valid until {timestamp}";
        }

        private static string TokenExpiredError(DateTime expires)
        {
            var timestamp = expires.ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture);
            return $"Token expired at {timestamp}";
        }

        private static string InvalidTokenError(string reason)
        {
            return $"Invalid token ({reason})";
        }

        private class ClaimsReader
        {
            private readonly ClaimsPrincipal _principal;
            private readonly SecurityToken _token;
            private readonly JwtSecurityToken _jwt;

            public ClaimsReader(ClaimsPrincipal principal, SecurityToken token)
            {
                _principal = principal;
                _token = token;
                _jwt = token as JwtSecurityToken;
            }

            public Errorable<DateTime> IssuedAt()
            {
                var iat = _jwt?.Payload.Iat;
                if (!iat.HasValue)
                {
                    return Errorable.Failure<DateTime>("Missing issued-at claim on token.");
                }

                return Errorable.Success(EpochTime.DateTime(iat.Value));
            }

            public Errorable<DateTimeOffset> NotBefore()
                => Errorable.Success(new DateTimeOffset(_token.ValidFrom));

            public Errorable<DateTimeOffset> NotAfter()
                => Errorable.Success(new DateTimeOffset(_token.ValidTo));

            public Errorable<string> Audience()
            {
                // We don't expect multiple audiences to appear in the token
                var audience = _jwt?.Audiences?.SingleOrDefault();
                if (audience == null)
                {
                    return Errorable.Failure<string>("Missing single audience claim on token.");
                }

                return Errorable.Success(audience);
            }

            public Errorable<string> Issuer()
                => Errorable.Success(_token.Issuer);

            public Errorable<IEnumerable<string>> ApplicationIds()
                => Errorable.Success(ReadAll(Claims.Application));

            public Errorable<IEnumerable<IPAddress>> IpAddresses()
                => ReadAll(Claims.IpAddress).Select(ParseIpAddress).Reduce();

            public Errorable<string> VirtualMachineId()
                => Errorable.Success(Read(Claims.VirtualMachineId));

            public Errorable<string> EntitlementId()
            {
                var entitlementId = Read(Claims.EntitlementId);
                if (string.IsNullOrEmpty(entitlementId))
                {
                    return Errorable.Failure<string>("Missing entitlement identifier in token.");
                }

                return Errorable.Success(entitlementId);
            }

            private static Errorable<IPAddress> ParseIpAddress(string value)
            {
                return IPAddress.TryParse(value, out var parsedAddress)
                    ? Errorable.Success(parsedAddress)
                    : Errorable.Failure<IPAddress>(InvalidTokenError($"Invalid IP claim: {value}"));
            }

            private string Read(string claimId)
                => _principal.FindFirst(claimId)?.Value;

            private IEnumerable<string> ReadAll(string claimId)
                => _principal.FindAll(claimId).Select(c => c.Value);
        }
    }
}
