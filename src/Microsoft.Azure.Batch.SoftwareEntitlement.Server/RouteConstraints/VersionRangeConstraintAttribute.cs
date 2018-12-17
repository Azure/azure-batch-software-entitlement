using System;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RouteConstraints
{
    [AttributeUsage(AttributeTargets.Method)]
    public class VersionRangeConstraintAttribute : Attribute, IActionConstraint
    {
        private readonly string _minVersion;
        private readonly string _maxVersion;

        public VersionRangeConstraintAttribute(string minVersion = null, string maxVersion = null)
        {
            _minVersion = minVersion;
            _maxVersion = maxVersion;
        }

        public int Order { get; set; }

        public bool Accept(ActionConstraintContext context)
        {
            var queryParameters = context.RouteContext.HttpContext.Request.Query;
            if (!queryParameters.ContainsKey(ApiVersions.ParameterName))
            {
                return false;
            }

            string specifiedVersion = queryParameters[ApiVersions.ParameterName];
            if (!ApiVersions.IsValidApiVersion(specifiedVersion))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_minVersion) && string.CompareOrdinal(specifiedVersion, _minVersion) < 0)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_maxVersion) && string.CompareOrdinal(specifiedVersion, _maxVersion) > 0)
            {
                return false;
            }

            return true;
        }
    }
}
