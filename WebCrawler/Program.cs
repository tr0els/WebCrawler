using System;

namespace WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            // Start url
            string url = "https://www.easv.dk/da/";

            // Initialize frontier and add url
            Frontier frontier = new Frontier();
            frontier.AddUrl(url);

            // Prepare crawler and start
            Crawler spider = new Crawler(frontier);
            spider.StartCrawling();
        }
    }
}
