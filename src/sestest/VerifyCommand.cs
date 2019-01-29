using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Functionality for the <c>submit</c> command
    /// </summary>
    public sealed class VerifyCommand : CommandBase
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="VerifyCommand"/> class
        /// </summary>
        /// <param name="logger">A logger to use while executing.</param>
        public VerifyCommand(ILogger logger) : base(logger)
        {
        }

        /// <summary>
        /// Generate a new token
        /// </summary>
        /// <param name="commandLine">Configuration from the command line.</param>
        /// <returns>Results of execution (0 = success).</returns>
        public async Task<int> Execute(VerifyCommandLine commandLine)
        {
            var exitCodeResult = await
                (
                from server in FindServer(commandLine)
                join token in FindToken(commandLine) on true equals true
                join app in FindApplication(commandLine) on true equals true
                join api in FindApiVersion(commandLine) on true equals true
                select SubmitToken(server, token, app, api)
                ).AsTask().ConfigureAwait(false);

            return exitCodeResult.LogIfFailed(Logger, ResultCodes.Failed);
        }

        private async Task<int> SubmitToken(Uri server, string token, string app, string api)
        {
            var serverPath = server.AbsolutePath.EndsWith('/')
                ? server.AbsolutePath
                : server.AbsolutePath + "/";

            var builder = new UriBuilder(server.Scheme, server.Host, server.Port)
            {
                Path = serverPath + "softwareEntitlements",
                Query = CreateParameters(("api-version", api))
            };

            var uri = builder.Uri;

            using (var handler = GetClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    var requestBody = new ApproveRequestBody
                    {
                        Token = token,
                        ApplicationId = app
                    };

                    var requestJson = JsonConvert.SerializeObject(requestBody, SerializerSettings);

                    // Create the request content, specifying the content type with the "odata" qualifier
                    // (without this the request is rejected with an HTTP 400).
                    var content = new StringContent(requestJson, Encoding.UTF8);
                    content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("application/json; odata=minimalmetadata");

                    var result = await client.PostAsync(uri, content).ConfigureAwait(false);

                    Logger.LogInformation($"Status Code: {result.StatusCode} ({(int)result.StatusCode})");

                    var responseContent = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!result.IsSuccessStatusCode)
                    {
                        Logger.LogError(result.ReasonPhrase);
                        return ResultCodes.Failed;
                    }

                    Logger.LogJson(responseContent);
                    return ResultCodes.Success;
                }
            }
        }

        private static HttpClientHandler GetClientHandler()
        {
            var handler = new HttpClientHandler();

            // Ensure we pick up any HTTPS proxy configured in an environment variable
            // (as the SDK does).
            var proxyOrigin = Environment.GetEnvironmentVariable("https_proxy");
            if (proxyOrigin != null)
            {
                handler.Proxy = new WebProxy(proxyOrigin);
            }

            return handler;
        }

        private Result<string, ErrorCollection> FindToken(VerifyCommandLine commandLine)
        {
            var token = commandLine.Token;
            if (string.IsNullOrEmpty(token))
            {
                token = ReadEnvironmentVariable("AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN");
            }

            if (string.IsNullOrEmpty(token))
            {
                return ErrorCollection.Create(
                    "No token supplied on command line and AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN not set.");
            }

            return token;
        }

        private static Result<string, ErrorCollection> FindApplication(VerifyCommandLine commandLine)
        {
            var application = commandLine.Application;
            if (string.IsNullOrEmpty(application))
            {
                return ErrorCollection.Create("No application specified.");
            }

            return application;
        }

        private Result<Uri, ErrorCollection> FindServer(VerifyCommandLine commandLine)
        {
            var server = commandLine.Server;
            if (string.IsNullOrEmpty(server))
            {
                server = ReadEnvironmentVariable("AZ_BATCH_ACCOUNT_URL");
            }

            if (string.IsNullOrEmpty(server))
            {
                return ErrorCollection.Create("No server supplied on command line and AZ_BATCH_ACCOUNT_URL not set.");
            }

            try
            {
                return new Uri(server);
            }
            catch (UriFormatException ex)
            {
                return ErrorCollection.Create(
                    $"Failed to parse server url ({ex.Message})");
            }
        }

        private static Result<string, ErrorCollection> FindApiVersion(VerifyCommandLine commandLine)
        {
            var version = commandLine.ApiVersion;
            if (string.IsNullOrEmpty(version))
            {
                version = "2017-05-01.5.0";
            }

            return version;
        }

        private string ReadEnvironmentVariable(string name)
        {
            var result = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrEmpty(result))
            {
                Logger.LogInformation("Using environment variable " + name);
            }

            return result;
        }

        private static string CreateParameters(params (string name, string value)[] parameters)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            foreach (var p in parameters)
            {
                query[p.name] = p.value;
            }

            return query.ToString();
        }
    }
}
