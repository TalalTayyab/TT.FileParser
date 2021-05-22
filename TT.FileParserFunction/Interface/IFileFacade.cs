using System.Collections.Generic;
using System.Threading.Tasks;

namespace TT.FileParserFunction
{
    public interface IFileFacade
    {
        string DirectoryName { get; }
        string FileName { get; }

        Task<bool> ChangeDirectory(string destinationDirectory);
        Task<bool> DeleteFile();
        IAsyncEnumerable<string> GetFileLines();

        Task<bool> IsFileReady();
    }
}