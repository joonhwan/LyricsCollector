using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace LyricsCollector
{
    public class GatherProcessor
    {
        private static readonly string[] LineSplits = new []{ Environment.NewLine };

        public GatherProcessor()
        {
            _languageDetector = new LanguageDetector();
        }

        public void Process(string inputDir, string outputFilePath)
        {
            var serializer = new JsonSerializer();
            using(var stream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream))
            {
                foreach (var inputFile in Directory.EnumerateFiles(inputDir, "*.json", SearchOption.AllDirectories))
                {
                    try
                    {
                        Console.WriteLine("Processing {0}", inputFile);

                        using (var reader = new StreamReader(inputFile, Encoding.UTF8))
                        using (var jsonReader = new JsonTextReader(reader))
                        {
                            var song = serializer.Deserialize<Song>(jsonReader);
                            
                            var lines = song.Lyrics.Split(LineSplits, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                var normedLine = NormalizeLyricsLine(line);
                                if (string.IsNullOrWhiteSpace(normedLine))
                                {
                                    continue;
                                }

                                var lang = _languageDetector.GetLanguageId(normedLine);
                                if (lang != "kor")
                                {
                                    continue;
                                }

                                writer.WriteLine(normedLine);
                                
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(" -- Error : {0}", e.Message);
                    }
                }
            }
        }

        private static readonly Regex ParenSurroundedTextPattern = new Regex("[<\\(\\[].*[\\)\\]>]");
        private static readonly string[] BadWords = new string[]
        {
            "verse", "chorus", "intro", "간주", "instrument", "@", "hook", "outro", "반복", "bridge", "브릿지", "브리지", "#", "*", "~~~~~~~~~~"
        };

        private LanguageDetector _languageDetector;

        private string NormalizeLyricsLine(string line)
        {
            var normedLine = ParenSurroundedTextPattern.Replace(line, "");

            if (BadWords.Any(s => normedLine.Contains(s, StringComparison.InvariantCultureIgnoreCase)))
            {
                normedLine = string.Empty;
            }

            return normedLine.Trim();
        }

    }
}