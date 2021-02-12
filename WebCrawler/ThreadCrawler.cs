using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebCrawler
{
    class ThreadCrawler
    {
        private HttpClient _httpClient;
        private Frontier _frontier;

        private int _maxContentLength = 1000000; // bytes
        private string _validContentTypes = "text/html, text/plain, text/xml";
        private string _validSchemes = "http, https"; // ignore mailto, tel, news, ftp etc.

        private int _maxConnectionsPerServer = 10; // max simultaneous http requests per server
        private int _maxUrlsToVisit = 200; // stops crawler after visiting n urls
        private int _maxTime = 60*2; // stops crawler after n seconds

        private int _maxTaskCount = 10; // tasks spawned at once (batch size)
        private string _keywordToFind = "autonomous agent"; // empty to disable

        private int _numVisitedUrls = 0;
        private int _numKbDownloaded = 0;
        private int _taskCount = 0;

        public ThreadCrawler(Frontier frontier)
        {
            _frontier = frontier;

            // setup http 
            var socketsHandler = new SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                PooledConnectionLifetime = TimeSpan.FromSeconds(60),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                MaxConnectionsPerServer = _maxConnectionsPerServer
            };

            _httpClient = new HttpClient(socketsHandler);
        }

        public async Task SendAsync()
        {
            var sw = Stopwatch.StartNew();

            Console.WriteLine("Starting...");

            // tasks
            List<Task> tasks = new List<Task>();

            while (_numVisitedUrls < _maxUrlsToVisit && sw.Elapsed.Seconds < _maxTime && _frontier.UrlsInFrontier() > 0)
            {
                while (_numVisitedUrls < _maxUrlsToVisit && sw.Elapsed.Seconds < _maxTime && _taskCount < _maxTaskCount && _frontier.UrlsInFrontier() > 0)
                {
                    _numVisitedUrls++;
                    _taskCount++;
                    tasks.Add(DownloadUrl(_numVisitedUrls, _frontier.NextUrl()));
                }

                await Task.WhenAll(tasks);
                _taskCount = 0;
            }

            sw.Stop();

            Console.WriteLine("");
            Console.WriteLine($"Done crawling {_numVisitedUrls} urls in {sw.Elapsed.Seconds}secs ({sw.Elapsed.Minutes}mins)");
            Console.WriteLine($"Frontier constains another {_frontier.UrlsInFrontier()} urls");
            Console.WriteLine($"A total of {_numKbDownloaded/1000}MB has been downloaded");
            if (!string.IsNullOrEmpty(_keywordToFind))
            {
                Console.WriteLine($"Keyword {_keywordToFind} was found in {_frontier.UrlsWithKeyword()} urls");
            }

            _httpClient.Dispose();
        }

        // Visit url
        private async Task DownloadUrl(int urlNum, string url)
        {
            try
            {
                // Send request to get ONLY headers
                var response1 = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url)
                );

                // Check status code
                if (!response1.IsSuccessStatusCode) {
                    Console.WriteLine($"Error Url #{urlNum} - url: {url} returned http status code {response1.StatusCode}");
                }

                // Get Content Headers
                var contentHeaders = response1.Content.Headers;

                // Check header for content-length
                if (contentHeaders.ContentLength > _maxContentLength)
                {
                    Console.WriteLine($"Error Url #{urlNum} - url: {url} Content-length ({contentHeaders.ContentLength}) exceeds the limit (" + _maxContentLength + ")");
                    return;
                }

                // Check header for content-types
                if (contentHeaders.ContentType.MediaType != null && !_validContentTypes.ToArray().Any(contentHeaders.ContentType.MediaType.Contains))
                {
                    Console.WriteLine($"Error Url #{urlNum} - url: {url} Wrong content type ({contentHeaders.ContentType.MediaType})");
                    return;
                }

                // Now get url content
                var response = await _httpClient.GetAsync(url);
                url = response.RequestMessage.RequestUri.ToString();

                string content = await response.Content.ReadAsStringAsync();
                int contentSizeInKb = (int)response.Content.Headers.ContentLength.Value / 1000;
                _numKbDownloaded += contentSizeInKb;

                //Console.WriteLine($"Visited url #{urlNum} - {url} - content: ({contentSizeInKb}kb)");

                // Find new urls in page
                List<string> pageUrls = FindUrls(content);

                // Normalize urls
                pageUrls = NormalizeUrls(url, pageUrls);

                // Add urls to frontier
                _frontier.AddUrls(pageUrls);

                // Check for keyword?
                if (!string.IsNullOrEmpty(_keywordToFind) && FindKeyword(_keywordToFind, content))
                {
                    _frontier.AddKeywordUrl(url);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error Url #{urlNum} - {url} Exception caught: " + e.Message);
            }
        }

        private bool FindKeyword(string keyword, string content)
        {
            // Case insensitive string matching using regex
            return content.ToLower().Contains(keyword.ToLower());
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
                        //Console.WriteLine("Found: " + validUri.ToString() + " @" + validUri.Scheme + "://" + validUri.Host);
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
