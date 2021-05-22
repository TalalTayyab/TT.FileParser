using System.Collections.Generic;
using System.Threading.Tasks;

namespace TT.FileParserFunction
{
    public interface IDirectoryFacade
    {
        Task<bool> DeleteFile(string fileName);
        IFileFacade GetFile(string fileName);
        IAsyncEnumerable<IFileFacade> GetFiles();
        Task CreateIfNotExists();
    }
}