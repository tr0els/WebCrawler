using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler
{
    class requestfun3
    {
        public async Task testAsync()
        {
            var ips = await Dns.GetHostAddressesAsync("www.google.com");

            foreach (var ipAddress in ips)
            {
                Console.WriteLine(ipAddress.MapToIPv4().ToString());
            }

            var socketsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromSeconds(60),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(20),
                MaxConnectionsPerServer = 10
            };

            var client = new HttpClient(socketsHandler);

            var sw = Stopwatch.StartNew();

            Console.WriteLine("test");

            // maybe change the client.GetAsync to a method call that does more
            var tasks = Enumerable.Range(0, 100).Select(i => client.GetAsync("https://www.google.com"));

            Console.WriteLine("test2");

            await Task.WhenAll(tasks);


            sw.Stop();

            Console.WriteLine($"{sw.ElapsedMilliseconds}ms taken for 200 requests");

            Console.WriteLine("Press a key to exit...");
            Console.ReadKey();
        }
    }
}
