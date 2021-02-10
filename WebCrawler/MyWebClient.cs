using System;
using System.Net;

namespace WebCrawler
{
    // How to make the .NET WebClient class follow redirects and get the target url
    // Unlike its brother HttpWebRequest, the WebClient class automatically follows redirects,
    // but if you need to get the "final" url, you'll need to "wrap" your WebClient in a class that derives from System.Net.WebClient.
    //
    // Credits for this class --> http://www.nullskull.com/a/1484/make-the-webclient-class-follow-redirects-and-get-target-url.aspx

    public class MyWebClient : WebClient
    {
        Uri _responseUri;

        public Uri ResponseUri
        {
            get { return _responseUri; }
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = null;
            try
            {
                response = base.GetWebResponse(request);
                _responseUri = response.ResponseUri;
            }
            catch
            {
            }
            return response;
        }
    }

}
