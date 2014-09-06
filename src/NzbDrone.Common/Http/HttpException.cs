using System;

namespace NzbDrone.Common.Http
{
    public class HttpException : Exception
    {
        public HttpRequest Request { get; private set; }
        public HttpResponse Response { get; private set; }

        public HttpException(HttpRequest request, HttpResponse response)
            : base(string.Format("HTTP request failed: [{0}] [{1}] at [{2}]", (int)response.StatusCode, request.Method, request.Url.ToString()))
        {
            Request = request;
            Response = response;
        }

        public override string ToString()
        {
            if (Response != null)
            {
                return base.ToString() + Environment.NewLine + Response.Content;
            }

            return base.ToString();
        }
    }
}