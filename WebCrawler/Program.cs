using System;
using System.Threading.Tasks;

namespace WebCrawler
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            // Start url
            string url = "https://www.easv.dk/da/";

            // Initialize frontier and add url
            Frontier frontier = new Frontier();
            frontier.AddUrl(url);

            // Prepare crawler and start
            Crawler spider = new Crawler(frontier);
            //spider.StartCrawling();


            // fun1
            /*
            requestfun fun = new requestfun();
            var data = await fun.GetData(url);

            Console.WriteLine(data);
            */


            
            // fun2
            /*
            requestfun2 randoService = new requestfun2(url, maxConcurrentRequests: 4);

            for (int i = 0; i < 15; i++)
            {
                Task.Run(async () =>
                {
                    Console.WriteLine($"Requesting random number ");
                    Console.WriteLine(await randoService.GetRandomNumber());
                });
            }
            */


            // fun 3
            /*
            requestfun3 fun = new requestfun3();
            await fun.testAsync();
            */

            // fun 4
            /*
            requestfun4 fun = new requestfun4();
            fun.SendAsync();
            */
        }
    }
}
