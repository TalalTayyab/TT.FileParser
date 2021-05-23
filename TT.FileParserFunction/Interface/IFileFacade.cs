using System.Collections.Generic;
using System.Threading.Tasks;

namespace TT.FileParserFunction
{
    public interface IFileFacade
    {
        string DirectoryName { get; }
        string FileName { get; }

        Task<bool> ChangeDirectory(string destinationDirectory);
        Task<bool> Delete();
        IAsyncEnumerable<string> GetLines();

        Task<bool> IsReady();

        Task<bool> Exists();
    }
}