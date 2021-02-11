using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebCrawler
{
    class requestfun4
    {
        private HttpClient _httpClient;
        private Frontier _frontier;

        private string _validSchemes = "http, https"; // ignore mailto, tel, news, ftp etc.



        public requestfun4(Frontier frontier)
        {
            _frontier = frontier;

            // setup http 
            var socketsHandler = new SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                PooledConnectionLifetime = TimeSpan.FromSeconds(60),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                MaxConnectionsPerServer = 3
            };

            _httpClient = new HttpClient(socketsHandler);
        }

        public async Task SendAsync(string url)
        {
            // add start url
            _frontier.AddUrl(url);

            Console.WriteLine("Starting...");

            // tasks
            List<Task> tasks = new List<Task>();
            int numVisitedUrls = 0;
            int maxUrlsToVisit = 100;
            int taskCount = 0;
            int maxTaskCount = 20; // tasks started at once (batch)

            while (numVisitedUrls < maxUrlsToVisit)
            {
                while (taskCount < maxTaskCount)
                {
                    string nextUrl = _frontier.NextUrl();
                    if (nextUrl is null)
                    {
                        break;
                    }
                    else
                    {
                        tasks.Add(DownloadUrl(numVisitedUrls, nextUrl));
                    }

                    taskCount++;
                    numVisitedUrls++;
                }

                await Task.WhenAll(tasks);
                taskCount = 0;
            }

            Console.WriteLine("Done!");
        }
        private async Task DownloadUrl(int taskid, string url)
        {
            Console.WriteLine($"task {taskid}: start");
            var response = await _httpClient.GetAsync(url);
            string responseUri = response.RequestMessage.ToString();
            url = responseUri;
            //string responseUri = response.RequestMessage.RequestUri.ToString();


            if(response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Status not OK. Status={response.StatusCode}");
            }

            string content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"task {taskid}: finish - url: {url} - {FindKeyword(content)} - content: ({(content.Length * sizeof(Char))/1000}kb)");

            // Find new urls in page
            List<string> pageUrls = FindUrls(content);

            // Normalize urls
            pageUrls = NormalizeUrls(url, pageUrls);

            // Add urls to frontier
            _frontier.AddUrls(pageUrls);
        }

        private string FindKeyword(string content)
        {
            return content.Contains("easv") ? "Keyword found" : "Keyword not found";
        }

        private List<string> FindUrls(string page)
        {
            var urlTagPattern = new Regex(@"<a.*?href=[""'](?<url>.*?)[""'].*?>(?<name>.*?)</a>", RegexOptions.IgnoreCase);
            var hrefPattern = new Regex("href\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", RegexOptions.IgnoreCase);
            
            var urls = urlTagPattern.Matches(page);

            List<string> foundUrls = new List<string>();

            foreach (Match url in urls)
            {
                foundUrls.Add(hrefPattern.Match(url.Value).Groups[1].Value);
            }

            return foundUrls;
        }

                private string NormalizeUrl(string baseUrl, string url)
        {
            // Urls to lowercase
            url.ToLower();
            baseUrl.ToLower();

            Uri validUri;

            // Test if url is valid by using TryCreate
            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out validUri))
            {

                // Turn a relative url > absolute url
                if (!validUri.IsAbsoluteUri)
                {
                    // Create base uri
                    UriBuilder baseUri = new UriBuilder(baseUrl);
                    baseUri.Host = new UriBuilder(baseUrl).Host;
                    baseUri.Scheme = new UriBuilder(baseUrl).Scheme;
                    baseUri.Port = new UriBuilder(baseUrl).Port;

                    // Create absolute url from baseUrl + relative url
                    if (Uri.TryCreate(baseUri.Uri, validUri, out validUri))
                    {
                        Console.WriteLine("Found: " + validUri.ToString() + " @" + validUri.Scheme + "://" + validUri.Host);
                    }
                }

                // Remove # part from url 
                UriBuilder noFragmentUri = new UriBuilder(validUri);
                noFragmentUri.Fragment = null;
                validUri = noFragmentUri.Uri;

                // Check scheme is valid
                if(_validSchemes.ToArray().Any(validUri.Scheme.Contains))
                {
                    // Return valid url
                    return validUri.ToString();
                }
                
                // Invalid scheme
                Console.WriteLine("Skipping: " + validUri.ToString() + " >> invalid scheme: " + validUri.Scheme);
                return null;
            }

            // Url could not be created
            Console.WriteLine("Skipping: " + url + " >> invalid url");
            return null;
        }

        private List<string> NormalizeUrls(string hostUrl, List<string> pageUrls)
        {
            for (int i = 0; i < pageUrls.Count; i++)
            {
                pageUrls[i] = NormalizeUrl(hostUrl, pageUrls[i]);
                if (pageUrls[i] == null)
                {
                    pageUrls.RemoveAt(i);
                }
            }

            return pageUrls;
        }
    }
}
