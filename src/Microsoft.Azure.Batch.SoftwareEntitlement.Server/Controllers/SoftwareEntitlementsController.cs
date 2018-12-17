using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.RouteConstraints;
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

        private readonly ApproveV1RequestHandler _approveV1RequestHandler;
        private readonly ApproveV2RequestHandler _approveV2RequestHandler;

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
            _approveV1RequestHandler = new ApproveV1RequestHandler(_logger, verifier);
            _approveV2RequestHandler = new ApproveV2RequestHandler(_logger, verifier);

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
        [MissingVersionConstraint]
        [Produces("application/json")]
        public IActionResult MissingApiVersion()
        {
            _logger.LogDebug("No api-version specified");
            return HandleApiVersionError("Missing api-version query parameter; denying entitlement request.");
        }

        [HttpPost]
        [InvalidVersionConstraint]
        [Produces("application/json")]
        public IActionResult InvalidApiVersion(
            [FromQuery(Name = ApiVersions.ParameterName)] string apiVersion)
        {
            _logger.LogDebug("Invalid api-version specified");
            return HandleApiVersionError(
                $"Selected api-version of {apiVersion} is not supported; denying entitlement request.");
        }

        [HttpPost]
        [VersionRangeConstraint(maxVersion: ApiVersions.June012017)]
        [Produces("application/json")]
        public IActionResult ApproveV1(
            [FromBody] ApproveRequestBody requestBody,
            [FromQuery(Name = ApiVersions.ParameterName)] string apiVersion)
        {
            _logger.LogInformation(
                "Selected api-version is {ApiVersion}",
                apiVersion);

            return HandleRequest(_approveV1RequestHandler, requestBody);
        }

        [HttpPost]
        [VersionRangeConstraint(minVersion: ApiVersions.Sept012017, maxVersion: ApiVersions.August012018)]
        [Produces("application/json")]
        public IActionResult ApproveV2(
            [FromBody] ApproveRequestBody requestBody,
            [FromQuery(Name = ApiVersions.ParameterName)] string apiVersion)
        {
            _logger.LogInformation(
                "Selected api-version is {ApiVersion}",
                apiVersion);

            return HandleRequest(_approveV2RequestHandler, requestBody);
        }

        private IActionResult HandleApiVersionError(string errorMessage)
        {
            var responseValue = new FailureResponse("EntitlementDenied", new ErrorMessage(errorMessage));
            var response = new Response(StatusCodes.Status400BadRequest, responseValue);
            return this.CreateActionResult(response);
        }

        private IActionResult HandleRequest<TRequestBody>(
            IRequestHandler<TRequestBody> requestHandler,
            TRequestBody requestBody)
        {
            try
            {
                var response = requestHandler.Handle(HttpContext, requestBody);
                return this.CreateActionResult(response);
            }
            finally
            {
                if (_serverOptions.Flags.HasFlag(ServerFlags.ExitAfterRequest))
                {
                    _lifetime.StopApplication();
                }
            }
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
