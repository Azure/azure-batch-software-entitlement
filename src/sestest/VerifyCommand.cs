using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Functionality for the <c>submit</c> command
    /// </summary>
    public sealed class VerifyCommand : CommandBase
    {
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
            var server = FindServer(commandLine);
            var token = FindToken(commandLine);
            var app = FindApplication(commandLine);
            var api = FindApiVersion(commandLine);

            Errorable<string> result = await server.With(token).With(app).With(api)
                .MapAsync(SubmitToken);
            return result.Match(LogResult, LogErrors);
        }

        private async Task<string> SubmitToken(Uri server, string token, string app, string api)
        {
            var builder = new UriBuilder(server);
            //TODO: Do this in a path safe way similar to Path.Combine
            builder.Path = builder.Path + "softwareEntitlements";
            builder.Query = CreateParameters(("api-version", api));

            var uri = builder.Uri;

            using (var client = new HttpClient())
            {
                var request = new SoftwareEntitlementRequest
                {
                    Token = token,
                    ApplicationId = app
                };

                var requestJson = JsonConvert.SerializeObject(request);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                var result = await client.PostAsync(uri, content).ConfigureAwait(false);
                Logger.LogInformation($"Status Code: {result.StatusCode} ({(int) result.StatusCode})");

                return await result.Content.ReadAsStringAsync();
            }
        }

        private int LogResult(string response)
        {
            var pretty = JsonPrettify(response).AsLines();
            Logger.LogInformation(pretty);
            return 0;
        }

        private Errorable<string> FindToken(VerifyCommandLine commandLine)
        {
            var token = commandLine.Token;
            if (string.IsNullOrEmpty(token))
            {
                token = ReadEnvironmentVariable("AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN");
            }

            if (string.IsNullOrEmpty(token))
            {
                return Errorable.Failure<string>(
                    "No token supplied on command line and AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN not set.");
            }

            return Errorable.Success(token);
        }

        private Errorable<string> FindApplication(VerifyCommandLine commandLine)
        {
            var application = commandLine.Application;
            if (string.IsNullOrEmpty(application))
            {
                return Errorable.Failure<string>("No application specified.");
            }

            return Errorable.Success(application);
        }

        private Errorable<Uri> FindServer(VerifyCommandLine commandLine)
        {
            var server = commandLine.Server;
            if (string.IsNullOrEmpty(server))
            {
                server = ReadEnvironmentVariable("AZ_BATCH_ACCOUNT_URL");
            }

            if (string.IsNullOrEmpty(server))
            {
                return Errorable.Failure<Uri>("No server supplied on command line and AZ_BATCH_ACCOUNT_URL not set.");
            }

            try
            {
                return Errorable.Success(new Uri(commandLine.Server));
            }
            catch (UriFormatException ex)
            {
                return Errorable.Failure<Uri>(
                    $"Failed to parse server url ({ex.Message})");
            }
        }

        private Errorable<string> FindApiVersion(VerifyCommandLine commandLine)
        {
            var version = commandLine.ApiVersion;
            if (string.IsNullOrEmpty(version))
            {
                version = "2017-05-01.5.0";
            }

            return Errorable.Success(version);
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

        private string CreateParameters(params (string name, string value)[] parameters)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            foreach (var p in parameters)
            {
                query[p.name] = p.value;
            }

            return query.ToString();
        }

        /// <summary>
        /// Pretty print a JSON string for logging
        /// </summary>
        /// <remarks>
        /// As found on StackOverflow: https://stackoverflow.com/a/30329731/30280
        /// </remarks>
        /// <param name="json">JSON string to reformat.</param>
        /// <returns>Equivalent JSON with nice formatting.</returns>
        private static string JsonPrettify(string json)
        {
            using (var stringReader = new StringReader(json))
            {
                using (var stringWriter = new StringWriter())
                {
                    var jsonReader = new JsonTextReader(stringReader);
                    var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                    jsonWriter.WriteToken(jsonReader);
                    return stringWriter.ToString();
                }
            }
        }
    }
}
