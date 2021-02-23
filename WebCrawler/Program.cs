using System;
using System.Threading.Tasks;

namespace WebCrawler
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            // Start url
            string url = "https://www.easv.dk/";
            string url2 = "https://www.google.com/search?q=autonomous+agent";


            // Initialize frontier and add url
            Frontier frontier = new Frontier();
            frontier.AddUrl(url);

            // Sequential crawler
            Crawler smallSpider = new Crawler(frontier);
            //smallSpider.StartCrawling();

            // Parallel crawler using threads
            ThreadCrawler giantHouseSpider = new ThreadCrawler(frontier);
            await giantHouseSpider.SendAsync();
            
        }
    }
}
