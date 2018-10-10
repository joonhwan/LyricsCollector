using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Abot.Crawler;
using Abot.Poco;
using AbotWebCrawlingTestApp;
using Newtonsoft.Json;

namespace LyricsCollector
{
    public class SongScraper
    {
        private EasyWebCrawler _crawler;
        private HashSet<string> _crawledIds;
        private List<Song> _songs;
        private PathStringCorrector _pathCorrector;
        private string _artistNameHint;

        public SongScraper()
        {
            var config = new CrawlConfiguration();
            config.MaxConcurrentThreads = 1;
            config.MaxCrawlDepth = 1;
            config.IsExternalPageCrawlingEnabled = true;
            config.IsExternalPageLinksCrawlingEnabled = true;
            _crawler = new EasyWebCrawler(config);
            _crawledIds = new HashSet<string>();
            _songs = new List<Song>();
            _pathCorrector = new PathStringCorrector();

            _crawler.ShouldCrawlPageLinks((page, context) =>
            {
                var query = page.Uri.Query;
                var allow = !query.StartsWith("?id=");
                //Console.WriteLine("CrawlLink? : [{0}], {1} -> {2} ", allow ? "O" : "X", page.ParentUri, page.Uri);
                return new CrawlDecision() { Allow = allow };
            });
            _crawler.ShouldCrawlPage((page, context) =>
            {
                var query = page.Uri.Query;
                var parsedQuery = HttpUtility.ParseQueryString(query);
                var id = parsedQuery.Get("id");
                var searchArtist = parsedQuery.Get("searchartist") == "1";
                
                var allow = false;
                if (id != null)
                {
                    allow = _crawledIds.Add(id);
                }
                else if (searchArtist)
                {
                    allow = true;
                }
                //Console.WriteLine("CrawlPage? : [{0}], {2} ", allow ? "O" : "X", page.ParentUri, page.Uri);
                return new CrawlDecision() { Allow = allow };
            });
            _crawler.PageCrawlCompleted += CrawlerOnPageCrawlCompleted;
        }

        public string OutDirectoryBasePrefix { get; set; } = "lyrics";

        private void CrawlerOnPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            var parsedQuery = HttpUtility.ParseQueryString(e.CrawledPage.Uri.Query);
            var id = parsedQuery.Get("id");
            if (id != null)
            {
                var html = e.CrawledPage.AngleSharpHtmlDocument;
                var headerElements = html.QuerySelectorAll("table table table tbody>tr>td>font>a");
                var headers = headerElements.Select(element => element.TextContent).ToList();
                if (headers.Count == 2)
                {
                    var artist = headers[1];
                    var title = headers[0];

                    //var artist = html.QuerySelector("table table table>tbody>tr>td>font>a")?.TextContent;
                    var lines = html.QuerySelectorAll("table.tabletext>tbody>tr>td").Select(element => element.TextContent).ToList();
                    if (lines.Count > 2)
                    {
                        artist = lines[lines.Count - 1];
                        title = lines[lines.Count - 2];
                        lines = lines.Take(lines.Count - 2).ToList();
                    }

                    if (artist.StartsWith(_artistNameHint))
                    {
                        var sb = new StringBuilder(1024);
                        var badLyrics = false;
                        foreach (var line in lines)
                        {
                            if(IsBadLyricsLines(line))
                            {
                                continue;
                            }
                            
                            sb.AppendLine(line);
                        }

                        var lyrics = sb.ToString();
                        _songs.Add(new Song()
                        {
                            Artist = artist,
                            Id = id,
                            Lyrics = lyrics,
                            Title = title,
                        });

                        //Console.WriteLine("completed {0}", e.CrawledPage.Uri);
                        Console.Write(".");
                    }
                    else
                    {
                        Console.Write("-");
                    }
                }
            }
        }
        
        private static readonly string[] _badWords = new string[]
        {
            "tweet",
            "---",
        };
        private bool IsBadLyricsLines(string trimmedLine)
        {
            return string.IsNullOrWhiteSpace(trimmedLine) || _badWords.Any(s => trimmedLine.Contains(s, StringComparison.InvariantCultureIgnoreCase));
        }

        public void Scrap(ArtistPage artist)
        {
            var outputDirPrefix = $"{OutDirectoryBasePrefix}/{artist.Page}";
            Scrap(artist.Name, artist.Url, outputDirPrefix);
        }

        public void Scrap(string artist, string url, string outputDirPrefix)
        {
            _artistNameHint = artist;
            var uri = new Uri(url);
            var result = _crawler.Crawl(uri);
            if (result.ErrorOccurred)
            {
                Console.WriteLine("Error : {0}", result.ErrorException.Message);
            }
            else
            {
                Console.WriteLine("done --> {0} ea", _songs.Count);
                var normedArtist = _pathCorrector.NormalizedFileName(artist);
                var directory = $"{outputDirPrefix}/{normedArtist}";
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var serializer = new JsonSerializer();

                foreach (var song in _songs)
                {
                    var filePath = _pathCorrector.GetNormalizedPath(directory, song.Title);
                    filePath = filePath + ".json";
                    using(var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    using (var textWriter = new StreamWriter(stream, Encoding.UTF8))
                    using (var jsonWriter = new JsonTextWriter(textWriter)
                    {
                        Formatting = Formatting.Indented
                    })
                    {
                        serializer.Serialize(jsonWriter, song);
                    }
                }
            }
        }
    }

    public class Song
    {
        public string Id { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Lyrics { get; set; }
    }
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }
    public class PathStringCorrector
    {
        private Regex _r;

        public PathStringCorrector()
        {
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            _r = new Regex($"[{Regex.Escape(regexSearch)}]");
        }
        
        public string NormalizedFileName(string candidate, string replaceCharacter = "_")
        {
            return _r.Replace(candidate, replaceCharacter);
        }

        public string GetNormalizedPath(string directoryPath, string nameCandidate)
        {
            var normalizedName = NormalizedFileName(nameCandidate);

            var index = 0;
            var filePathPrefix = Path.Combine(directoryPath, normalizedName);
            var filePath = filePathPrefix;
            while (File.Exists(filePath))
            {
                filePath = $"{filePathPrefix}_{index++}";
            }

            return filePath;
        }
    }
}