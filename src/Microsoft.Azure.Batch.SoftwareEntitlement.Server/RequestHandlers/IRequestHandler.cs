using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public interface IRequestHandler<in TRequestContext>
    {
        Response Handle(HttpContext httpContext, TRequestContext requestContext);
    }
}
