using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// An <see cref="IEntitlementParser"/> implementation for extracting <see cref="NodeEntitlements"/>
    /// objects from JWT tokens.
    /// </summary>
    public class JwtEntitlementParser : IEntitlementParser
    {
        private readonly string _expectedAudience;
        private readonly string _expectedIssuer;
        private readonly SecurityKey _signingKey;
        private readonly SecurityKey _encryptionKey;

        /// <summary>
        /// Creates a <see cref="JwtEntitlementParser"/> instance.
        /// </summary>
        /// <param name="expectedAudience">The audience value expected to be found in the tokens to be parsed.</param>
        /// <param name="expectedIssuer">The issuer value expected to be found in the tokens to be parsed.</param>
        /// <param name="signingKey">The key that the tokens are expected to be signed with, or null if not signed.</param>
        /// <param name="encryptingKey">The key that the tokens are expected to be encrypted with, or null if not encrypted.</param>
        public JwtEntitlementParser(
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
        /// Builds a <see cref="NodeEntitlements"/> from a JWT token string.
        /// </summary>
        /// <param name="token">The JWT token string</param>
        /// <returns>
        /// An <see cref="Errorable{NodeEntitlements}"/> containing the result, or an
        /// <see cref="Errorable.Failure{NodeEntitlements}"/> if it failed to validate correctly.
        /// </returns>
        public Errorable<NodeEntitlements> Parse(string token)
        {
            return ExtractJwt(token).Map(CreateEntitlementPropertyProvider).Bind(NodeEntitlements.Build);
        }

        private static IEntitlementPropertyProvider CreateEntitlementPropertyProvider(ClaimsPrincipal principal, JwtSecurityToken jwt)
        {
            return new JwtEntitlementPropertyProvider(principal, jwt);
        }

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
                ValidateIssuerSigningKey = true,
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
