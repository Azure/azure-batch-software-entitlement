using CommandLine;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    [Verb("verify", HelpText = "Submit a token for verification")]
    public sealed class VerifyCommandLine : CommandLineBase
    {
        [Option("token",
            HelpText = "Software entitlement token to verify (defaults to the environment variable AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN)")]
        public string Token { get; set; }

        [Option("app", HelpText = "The application for which entitlement is requested.")]
        public string Application { get; set; }

        [Option("server",
            HelpText = "URL for the software entitlement server (defaults to the environment variable AZ_BATCH_ACCOUNT_URL)")]
        public string Server { get; set; }

        [Option("api-version",
            HelpText = "API version to specify when making the request (defaults to '2017-05-01.5.0')")]
        public string ApiVersion { get; set; } = "2017-05-01.5.0";
    }
}
