using System;
using System.IO;
using System.Text;
using LyricsCollector.Properties;
using Newtonsoft.Json;
using NTextCat;

namespace LyricsCollector
{
    public class LanguageDetector
    {
        private readonly RankedLanguageIdentifier _languageIdentifier;

        public LanguageDetector()
        {
            var xml = Resources.Core14_profile;
            var bytes = Encoding.UTF8.GetBytes(xml);
            var factory = new RankedLanguageIdentifierFactory();
            using (var s = new MemoryStream(bytes))
            {
                _languageIdentifier = factory.Load(s);
            }
        }

        public string GetLanguageId(string contents)
        {
            try
            {
                var result = _languageIdentifier.Identify(contents);

                LanguageInfo bestFitLangInfo = null;
                var minMismatch = double.MaxValue;
                foreach (var item in result)
                {
                    if (item.Item2 < minMismatch)
                    {
                        bestFitLangInfo = item.Item1;
                        minMismatch = item.Item2;
                    }
                }

                return bestFitLangInfo?.Iso639_3 ?? "eng";
            }
            catch (Exception e)
            {
                return "eng";
            }
        }

        public string GetLanguageIdFromSongFile(string inputFile)
        {
            try
            {
                var serializer = new JsonSerializer();
                using (var streamReader = new StreamReader(inputFile, Encoding.UTF8))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    var song = serializer.Deserialize<Song>(jsonTextReader);
                    var result = _languageIdentifier.Identify(song.Lyrics);

                    LanguageInfo bestFitLangInfo = null;
                    var minMismatch = double.MaxValue;
                    foreach (var item in result)
                    {
                        if (item.Item2 < minMismatch)
                        {
                            bestFitLangInfo = item.Item1;
                            minMismatch = item.Item2;
                        }
                    }

                    return bestFitLangInfo?.Iso639_3 ?? "eng";
                }
            }
            catch (Exception e)
            {
                return "eng";
            }
        }
    }
}