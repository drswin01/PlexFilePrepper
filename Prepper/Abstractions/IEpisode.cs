using System;

namespace Prepper.Abstractions
{
    public interface IEpisode
    {
        string SeriesName { get; }
        int Season { get; }
        int EpisodeNumber { get; }
        string EpisodeTitle { get; }
        DateTime PublishDate { get; }
    }
}
