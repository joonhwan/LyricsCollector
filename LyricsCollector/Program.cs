using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using AbotWebCrawlingTestApp;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace LyricsCollector
{
    class Program
    {
        static void Main(string[] args)
        {
            DoMain(args);
            //TestBed();
        }


        private static void DoMain(string[] args)
        {
            var app = new CommandLineApplication();

            app.Name = "LyricsCollector";
            app.Description = "Korean Song Lyrics Collector - boom4u.net";
            app.HelpOption("-?|-h|--help");

            var versionInfo = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            app.VersionOption("-v|--version", () => $"Version {versionInfo}");

            // no command case
            app.OnExecute(() =>
            {
                //Console.WriteLine("Global Command : No Args Case...");
                app.ShowHelp();
                return 0;
            });

            app.Command("artist", artistApp =>
            {
                artistApp.Description = "Collect Artist List";
                artistApp.ExtendedHelpText = "Artist목록만 수집합니다";
                artistApp.HelpOption("-?|-h|--help");

                var outfilePath = artistApp.Argument("outJsonFilePath", "output json file path");
                artistApp.OnExecute(() =>
                {
                    var outputFilePath = outfilePath.Value ?? "artists.json";
                    Console.WriteLine("Output File Path : {0}", outputFilePath);

                    var scraper = new ArtistDataScraper();
                    if (scraper.Do())
                    {
                        Console.WriteLine("Collected {0} artists.", scraper.Result.Count);
                        scraper.SaveResult(outputFilePath);
                        return 0;
                    }

                    return -1;
                });
            });

            app.Command("lyrics", lyricsApp =>
            {
                lyricsApp.Description = "Collect Lyric List";
                lyricsApp.HelpOption("-?|-h|--help");

                var inputArtistFile = lyricsApp.Option("-i|--inputfile", "Input Artist Json File", CommandOptionType.SingleValue);
                var outputDirectoryOption = lyricsApp.Option("-o|--outdir", "Output Directory", CommandOptionType.SingleValue);
                lyricsApp.OnExecute(() =>
                {
                    List<ArtistPage> artists = null;
                    if (inputArtistFile.HasValue())
                    {
                        var jsonSerializer = new JsonSerializer();
                        using (var reader = new StreamReader("c:/artistpages.json", Encoding.UTF8))
                        using (var jsonTextReader = new JsonTextReader(reader))
                        {
                            artists = jsonSerializer.Deserialize<List<ArtistPage>>(jsonTextReader);
                        }

                        Console.WriteLine("input artist file containts {0} artist.", artists.Count);
                    }
                    else
                    {
                        Console.WriteLine("Collecting Artist List...");

                        var scraper = new ArtistDataScraper();
                        if (scraper.Do())
                        {
                            artists = scraper.Result;
                        }
                    }

                    if (artists == null || artists.Count == 0)
                    {
                        Console.WriteLine("Unable to get artist list!");
                        return -1;
                    }

                    var outDir = outputDirectoryOption.HasValue() ? outputDirectoryOption.Value() : Environment.CurrentDirectory;
                    Console.WriteLine("Output Dir : {0}", outDir);

                    {
                        var sn = 0;
                        var total = artists.Count;
                        foreach (var artist in artists)
                        {
                            var scraper = new SongScraper()
                            {
                                OutDirectoryBasePrefix = outDir,
                            };
                            Console.WriteLine("Processing {0}/{1} : {2}", ++sn, total, artist.Name);
                            scraper.Scrap(artist);
                        }
                    }

                    return 0;
                });
            });
            app.Command("gather", gatherApp =>
            {
                gatherApp.Description = "Gater Lyric into Single File";
                gatherApp.HelpOption("-?|-h|--help");

                var inputDirOption = gatherApp.Option("-i|--inputdir", "Input Directory", CommandOptionType.SingleValue);
                var outputFilePathOption = gatherApp.Option("-o|--outfile", "Output Directory", CommandOptionType.SingleValue);
                gatherApp.OnExecute(() =>
                {
                    if (!inputDirOption.HasValue())
                    {
                        Console.WriteLine("No Input Directory. specify it using --inputdir {directory} option.");
                        return -1;
                    }

                    var inputDir = inputDirOption.Value();

                    var outputFilePath = outputFilePathOption.HasValue() ? outputFilePathOption.Value() : "gathered-lyrics.txt";
                    Console.WriteLine("gathering all lyrics into {0}", outputFilePath);

                    var gatherProcessor = new GatherProcessor();
                    gatherProcessor.Process(inputDir, outputFilePath);

                    return 0;
                });
            });
            app.Command("detect", detectApp =>
            {
                detectApp.Description = "Detect Language of Lyrics Data";
                detectApp.HelpOption("-?|-h|--help");

                var inputFileArg = detectApp.Argument("inputLyricsJsonFilePath", "input lyrics json file path", true);
                
                detectApp.OnExecute(() =>
                {
                    var detector = new LanguageDetector();
                    foreach (var inputFile in ExpandFilePaths(inputFileArg.Values))
                    {
                        var result = detector.GetLanguageIdFromSongFile(inputFile);
                        Console.WriteLine("{0} : {1}", inputFile, result);
                    }
                    return 0;
                });
            });
            try
            {
                app.Execute(args);
            }
            catch (CommandParsingException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to execute app : {0}", e.Message);
            }
        }
        
        public static string[] ExpandFilePaths(IEnumerable<string> args)
        {
            var fileList = new List<string>();

            foreach (var arg in args)
            {
                var substitutedArg = System.Environment.ExpandEnvironmentVariables(arg);

                var dirPart = Path.GetDirectoryName(substitutedArg);
                if (dirPart.Length == 0)
                    dirPart = ".";

                var filePart = Path.GetFileName(substitutedArg);

                foreach (var filepath in Directory.GetFiles(dirPart, filePart))
                    fileList.Add(filepath);
            }

            return fileList.ToArray();
        }
        private static void TestBed()
        {
            //SimplestCrawling();
            //var scraper = new ArtistDataScraper();
            //scraper.Do();

            var scraper = new SongScraper();
            scraper.Scrap("샾 (shap#)", "http://boom4u.net/lyrics/?searchartist=1&keyword=%26%2349406%3B+%28shap%23%29", "c:/temp/lyrics");
            ////scraper.Scrap("98 Degrees(98°)", "http://boom4u.net/lyrics/?searchartist=1&keyword=98+Degrees%2898%A1%C6%29");

            //var inputFile = "c:/artistpages.json";
            //var outDirBasePrefix = "c:/temp/lyrics";

            //return;

            //List<ArtistPage> artistPages = null;
            //var jsonSerializer = new JsonSerializer();
            //using (var reader = new StreamReader("c:/artistpages.json", Encoding.UTF8))
            //using (var jsonTextReader = new JsonTextReader(reader))
            //{
            //    artistPages = jsonSerializer.Deserialize<List<ArtistPage>>(jsonTextReader);
            //}
        }

        private static void SimplestCrawling()
        {
            //Create web request from URL array
            WebRequest request = WebRequest.Create("http://www.boom4u.net/lyrics/view.php?id=10H382E487DB035");

            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;

            // Get the response.
            HttpWebResponse response = (HttpWebResponse) request.GetResponse();

            //Display Status
            Console.WriteLine(response.StatusDescription);


            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();


            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream, Encoding.GetEncoding("euc-kr"));

            // Read the content.
            string responseFromServer = reader.ReadToEnd();

            // Display the content.
            Console.WriteLine(responseFromServer);

            // Cleanup the streams and the response.
            reader.Close();

            dataStream.Close();

            response.Close();

            Console.ReadKey();
        }
    }
}
