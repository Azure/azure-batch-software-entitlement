using System;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RouteConstraints
{
    [AttributeUsage(AttributeTargets.Method)]
    public class InvalidVersionConstraintAttribute : Attribute, IActionConstraint
    {
        public int Order { get; set; }

        public bool Accept(ActionConstraintContext context)
        {
            var queryParameters = context.RouteContext.HttpContext.Request.Query;
            if (!queryParameters.ContainsKey(ApiVersions.ParameterName))
            {
                return false;
            }

            string specifiedVersion = queryParameters[ApiVersions.ParameterName];
            if (ApiVersions.IsValidApiVersion(specifiedVersion))
            {
                return false;
            }

            return true;
        }
    }
}
