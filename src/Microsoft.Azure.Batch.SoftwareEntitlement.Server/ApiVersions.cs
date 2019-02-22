using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server
{
    public static class ApiVersions
    {
        public const string ParameterName = "api-version";

        public const string May012017 = "2017-05-01.5.0";
        public const string June012017 = "2017-06-01.5.1";
        public const string Sept012017 = "2017-09-01.6.0";
        public const string March012018 = "2018-03-01.6.1";
        public const string August012018 = "2018-08-01.7.0";
        public const string ApiVersionLatest = "9999-09-09.99.99";

        private static readonly ISet<string> ValidApiVersions = ImmutableHashSet.Create(
            May012017,
            June012017,
            Sept012017,
            March012018,
            August012018,
            ApiVersionLatest);

        /// <summary>
        /// Check to see whether the specified <c>api-version</c> is valid for software entitlements
        /// </summary>
        /// <param name="apiVersion">Api version from the query parameter</param>
        /// <returns>True if it is valid, false otherwise.</returns>
        public static bool IsValidApiVersion(string apiVersion)
            => !string.IsNullOrEmpty(apiVersion) && ValidApiVersions.Contains(apiVersion);
    }
}
