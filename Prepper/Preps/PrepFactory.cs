using Prepper.Abstractions;
using Prepper.Models;
using System;
using System.CodeDom;

namespace Prepper.Preps
{
    public static class PrepFactory
    {
        public static IPrep CreatePrep(bool usePaddedZeroes, bool useDashes, string fileName, IEpisode episode)
        {
            Type episodeType = episode.GetType();            
            if (episodeType.Equals(typeof(TvDbEpisode)))
            {
                return CreateTvDbSeriesPrep(usePaddedZeroes, useDashes, fileName, episode);
            }
            else
            {
                throw new NotImplementedException($"The episode type {episodeType.Name} does not have a matching provider implementation");
            }
        }

        private static IPrep CreateTvDbSeriesPrep(bool usePaddedZeroes, bool useDashes, string fileName, IEpisode episode)
        {
            if (usePaddedZeroes)
            {
                if (useDashes)
                {
                    return new TheTvDbSeriesNoSpacesPaddedZeroesDashesPrep(fileName, episode);
                }
                else
                {
                    throw new NotImplementedException("Using underscores for prep is not currently available.");
                }
            }
            else
            {   //Will also have an if else for the dashes
                throw new NotImplementedException("Non padded prep is not currently available.");
            }
        }
    }
}
