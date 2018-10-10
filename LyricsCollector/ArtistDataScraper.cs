using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using Abot.Crawler;
using Abot.Poco;
using Newtonsoft.Json;

namespace LyricsCollector
{
    public class ArtistDataScraper
    {
        private readonly string _startUrl = "http://www.boom4u.net/lyrics/artist.php?page=1";
        private EasyWebCrawler _crawler;
        private HashSet<int> _crawedPages;
        private List<ArtistPage> _artistPages;
        
        public ArtistDataScraper()
        {
            var config = new CrawlConfiguration();
            config.MaxConcurrentThreads = 1;
            config.IsExternalPageCrawlingEnabled = true;
            config.IsExternalPageLinksCrawlingEnabled = true;
            _crawler = new EasyWebCrawler(config);
            _crawedPages = new HashSet<int>();
            _artistPages = new List<ArtistPage>(1000);
            
            _crawler.ShouldCrawlPageLinks((page, context) =>
            {
                var query = page.Uri.Query;
                var allow = query.StartsWith("?page=");

                //Console.WriteLine("CrawlLink? : [{0}], {1} -> {2} ", allow ? "O" : "X", page.ParentUri, page.Uri);
                return new CrawlDecision() { Allow = allow };
            });
            _crawler.ShouldCrawlPage((page, context) =>
            {
                var query = page.Uri.Query;
                var parsedQuery = HttpUtility.ParseQueryString(query);
                var pageNumberString = parsedQuery.Get("page");
                var allow = false;
                int pageNumber = -1;
                if (pageNumberString != null)
                {
                    if (int.TryParse(pageNumberString, out pageNumber))
                    {
                        allow = _crawedPages.Add(pageNumber);
                    }
                    else
                    {
                        pageNumber = -1;
                    }
                }
                //Console.WriteLine("CrawlPage? : [{0}], {2} ", allow ? "O" : "X", page.ParentUri, page.Uri);
                return new CrawlDecision() { Allow = allow };
            });
            _crawler.PageCrawlCompleted += CrawlerOnPageCrawlCompleted;
        }

        public List<ArtistPage> Result => _artistPages;

        public bool Do()
        {
            var result = _crawler.Crawl(new Uri(_startUrl));
            //Console.WriteLine("Crawling Error Occurred ? -> {0}", result.ErrorOccurred);

            if (!result.ErrorOccurred)
            {
                Console.WriteLine("Successfully Collected ArtistName/Page Data");
                return true;
            }
            else
            {
                Console.WriteLine("Error : {0}", result.ErrorException.Message);
                return false;
            }
        }

        public void SaveResult(string filePath)
        {
            var serializer = new JsonSerializer();
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var textWriter = new StreamWriter(stream, Encoding.UTF8))
            using (var jsonWriter = new JsonTextWriter(textWriter)
            {
                Formatting = Formatting.Indented
            })
            {
                serializer.Serialize(jsonWriter, _artistPages);
            }
        }
        
        private void CrawlerOnPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            var html = e.CrawledPage.AngleSharpHtmlDocument;
            var page = -1;
            var pageString = HttpUtility.ParseQueryString(e.CrawledPage.Uri.Query).Get("page");
            if (pageString != null && !int.TryParse(pageString, out page))
            {
                page = -1;
            }

            if (page < 0) return;

            var artistCells = html.QuerySelectorAll("table>tbody>tr>td>table>tbody>tr>td>a");
            //            var artistNames = artistCells.Select(element => element.TextContent);
            
            foreach (var artistCell in artistCells)
            {
                var url = artistCell.GetAttribute("href");
                _artistPages.Add(new ArtistPage()
                {
                    Name = artistCell.TextContent,
                    Url = url,
                    Page = page,
                });
                //Console.WriteLine("   {0} : {1}", artistCell.TextContent, href);
            }
            
            Console.WriteLine("completed [{0}]: {1}", _artistPages.Count, e.CrawledPage.Uri);

        }
    }

    public class ArtistPage
    {
        public int Page { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }
}