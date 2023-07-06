using Prepper.Abstractions;
using System;
using System.Runtime.Serialization;
using TvDbSharper;

namespace Prepper.Models
{
    class TvDbEpisode : IEpisode, ISerializable
    {
        private readonly EpisodeBaseRecordDto episode;

        public TvDbEpisode(string seriesName, EpisodeBaseRecordDto episodeRecord)
        {
            SeriesName = seriesName;
            episode = episodeRecord;
        }

        public string SeriesName { get; }

        public int Season => episode.SeasonNumber;

        public int EpisodeNumber => episode.Number;

        public string EpisodeTitle => episode.Name;

        public DateTime PublishDate => DateTime.Parse(episode.Aired);

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            info.AddValue("EpisodeBaseRecordDto", episode);
            info.AddValue("SeriesName", SeriesName);
            info.AddValue("Season", Season);
            info.AddValue("EpisodeNumber", EpisodeNumber);
            info.AddValue("EpisodeTitle", EpisodeTitle);
            info.AddValue("PublishDate", PublishDate);
        }

        public override string ToString()
        {
            return $"S{Season}E{EpisodeNumber} - {EpisodeTitle}";
        }
    }
}
