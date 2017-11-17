using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
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

        // Verifier used to check tokens
        private readonly TokenVerifier _verifier;

        private const string ApiVersion201705 =  "2017-05-01.5.0";
        private const string ApiVersion201709 =  "2017-09-01.6.0";

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareEntitlementsController"/> class
        /// </summary>
        /// <param name="serverOptions">Options to use when handling requests.</param>
        /// <param name="logger">Reference to our logger for diagnostics.</param>
        /// <param name="lifetime">Lifetime instance to allow us to automatically shut down if requested.</param>
        public SoftwareEntitlementsController(ServerOptions serverOptions, ILogger logger, IApplicationLifetime lifetime)
        {
            _serverOptions = serverOptions;
            _logger = logger;
            _lifetime = lifetime;

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

            _verifier = new TokenVerifier(_serverOptions.SigningKey, _serverOptions.EncryptionKey);
        }

        [HttpPost]
        [Produces("application/json")]
        public IActionResult RequestEntitlement(
            [FromBody] SoftwareEntitlementRequest entitlementRequest,
            [FromQuery(Name = "api-version")] string apiVersion)
        {
            try
            {
                if (!IsValidApiVersion(apiVersion))
                {
                    return CreateInvalidApiVersionError(entitlementRequest, apiVersion);
                }

                _logger.LogInformation(
                    "Selected api-version is {ApiVersion}",
                    apiVersion);

                if (entitlementRequest == null)
                {
                    return CreateMissingRequestError();
                }

                _logger.LogInformation(
                    "Requesting entitlement for {Application}",
                    entitlementRequest.ApplicationId);
                _logger.LogDebug("Request token: {Token}", entitlementRequest.Token);

                var remoteAddress = HttpContext.Connection.RemoteIpAddress;
                _logger.LogDebug("Remote Address: {Address}", remoteAddress);

                Errorable<NodeEntitlements> verificationResult = _verifier.Verify(
                    entitlementRequest.Token,
                    _serverOptions.Audience,
                    _serverOptions.Issuer,
                    entitlementRequest.ApplicationId,
                    remoteAddress);

                return verificationResult.Match(
                    whenSuccessful: entitlement => CreateEntitlementApprovedResponse(apiVersion, entitlement),
                    whenFailure: errors => CreateEntitlementDeniedError(entitlementRequest, errors));
            }
            finally
            {
                if (_serverOptions.Flags.HasFlag(ServerFlags.ExitAfterRequest))
                {
                    _lifetime.StopApplication();
                }
            }
        }

        private ObjectResult CreateEntitlementApprovedResponse(string apiVersion, NodeEntitlements entitlement)
        {
            var response = new SoftwareEntitlementSuccessfulResponse
            {
                EntitlementId = entitlement.Identifier,
            };

            if (ApiSupportsVirtualMachineId(apiVersion))
            {
                response.VirtualMachineId = entitlement.VirtualMachineId;
            }

            if (ApiSupportsExpiryTimestamp(apiVersion))
            {
                response.Expiry = entitlement.NotAfter;
            }

            return Ok(response);
        }

        private ObjectResult CreateEntitlementDeniedError(SoftwareEntitlementRequest entitlementRequest,
            IEnumerable<string> errors)
        {
            foreach (var e in errors)
            {
                _logger.LogError(e);
            }

            var error = new SoftwareEntitlementFailureResponse
            {
                Code = "EntitlementDenied",
                Message = new ErrorMessage($"Entitlement for {entitlementRequest.ApplicationId} was denied.")
            };

            return StatusCode(403, error);
        }

        private ObjectResult CreateMissingRequestError()
        {
            _logger.LogError("No software entitlement request made");

            var error = new SoftwareEntitlementFailureResponse
            {
                Code = "EntitlementDenied",
                Message = new ErrorMessage("No software entitlement request made.")
            };

            return StatusCode(400, error);
        }

        private ObjectResult CreateInvalidApiVersionError(SoftwareEntitlementRequest entitlementRequest, string apiVersion)
        {
            _logger.LogError(
                "Selected api-version of {ApiVersion} is not supported; denying entitlement request.",
                apiVersion);

            var error = new SoftwareEntitlementFailureResponse
            {
                Code = "EntitlementDenied",
                Message = new ErrorMessage($"Entitlement for {entitlementRequest.ApplicationId} was denied.")
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

        private bool ApiSupportsVirtualMachineId(string apiVersion)
        {
            return string.Equals(apiVersion, ApiVersion201705, StringComparison.Ordinal);
        }

        private bool ApiSupportsExpiryTimestamp(string apiVersion)
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
