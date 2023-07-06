using System.Collections.Generic;

namespace Prepper.Abstractions
{
    public interface IReferenceProvider
    {
        string GetProviderName();
        bool DoesProviderNameMatch(string name);
        List<string> GetMediaTypes();
        void SetMediaType(string mediaTypeName);
        void PerformTitleSearch(string searchString);
        List<string> GetSearchResults();
        List<IEpisode>? GetEpisodesForTitle(string? title = null, int page = 1);
    }
}
