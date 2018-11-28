using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.Controllers
{
    [Route("softwareEntitlements")]
    public class SoftwareEntitlementsController : Controller
    {
        // Configuration options
        private readonly ServerOptions _serverOptions;

        // A reference to our logger
        private readonly ILogger _logger;
        private readonly IApplicationLifetime _lifetime;

        // Verifier used to check token
        private readonly TokenVerifier _verifier;

        private const string ApiVersion201705 = "2017-05-01.5.0";
        private const string ApiVersion201709 = "2017-09-01.6.0";

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareEntitlementsController"/> class
        /// </summary>
        /// <param name="serverOptions">Options to use when handling requests.</param>
        /// <param name="logger">Reference to our logger for diagnostics.</param>
        /// <param name="lifetime">Lifetime instance to allow us to automatically shut down if requested.</param>
        /// <param name="verifier">A component responsible for checking data in the request against the claims
        /// in the token</param>
        public SoftwareEntitlementsController(
            ServerOptions serverOptions,
            ILogger logger,
            IApplicationLifetime lifetime,
            TokenVerifier verifier)
        {
            _serverOptions = serverOptions;
            _logger = logger;
            _lifetime = lifetime;
            _verifier = verifier;

            if (_serverOptions.SigningKey != null)
            {
                _logger.LogDebug(
                    "Tokens must be signed with {Credentials}",
                    _serverOptions.SigningKey.KeyId);
            }

            if (_serverOptions.EncryptionKey != null)
            {
                _logger.LogDebug(
                    "Tokens must be encrypted with {Credentials}",
                    _serverOptions.EncryptionKey.KeyId);
            }
        }

        [HttpPost]
        [Produces("application/json")]
        public IActionResult RequestEntitlement(
            [FromBody] SoftwareEntitlementRequestBody entitlementRequestBody,
            [FromQuery(Name = "api-version")] string apiVersion)
        {
            try
            {
                if (!IsValidApiVersion(apiVersion))
                {
                    return CreateBadRequestResponse(
                        $"Selected api-version of {apiVersion} is not supported; denying entitlement request.");
                }

                _logger.LogInformation(
                    "Selected api-version is {ApiVersion}",
                    apiVersion);

                return ExtractApprovalRequestParameters(HttpContext, entitlementRequestBody).Match(
                    whenSuccessful: r => CreateApprovalResponse(r.Request, r.Token, apiVersion),
                    whenFailure: errors => CreateBadRequestResponse(errors));
            }
            finally
            {
                if (_serverOptions.Flags.HasFlag(ServerFlags.ExitAfterRequest))
                {
                    _lifetime.StopApplication();
                }
            }
        }

        private IActionResult CreateApprovalResponse(TokenVerificationRequest request, string token, string apiVersion)
        {
            var verificationResult = _verifier.Verify(request, token);

            return verificationResult.Match(
                whenSuccessful: tokenProperties => CreateApprovalSuccessResponse(apiVersion, tokenProperties),
                whenFailure: errors => CreateEntitlementDeniedResponse(request.ApplicationId, errors));
        }

        /// <summary>
        /// Attempts to extracts all the parameters required for validating an entitlement approval request.
        /// Any error here reflects a badly formed request.
        /// </summary>
        /// <param name="httpContext">The HTTP context of the request</param>
        /// <param name="requestBody">The information in the request body</param>
        /// <returns>
        /// A tuple in which either the <see cref="TokenVerificationRequest"/> and token values
        /// are present if the request was well formed, or an informative error otherwise.
        /// </returns>
        private Errorable<(TokenVerificationRequest Request, string Token)> ExtractApprovalRequestParameters(
            HttpContext httpContext,
            SoftwareEntitlementRequestBody requestBody)
        {
            if (requestBody == null)
            {
                return Errorable.Failure<(TokenVerificationRequest, string)>(
                    "Missing request body from software entitlement request.");
            }

            if (string.IsNullOrEmpty(requestBody.Token))
            {
                return Errorable.Failure<(TokenVerificationRequest, string)>(
                    "Missing token from software entitlement request.");
            }

            if (string.IsNullOrEmpty(requestBody.ApplicationId))
            {
                return Errorable.Failure<(TokenVerificationRequest, string)>(
                    "Missing applicationId value from software entitlement request.");
            }

            var remoteAddress = httpContext.Connection.RemoteIpAddress;
            _logger.LogDebug("Remote Address: {Address}", remoteAddress);

            var request = new TokenVerificationRequest(requestBody.ApplicationId, remoteAddress);

            return Errorable.Success((Request: request, Token: requestBody.Token));
        }

        private ObjectResult CreateApprovalSuccessResponse(string apiVersion, EntitlementTokenProperties tokenProperties)
        {
            var response = new SoftwareEntitlementSuccessfulResponse
            {
                TokenId = tokenProperties.Identifier,
            };

            if (ApiSupportsVirtualMachineId(apiVersion))
            {
                response.VirtualMachineId = tokenProperties.VirtualMachineId;
            }

            if (ApiSupportsExpiryTimestamp(apiVersion))
            {
                response.Expiry = tokenProperties.NotAfter;
            }

            return Ok(response);
        }

        private ObjectResult CreateEntitlementDeniedResponse(string applicationId, IEnumerable<string> errors)
        {
            foreach (var e in errors)
            {
                _logger.LogError(e);
            }

            var error = new SoftwareEntitlementFailureResponse
            {
                Code = "EntitlementDenied",
                Message = new ErrorMessage($"Entitlement for {applicationId} was denied.")
            };

            return StatusCode(403, error);
        }

        private ObjectResult CreateBadRequestResponse(params string[] errors)
            => CreateBadRequestResponse((IEnumerable<string>)errors);

        private ObjectResult CreateBadRequestResponse(IEnumerable<string> errors)
        {
            foreach (var e in errors)
            {
                _logger.LogError(e);
            }

            var message = string.Join("; ", errors);
            var error = new SoftwareEntitlementFailureResponse
            {
                Code = "EntitlementDenied",
                Message = new ErrorMessage(message)
            };

            return StatusCode(400, error);
        }

        /// <summary>
        /// Check to see whether the specified <c>api-version</c> is valid for software entitlements
        /// </summary>
        /// <param name="apiVersion">Api version from the query parameter</param>
        /// <returns>True if it is valid, false otherwise.</returns>
        private bool IsValidApiVersion(string apiVersion)
        {
            if (string.IsNullOrEmpty(apiVersion))
            {
                _logger.LogDebug("No api-version specified");
                return false;
            }

            // Check all the valid apiVersions
            // TODO: Once this list passes three or four items, use a HashSet<string> to do the check more efficiently

            return apiVersion.Equals(ApiVersion201705, StringComparison.Ordinal)
                   || apiVersion.Equals(ApiVersion201709, StringComparison.Ordinal);
        }

        private static bool ApiSupportsVirtualMachineId(string apiVersion)
        {
            return string.Equals(apiVersion, ApiVersion201705, StringComparison.Ordinal);
        }

        private static bool ApiSupportsExpiryTimestamp(string apiVersion)
        {
            return string.Equals(apiVersion, ApiVersion201709, StringComparison.Ordinal);
        }

        public class ServerOptions
        {
            /// <summary>
            /// Gets the key to use when checking the signature on a token
            /// </summary>
            public SecurityKey SigningKey { get; }

            /// <summary>
            /// Gets the key to use when decrypting a token
            /// </summary>
            public SecurityKey EncryptionKey { get; }

            /// <summary>
            /// Gets the audience to which tokens should be addressed
            /// </summary>
            public string Audience { get; }

            /// <summary>
            /// Gets the issuer by which tokens should have been created
            /// </summary>
            public string Issuer { get; }

            /// <summary>
            /// Gets additional flags used to control the server
            /// </summary>
            public ServerFlags Flags { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="ServerOptions"/> class
            /// </summary>
            /// <param name="signingKey">Key to use when checking token signatures.</param>
            /// <param name="encryptionKey">Key to use when decrypting tokens.</param>
            /// <param name="audience">Audience to which tokens should be addressed.</param>
            /// <param name="issuer">Issuer by which tokens should have been created.</param>
            /// <param name="flags">Additional flags for controlling behaviour.</param>
            public ServerOptions(SecurityKey signingKey, SecurityKey encryptionKey, string audience, string issuer, ServerFlags flags)
            {
                SigningKey = signingKey;
                EncryptionKey = encryptionKey;
                Audience = audience;
                Issuer = issuer;
                Flags = flags;
            }
        }

        [Flags]
        public enum ServerFlags
        {
            None = 0,
            ExitAfterRequest = 1
        }
    }
}
