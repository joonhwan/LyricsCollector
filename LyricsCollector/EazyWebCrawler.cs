using Abot.Crawler;
using Abot.Poco;

namespace LyricsCollector
{
    public class EasyWebCrawler : PoliteWebCrawler
    {
        public EasyWebCrawler(CrawlConfiguration config)
            : base(config, null, null, null, null, null, null, null, null)
        {   
        }
    }
}