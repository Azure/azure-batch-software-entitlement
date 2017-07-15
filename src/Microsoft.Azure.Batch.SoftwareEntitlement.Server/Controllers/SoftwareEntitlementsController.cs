using System;
using System.Text;
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
        private readonly Options _options;

        // A reference to our logger

        private readonly ILogger _logger;

        // Verifier used to check tokens
        private readonly TokenVerifier _verifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareEntitlementsController"/> class
        /// </summary>
        /// <param name="options">Options to use when handling requests.</param>
        /// <param name="logger">Reference to our logger for diagnostics.</param>
        public SoftwareEntitlementsController(Options options, ILogger logger)
        {
            _options = options;
            _logger = logger;

            if (_options.SigningKey != null)
            {
                _logger.LogDebug(
                    "Tokens must be signed with {Credentials}",
                    _options.SigningKey.KeyId);
            }

            if (_options.EncryptionKey != null)
            {
                _logger.LogDebug(
                    "Tokens must be encrypted with {Credentials}",
                    _options.EncryptionKey.KeyId);
            }

            _verifier = new TokenVerifier(_options.SigningKey, _options.EncryptionKey);
        }

        [HttpPost]
        [Produces("application/json")]
        public IActionResult RequestEntitlement(
            [FromBody] SoftwareEntitlementRequest entitlementRequest,
            [FromQuery(Name = "api-version")] string apiVersion)
        {
            _logger.LogInformation(
                "Selected api-version is {apiversion}",
                apiVersion);

            _logger.LogInformation(
                "Request entitlement for {application}",
                entitlementRequest.ApplicationId);
            _logger.LogDebug("Request token: {token}", entitlementRequest.Token);

            var remoteAddress = HttpContext.Connection.RemoteIpAddress;
            _logger.LogDebug("Remote Address: {address}", remoteAddress);

            var verificationResult = _verifier.Verify(
                entitlementRequest.Token,
                _options.Audience,
                entitlementRequest.ApplicationId,
                remoteAddress);
            if (!verificationResult.HasValue)
            {
                foreach (var e in verificationResult.Errors)
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

            var entitlement = verificationResult.Value;
            var response = new SoftwareEntitlementSuccessfulResponse
            {
                EntitlementId = entitlement.Identifier,
                VirtualMachineId = entitlement.VirtualMachineId
            };

            return Ok(response);
        }

        public class Options
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
            /// Initializes a new instance of the <see cref="Options"/> class.
            /// </summary>
            /// <param name="signingKey">Key to use when checking token signatures.</param>
            /// <param name="encryptionKey">Key to use when decrypting tokens.</param>
            /// <param name="audience">Audience to which tokens should be addressed.</param>
            public Options(SecurityKey signingKey, SecurityKey encryptionKey, string audience)
            {
                SigningKey = signingKey;
                EncryptionKey = encryptionKey;
                Audience = audience;
            }
        }
    }
}
