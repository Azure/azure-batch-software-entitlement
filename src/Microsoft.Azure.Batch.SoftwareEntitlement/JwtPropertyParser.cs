using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// An <see cref="ITokenPropertyParser"/> implementation for extracting <see cref="EntitlementTokenProperties"/>
    /// objects from JWT tokens.
    /// </summary>
    public class JwtPropertyParser : ITokenPropertyParser
    {
        private readonly string _expectedAudience;
        private readonly string _expectedIssuer;
        private readonly SecurityKey _signingKey;
        private readonly SecurityKey _encryptionKey;

        /// <summary>
        /// Creates a <see cref="JwtPropertyParser"/> instance.
        /// </summary>
        /// <param name="expectedAudience">The audience value expected to be found in the tokens to be parsed.</param>
        /// <param name="expectedIssuer">The issuer value expected to be found in the tokens to be parsed.</param>
        /// <param name="signingKey">The key that the tokens are expected to be signed with, or null if not signed.</param>
        /// <param name="encryptingKey">The key that the tokens are expected to be encrypted with, or null if not encrypted.</param>
        public JwtPropertyParser(
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

        /// <summary>
        /// Builds a <see cref="EntitlementTokenProperties"/> from a JWT token string.
        /// </summary>
        /// <param name="token">The JWT token string</param>
        /// <returns>
        /// An <see cref="Errorable{NodeEntitlements}"/> containing the result, or an
        /// error if it failed to validate correctly.
        /// </returns>
        public Errorable<EntitlementTokenProperties> Parse(string token) =>
            (
            from pair in ExtractJwt(token)
            let provider = new JwtPropertyProvider(pair.Principal, pair.Token)
            from entitlements in EntitlementTokenProperties.Build(provider)
            select entitlements
            ).AsErrorable();

        private Errorable<(ClaimsPrincipal Principal, JwtSecurityToken Token)> ExtractJwt(string tokenString)
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
                ValidateIssuerSigningKey = _signingKey != null,
                TokenDecryptionKey = _encryptionKey
            };

            Errorable<(ClaimsPrincipal Principal, JwtSecurityToken Token)> Failure(string error)
                => Errorable.Failure<(ClaimsPrincipal Principal, JwtSecurityToken Token)>(error);

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(tokenString, validationParameters, out var token);

                if (!(token is JwtSecurityToken jwt))
                {
                    return Failure("Validated security token is expected to be a JWT");
                }

                return Errorable.Success((principal, jwt));
            }
            catch (SecurityTokenNotYetValidException exception)
            {
                return Failure(TokenNotYetValidError(exception.NotBefore));
            }
            catch (SecurityTokenExpiredException exception)
            {
                return Failure(TokenExpiredError(exception.Expires));
            }
            catch (SecurityTokenNoExpirationException)
            {
                return Failure(MissingExpirationError());
            }
            catch (SecurityTokenInvalidIssuerException exception)
            {
                return Failure(UnexpectedIssuerError(exception.InvalidIssuer));
            }
            catch (SecurityTokenInvalidAudienceException exception)
            {
                return Failure(UnexpectedAudienceError(exception.InvalidAudience));
            }
            catch (ArgumentException exception)
                when (exception.Message.StartsWith("IDX", StringComparison.Ordinal))
            {
                // This covers a number of cases, including:
                //  - Unexpected number of parts (base-64 strings separated by period characters)
                //  - Invalid characters (not base-64)
                //  - Invalid base-64 strings (not decodable)
                //  - base-64 strings that decode to invalid JWT parts
                return Failure("Token is not well formed");
            }
            catch (Exception exception)
            {
                return Failure(InvalidTokenError(exception.Message));
            }
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

        private static string MissingExpirationError()
            => "Missing token expiration";

        private static string UnexpectedIssuerError(string issuer)
            => $"Invalid issuer {issuer}";

        private static string UnexpectedAudienceError(string audience)
            => $"Invalid audience {audience}";

        private static string InvalidTokenError(string reason)
            => $"Invalid token ({reason})";
    }
}
