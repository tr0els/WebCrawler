using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebCrawler
{
    public class Requestfun
    {
        private readonly HttpClient HttpClient;
        private readonly string GetRandomNumberUrl;
        private SemaphoreSlim semaphore;
        private long circuitStatus;
        private const long OPEN = 0;
        private const long TRIPPED = 1;
        public string UNAVAILABLE = "Unavailable";

        public Requestfun(string url, int maxConcurrentRequests)
        {


            GetRandomNumberUrl = url;

            HttpClient = new HttpClient();
            SetMaxConcurrency(url, maxConcurrentRequests);
            semaphore = new SemaphoreSlim(maxConcurrentRequests);

            circuitStatus = OPEN;
        }

        private void SetMaxConcurrency(string url, int maxConcurrentRequests)
        {
            ServicePointManager.FindServicePoint(new Uri(url)).ConnectionLimit = maxConcurrentRequests;
        }

        public void OpenCircuit()
        {
            if (Interlocked.CompareExchange(ref circuitStatus, OPEN, TRIPPED) == TRIPPED)
            {
                Console.WriteLine("Opened circuit");
            }
        }
        private void TripCircuit(string reason)
        {
            if (Interlocked.CompareExchange(ref circuitStatus, TRIPPED, OPEN) == OPEN)
            {
                Console.WriteLine($"Tripping circuit because: {reason}");
            }
        }
        private bool IsTripped()
        {
            return Interlocked.Read(ref circuitStatus) == TRIPPED;
        }
        public async Task<string> GetRandomNumber()
        {
            try
            {
                await semaphore.WaitAsync();

                if (IsTripped())
                {
                    return UNAVAILABLE;
                }

                var response = await HttpClient.GetAsync(GetRandomNumberUrl);
			
                if(response.StatusCode != HttpStatusCode.OK)
                {
                    TripCircuit(reason: $"Status not OK. Status={response.StatusCode}");
                    return UNAVAILABLE;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch(Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
            {
                Console.WriteLine("Timed out");
                TripCircuit(reason: $"Timed out");
                return UNAVAILABLE;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
