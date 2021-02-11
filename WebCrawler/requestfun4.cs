using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler
{
    class requestfun4
    {
        public void SendAsync()
        {
            int TaskCount = 8;
            var tasks = Enumerable.Range(0, TaskCount).Select(p => googleit());
            Task.WhenAll(tasks).Wait();
        }
        private async Task googleit()
        {
            HttpWebRequest request = WebRequest.Create("http://www.google.com") as HttpWebRequest;
            request.Credentials = CredentialCache.DefaultCredentials;

            var response = await request.GetResponseAsync();
            response.Close();
        }
    }
}
