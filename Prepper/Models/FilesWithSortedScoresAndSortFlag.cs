using FuzzySharp.Extractor;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Prepper.Models
{
    internal class FilesWithSortedScoresAndSortFlag : ISerializable
    {
        public string File { get; set; }
        public List<ExtractedResult<string>> SortedScores { get; set; }
        public bool IsSorted { get; set; }

        public FilesWithSortedScoresAndSortFlag(string file, IEnumerable<ExtractedResult<string>> sortedScores, bool isSorted)
        {
            File = file;
            SortedScores = new(sortedScores);
            IsSorted = isSorted;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(File), File);
            info.AddValue(nameof(List<ExtractedResult<string>>), SortedScores);
            info.AddValue(nameof(IsSorted), IsSorted);
        }
    }
}
