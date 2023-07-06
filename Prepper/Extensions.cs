using FuzzySharp;
using FuzzySharp.Extractor;
using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using Prepper.Abstractions;
using Prepper.Models;
using Prepper.Preps;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Prepper
{
    internal static class Extensions
    {
        /// <summary>
        /// Attempts to assign each file to a corresponding episode based on its potential prepped name
        /// </summary>
        /// <param name="fileList">The files to reorder. It is assumed this list is not in any particular order when passed in.</param>
        /// <param name="prep">Used to get the potential corrected filename for each episode.</param>
        /// <param name="episodeNamesToMatch">The episodes to match the files with. This collection is assumed to ordered and will not change it's order.</param>
        public static List<string> AutoReorderFileListMatchingEpisodes(this List<string> fileList, List<IEpisode> episodeNamesToMatch, bool usePaddedZeroes = true, bool useDashes = true, int isPositiveMatchThreshold = 70, int maxPasses = 100) 
        {
            List<FilesWithSortedScoresAndSortFlag> filesWithScoresAndSortFlag = new();
            List<string> potentialEpisodeFileNames = new();
            Dictionary<string, string> potentialEpisodeFileNamesNoEpisodeTitleMapped = new();
            episodeNamesToMatch.ForEach(e => 
            {
                var prep = PrepFactory.CreatePrep(usePaddedZeroes, useDashes, string.Empty, e);
                var correctedNameFull = prep.GetCorrectFileNameWithoutPathOrExtension();
                potentialEpisodeFileNames.Add(Path.GetFileNameWithoutExtension(correctedNameFull));
                potentialEpisodeFileNamesNoEpisodeTitleMapped.Add(Path.GetFileNameWithoutExtension(prep.GetCorrectFileNameWithoutPathExtensionOrEpisodeName()), correctedNameFull);
            });

            //setup file list and get sorted scores for all files
            foreach (var file in fileList)
            {
                //Get ratio scores and intialization scores
                var fileName = Path.GetFileNameWithoutExtension(file);
                var ratioScores = new List<ExtractedResult<string>>(Process.ExtractSorted(fileName, potentialEpisodeFileNames));
                var initializedScores = new List<ExtractedResult<string>>(Process.ExtractSorted(fileName, potentialEpisodeFileNames, s => s, scorer: ScorerCache.Get<TokenInitialismScorer>()));
                var ratioNoEpisodeTitleScores = new List<ExtractedResult<string>>(Process.ExtractSorted(fileName, potentialEpisodeFileNamesNoEpisodeTitleMapped.Keys));
                var initializedNoEpisodeTitleScores = new List<ExtractedResult<string>>(Process.ExtractSorted(fileName, potentialEpisodeFileNamesNoEpisodeTitleMapped.Keys, s => s, scorer: ScorerCache.Get<TokenInitialismScorer>()));
                List<ExtractedResult<string>> finalScores = new();
                //For each scored result take the higher score, or if the same use the first compared score
                for (int i = 0; i < potentialEpisodeFileNames.Count; i++)
                {
                    List<ExtractedResult<string>> scores = new()
                    {
                        ratioScores[i],
                        initializedScores[i],
                        ratioNoEpisodeTitleScores[i],
                        initializedNoEpisodeTitleScores[i]
                    };

                    var maxScore = scores.First(s => s.Score.Equals(scores.Max(s => s.Score)));
                    if (potentialEpisodeFileNamesNoEpisodeTitleMapped.Keys.Any(p => p == maxScore.Value))
                    {
                        maxScore = new ExtractedResult<string>(potentialEpisodeFileNamesNoEpisodeTitleMapped[maxScore.Value], maxScore.Score, maxScore.Index);
                    }
                    finalScores.Add(maxScore);
                }                    
                filesWithScoresAndSortFlag.Add(new(file, finalScores, false));
            }
            Log.Debug("Before Auto-Sort:/n", filesWithScoresAndSortFlag);
            for (var i = 0; i < maxPasses; i++)
            {
                FilesWithSortedScoresAndSortFlag? firstUnsortedFileFound = filesWithScoresAndSortFlag.FirstOrDefault(e => !e.IsSorted);
                //If there are no more files that need sorting then we can stop early
                if (firstUnsortedFileFound == null)
                {
                    Log.Debug($"Ended auto reorder early after {i + 1} passes.");
                    break;
                }

                //We can't sort if there isn't anything in sorted scores.
                var topMatch = firstUnsortedFileFound.SortedScores.FirstOrDefault();
                if (topMatch == null)
                {
                    firstUnsortedFileFound.IsSorted = true;
                    continue;
                }

                //Is the top match didn't score above the threshold score then it isn't a positive match and will be skipped for ordering.
                var topMatchScore = topMatch.Score;
                if (topMatch.Score < isPositiveMatchThreshold)
                {
                    firstUnsortedFileFound.IsSorted = true;
                    continue;
                }

                //check if any other files have the same top match and the same score
                var exactMatchCompetitors = filesWithScoresAndSortFlag.Where(fss => fss.File != firstUnsortedFileFound.File && fss.SortedScores.First().Value.Equals(topMatch.Value) && fss.SortedScores.First().Score == topMatch.Score).ToList();
                if (exactMatchCompetitors?.Count > 0)
                {
                    if (exactMatchCompetitors.Any(c => c.IsSorted))
                    {
                        //remove the topMatch and continue;
                        firstUnsortedFileFound.SortedScores.Remove(topMatch);
                        continue;
                    }
                }

                //check if any other files have the same top match and a higher score
                var higherTopMatchCompetitors = filesWithScoresAndSortFlag.Where(fss => fss.File != firstUnsortedFileFound.File && fss.SortedScores.First().Value.Equals(topMatch.Value) && fss.SortedScores.First().Score > topMatch.Score).ToList();
                if (higherTopMatchCompetitors?.Count > 0)
                {
                    //remove the topMatch and continue;
                    firstUnsortedFileFound.SortedScores.Remove(topMatch);
                    continue;
                }
                else
                {
                    //Assign the file to the top match's index and mark it sorted
                    _ = filesWithScoresAndSortFlag.Remove(firstUnsortedFileFound);
                    firstUnsortedFileFound.IsSorted = true;
                    filesWithScoresAndSortFlag.Insert(topMatch.Index, firstUnsortedFileFound);
                    continue;
                }
            }
            Serilog.Log.Debug("Auto-Sort results:");
            foreach (var fss in filesWithScoresAndSortFlag)
            {
                Log.Debug($"File: {fss.File}");
                fss.SortedScores.ForEach(s => Log.Debug($"Value: {s.Value} | Score: {s.Score} | Index: {s.Index}"));
                Log.Debug($"IsSorted: {fss.IsSorted}/n");
            }
           
            return filesWithScoresAndSortFlag.Select(f => f.File).ToList();
        }
    }
}
