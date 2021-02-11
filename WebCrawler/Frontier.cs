using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace WebCrawler
{
    public class Frontier
    {
        private Hashtable _allUrls = new Hashtable(); // All found urls (unordered, good lookup performance)
        private ConcurrentQueue<string> _frontierUrls = new ConcurrentQueue<string>(); // LIFO (breath first)

        public void AddUrl(string url)
        {
            // Add url to frontier queue if url is not already in allUrls
            if (!_allUrls.Contains(url))
            {
                _allUrls.Add(url, url);
                _frontierUrls.Enqueue(url);
            }
        }

        public void AddUrls(List<string> urls)
        {
            foreach (var url in urls)
            {
                AddUrl(url);
            }

            Console.WriteLine("Unique urls found: " + _allUrls.Count);
            Console.WriteLine("Unique urls visited: " + (_allUrls.Count - _frontierUrls.Count));
        }

        public string NextUrl()
        {
            string nextUrl = null;
            if (_frontierUrls.Count > 0)
            {
                while(!_frontierUrls.TryDequeue(out nextUrl));
            }
            return nextUrl;
        }
    }
}