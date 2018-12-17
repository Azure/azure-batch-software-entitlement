using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model
{
    public class Response
    {
        public Response(int statusCode, IResponseValue value = null)
        {
            StatusCode = statusCode;
            Value = value;
        }

        public static Response CreateSuccess(IResponseValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return new Response(StatusCodes.Status200OK, value);
        }

        /// <summary>
        /// The HTTP status code for this response
        /// </summary>
        public int StatusCode { get; }

        public IResponseValue Value { get; }
    }
}
