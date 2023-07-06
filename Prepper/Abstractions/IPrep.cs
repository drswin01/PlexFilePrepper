
namespace Prepper.Abstractions
{
    public interface IPrep
    {
        string GetCorrectFileName();
        string GetCorrectFileNameWithoutPathOrExtension();
        string GetCorrectFileNameWithoutPathExtensionOrEpisodeName();
        bool DoPrep();
        bool IsCompleted();
    }
}
