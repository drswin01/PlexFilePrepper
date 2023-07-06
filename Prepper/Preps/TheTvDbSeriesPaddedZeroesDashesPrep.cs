using Prepper.Abstractions;
using System;
using System.IO;
using Serilog;

namespace Prepper.Preps
{
    public class TheTvDbSeriesNoSpacesPaddedZeroesDashesPrep : IPrep
    {
        private string correctedFileName;
        private bool isComplete;
        public string FileName { get; set; }
        public IEpisode Episode { get; set; }

        public TheTvDbSeriesNoSpacesPaddedZeroesDashesPrep(string fileName, IEpisode episode)
        {
            FileName = fileName;
            Episode = episode;
            isComplete = false;
        }

        public string GetCorrectFileName()
        {
            if (string.IsNullOrWhiteSpace(correctedFileName))
            {
                string? directory = null;
                string? extension = null;
                if (FileName != null)
                {
                    directory = Path.GetDirectoryName(FileName);
                    extension = Path.GetExtension(FileName);
                }
                string SeasonNumberPadded = Episode.Season > 9 ? Episode.Season.ToString() : $"0{Episode.Season}";
                string EpisodeNumberPadded = Episode.EpisodeNumber > 9 ? Episode.EpisodeNumber.ToString() : $"0{Episode.EpisodeNumber}";
                string replaceCharacters = "*.\"/\\" + "[]:;|,";
                string seriesTitleFiltered = string.Concat(Episode.SeriesName.Split(Path.GetInvalidFileNameChars()));
                string episodeTitleFiltered = string.Concat(Episode.EpisodeTitle.Split(Path.GetInvalidFileNameChars()));
                foreach (char c in replaceCharacters)
                {
                    seriesTitleFiltered = seriesTitleFiltered.Replace(c.ToString(), string.Empty);
                }
                correctedFileName = Path.Combine(directory!, $"{seriesTitleFiltered}-S{SeasonNumberPadded}E{EpisodeNumberPadded}-{episodeTitleFiltered}{extension}");
            }
            return correctedFileName;
        }

        public string GetCorrectFileNameWithoutPathOrExtension()
        {
            if (string.IsNullOrWhiteSpace(correctedFileName))
            {
                string SeasonNumberPadded = Episode.Season > 9 ? Episode.Season.ToString() : $"0{Episode.Season}";
                string EpisodeNumberPadded = Episode.EpisodeNumber > 9 ? Episode.EpisodeNumber.ToString() : $"0{Episode.EpisodeNumber}";
                string replaceCharacters = "*.\"/\\" + "[]:;|,'";
                string seriesTitleFiltered = string.Concat(Episode.SeriesName.Split(Path.GetInvalidFileNameChars()));
                string episodeTitleFiltered = string.Concat(Episode.EpisodeTitle.Split(Path.GetInvalidFileNameChars()));
                foreach (char c in replaceCharacters)
                {
                    seriesTitleFiltered = seriesTitleFiltered.Replace(c.ToString(), string.Empty);
                }
                correctedFileName = $"{seriesTitleFiltered}-S{SeasonNumberPadded}E{EpisodeNumberPadded}-{episodeTitleFiltered}";
            }
            return correctedFileName;
        }

        public bool IsCompleted() => isComplete;

        public bool DoPrep()
        {
            try
            {
                File.Move(FileName, GetCorrectFileName());
                isComplete = true;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error when trying to move file", ex);
                isComplete = false;
                return false;
            }
        }

        public override string ToString()
        {
            return $"Renaming {FileName} to {GetCorrectFileName()}";
        }

        public string GetCorrectFileNameWithoutPathExtensionOrEpisodeName()
        {
            if (string.IsNullOrWhiteSpace(correctedFileName))
            {
                string SeasonNumberPadded = Episode.Season > 9 ? Episode.Season.ToString() : $"0{Episode.Season}";
                string EpisodeNumberPadded = Episode.EpisodeNumber > 9 ? Episode.EpisodeNumber.ToString() : $"0{Episode.EpisodeNumber}";
                string replaceCharacters = "*.\"/\\" + "[]:;|,'";
                string seriesTitleFiltered = string.Concat(Episode.SeriesName.Split(Path.GetInvalidFileNameChars()));
                foreach (char c in replaceCharacters)
                {
                    seriesTitleFiltered = seriesTitleFiltered.Replace(c.ToString(), string.Empty);
                }
                correctedFileName = $"{seriesTitleFiltered}-S{SeasonNumberPadded}E{EpisodeNumberPadded}";
            }
            return correctedFileName;
        }
    }
}
