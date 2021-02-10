using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace WebCrawler
{
    public class Crawler
    {
        private Frontier _frontier;
        private string _currentUrl;

        // Default crawler settings
        private int _maxRedirects = 50;
        private int _maxContentLength = 1000000; // bytes
        private string _validContentTypes = "text/html, text/plain, text/xml";
        private string _validSchemes = "http, https"; // ignore mailto, tel, news, ftp etc.

        // Default url client settings
        private int _delay = 0; // ms
        private int _timeout = 100000; // ms
        private string _userAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; de; rv:1.9.2.3) Gecko/20100401 Firefox/3.6.3";

        public Crawler(Frontier frontier)
        {
            _frontier = frontier;
        }

        public void StartCrawling()
        {
            // Check if frontier is valid
            CheckFrontier();

            while (NextUrlToCrawl())
            {
                // Get page from url
                string page = DownloadPage();
                if(page is null) continue;

                // Find new urls in page
                List<string> pageUrls = FindUrls(page);

                // Normalize urls
                pageUrls = NormalizeUrls(_currentUrl, pageUrls);

                // Add urls to frontier
                _frontier.AddUrls(pageUrls);
            }

            Console.WriteLine("Done crawling.. Frontier has no more urls to crawl");
        }

        private bool NextUrlToCrawl()
        {
            // If there is a next url to crawl store it and return true
            _currentUrl = _frontier.NextUrl();
            return (_currentUrl is null) ? false : true;
        }

        private string DownloadPage()
        {
            // Handy uri and web helpers
            UriBuilder ub = new UriBuilder(_currentUrl);
            MyWebClient wc = new MyWebClient();

            // Disguise crawler as a browser
            // Otherwise it seems Facebook and other redirects to unsupported browser url
            wc.Headers.Add ("user-agent", _userAgent);

            Console.Write("Crawling: " + _currentUrl);

            try
            {
                // WebRequest is more lowlevel and has more options than WebClient
                HttpWebRequest webRequest;
                webRequest = WebRequest.CreateHttp(_currentUrl);
                webRequest.Method = "HEAD";
                webRequest.UserAgent = _userAgent;
                webRequest.MaximumAutomaticRedirections = _maxRedirects;
                webRequest.Timeout = _timeout;
                // Add more if needed

                long contentLength;
                string contentType;
                string finalUrl;

                // Sends the HttpWebRequest and waits for a response.
                // Using is for closing connection/cleaning up when done
                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    // Getting response-header only (no content)
                    // Getting content type and size 
                    // Getting final url after potential redirects
                    contentLength = webResponse.ContentLength;
                    contentType = webResponse.ContentType;
                    finalUrl = webResponse.ResponseUri.ToString();
                }

                // Check if url has changed (redirected)
                if (!_currentUrl.Equals(finalUrl))
                {
                    Console.WriteLine(">> Redirected to: " + finalUrl);
                    _currentUrl = finalUrl;
                }

                // Check header for content-length
                if (contentLength > _maxContentLength)
                {
                    Console.WriteLine(" >> Error: Content-length (" + contentLength + ") exceeds the limit (" + _maxContentLength + ")");
                    return null;
                }

                // Check header for content-types
                if (!_validContentTypes.ToArray().Any(contentType.Contains))
                {
                    Console.WriteLine(" >> Error: Wrong content type (" + contentType + ")");
                    return null;
                }

                // Get url resource content 
                string page = wc.DownloadString(finalUrl);
                Console.WriteLine(" >> Success");
                
                return page;
            }
            catch (WebException we)
            {
                // handle server error
                if (we.Response != null)
                {
                    HttpWebResponse response = (System.Net.HttpWebResponse) we.Response;
                    Console.WriteLine(" >> Error (" + (int) response.StatusCode + ") " + response.StatusCode);
                }
                else
                {
                    Console.WriteLine(" >> Error No Response");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(" >> Error" + e.Message);
            }

            return null;
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

        private bool IsKeywordInContent(string content)
        {
            // if string contains keyword return true
            return true;
        }

        private void CheckFrontier()
        {
            if (_frontier is null)
            {
                Console.WriteLine("Crawler error: Frontier is null");
                Environment.Exit(-1);
            }
        }

        public void SetFrontier(Frontier frontier)
        {
            _frontier = frontier;
        }
    }
}
