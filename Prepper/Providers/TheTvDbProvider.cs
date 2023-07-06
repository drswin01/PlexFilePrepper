using Prepper.Abstractions;
using Prepper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TvDbSharper;

namespace Prepper.Providers
{
    public class TheTvDbProvider : IReferenceProvider
    {
        private const string ProviderName = "TheTvDb";
        private readonly TvDbClient client;
        private string currentMediaType;
        private TvDbApiResponse<SearchResultDto[]> latestSeriesReponse;
        private SearchResultDto currentSeries;
        private readonly List<string> latestResponseNames;

        private static TheTvDbProvider instance;

        public static TheTvDbProvider GetProvider(string apikey, string pin)
        {
            instance ??= new TheTvDbProvider(apikey, pin);
            return instance;
        }

        private TheTvDbProvider(string apikey, string pin)
        {
            client = new TvDbClient();
            Task authTask = client.Login(apikey, pin);
            authTask.GetAwaiter().GetResult(); ;
            if (string.IsNullOrWhiteSpace(client.AuthToken)) throw new TvDbServerException("Unable to authenticate to TheTvDb API", 401);
            latestResponseNames = new List<string>();
        }

        public string GetProviderName()
        {
            return ProviderName;
        }

        public bool DoesProviderNameMatch(string name)
        {
            return ProviderName.Equals(name);
        }

        public List<string> GetMediaTypes()
        {
            return new List<string>
            {
                "Series"
            };
        }

        public List<string> GetSearchResults()
        {
            return latestResponseNames;
        }

        public void PerformTitleSearch(string searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString)) return;
            switch (currentMediaType)
            {
                case "Series":
                    SearchOptionalParams options = new()
                    {
                        Type = "series",
                        Query = searchString
                    };
                    latestSeriesReponse = client.Search(options).GetAwaiter().GetResult();
                    latestResponseNames.Clear();
                    latestResponseNames.AddRange(latestSeriesReponse.Data.Select(d => d.Name).ToList<string>());
                    break;
                default: return;
            }
        }

        public List<IEpisode>? GetEpisodesForTitle(string? title = null, int page = 1)
        {
            if (title == null && currentSeries == null) return null;
            SearchResultDto? series;
            if (title == null && currentSeries != null)
            {
                series = currentSeries;
            }
            else if (title == null) return null;
            else
            {
                series = null;
                foreach (SearchResultDto seriesResult in latestSeriesReponse.Data)
                {
                    if (seriesResult.Name.Equals(title))
                    {
                        series = seriesResult;
                    }
                }
            }
            if (series == null) return null;
            if (!int.TryParse(series.TvdbId, out int seriesId)) throw new Exception($"Unable to parse series ID of {series.TvdbId}");
            var episodesResults = client.SeriesEpisodes(seriesId, "default").GetAwaiter().GetResult();
            List<IEpisode>? episodes = new();
            foreach (EpisodeBaseRecordDto e in episodesResults.Data.Episodes)
            {
                episodes.Add(new TvDbEpisode(series.Name, e));
            }
            currentSeries = series;
            return new List<IEpisode>(episodes);
        }

        public void SetMediaType(string mediaTypeName)
        {
            currentMediaType = mediaTypeName;
        }
    }
}
