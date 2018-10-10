using System;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Abot.Crawler;
using Abot.Poco;
using LyricsCollector;
using Robots.Model;

namespace AbotWebCrawlingTestApp
{
    public class AbotExample1
    {
        private EasyWebCrawler _crawler;

        public AbotExample1()
        {
            var crawlConfig = new CrawlConfiguration();
            _crawler = new EasyWebCrawler(crawlConfig);
            _crawler.CrawlBag.MyFoo1 = new Foo();
            _crawler.CrawlBag.MyFoo2 = new Foo();
            _crawler.PageCrawlStartingAsync += crawler_ProcessPageCrawlStarting;
            _crawler.PageCrawlCompletedAsync += crawler_ProcessPageCrawlCompleted;
            _crawler.PageCrawlDisallowedAsync += crawler_PageCrawlDisallowed;
            _crawler.PageLinksCrawlDisallowedAsync += crawler_PageLinksCrawlDisallowed;

            //_crawler.ShouldCrawlPage((crawl, context) =>
            //{
            //    var decision = new CrawlDecision()
            //    {
            //        Allow = false,
            //    };
            //    return decision;
            //});
            _crawler.ShouldDownloadPageContent((page, context) =>
            {
                var link = page.Uri;
                Console.WriteLine(" --> detected link : {0}", link);
                return new CrawlDecision() {Allow = false};
            });
        }

        public void Do()
        {
            var result = _crawler.Crawl(new Uri("http://www.boom4u.net/lyrics/view.php?id=10H382E487DB035")); //This is synchronous, it will not go to the next line until the crawl has completed

            if (result.ErrorOccurred)
                Console.WriteLine("Crawl of {0} completed with error: {1}", result.RootUri.AbsoluteUri, result.ErrorException.Message);
            else
                Console.WriteLine("Crawl of {0} completed without error.", result.RootUri.AbsoluteUri);
        }

        void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            var pageToCrawl = e.PageToCrawl;
            var context = e.CrawlContext;
            //context.CrawlBag.MyFoo1.Bar();
            e.PageToCrawl.PageBag.Bar = new Bar();
            
            Console.WriteLine("About to crawl link {0} which was found on page {1}", pageToCrawl.Uri.AbsoluteUri,   pageToCrawl.ParentUri.AbsoluteUri);
        }

        void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;

            if (crawledPage.WebException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
                Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
            else
                Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);

            if (string.IsNullOrEmpty(crawledPage.Content.Text))
                Console.WriteLine("Page had no content {0}", crawledPage.Uri.AbsoluteUri);
	
            var htmlAgilityPackDocument = crawledPage.HtmlDocument; //Html Agility Pack parser
            var angleSharpHtmlDocument = crawledPage.AngleSharpHtmlDocument; //AngleSharp parser
        }

        void crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;
            Console.WriteLine("Did not crawl the links on page {0} due to {1}", crawledPage.Uri.AbsoluteUri, e.DisallowedReason);
        }

        void crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine("Did not crawl page {0} due to {1}", pageToCrawl.Uri.AbsoluteUri, e.DisallowedReason);
        }
    }

    internal class Bar
    {
    }

    public class Foo
    {
    }
}