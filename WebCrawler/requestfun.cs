using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler
{
    class requestfun
    {

        public async Task<string> GetData(string starturl) 
        {
            var url = new Uri(starturl);
            var httpClient = new HttpClient();

            try
            {
                var result = await httpClient.GetStringAsync(url);
                string checkResult = result.ToString();
                httpClient.Dispose();
                return checkResult;
            }
            catch (Exception ex)
            {
                string checkResult = "Error " + ex.ToString();
                httpClient.Dispose();
                return checkResult;
            }
        }
    }
}
