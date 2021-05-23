using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TT.FileParserFunction
{
    public class AzureDirectory : IDirectoryFacade
    {
        private readonly ShareClient _shareClient;
        private ShareDirectoryClient _shareDirectoryClient;
        private IOptions<StorageOptions> _storageOptions;

        public AzureDirectory(ShareDirectoryClient shareDirectoryClient, ShareClient shareClient, IOptions<StorageOptions> storageOptions)
        {
            _shareDirectoryClient = shareDirectoryClient;
            _shareClient = shareClient;
            _storageOptions = storageOptions;
        }

        public async IAsyncEnumerable<IFileFacade> GetFiles()
        {
            AsyncPageable<ShareFileItem> items = _shareDirectoryClient.GetFilesAndDirectoriesAsync();

            await foreach (var item in items)
            {
                if (item.IsDirectory)
                {
                    continue;
                }

                yield return new AzureFile(item.Name, _shareDirectoryClient, _shareClient, _storageOptions);
            }
        }

        public async Task<bool> DeleteFile(string fileName)
        {
            if (!await _shareDirectoryClient.ExistsAsync())
                return false;

            var file = GetFile(fileName);

            return await file.Delete();
        }

        public async Task CreateIfNotExists()
        {
            if (await _shareDirectoryClient.ExistsAsync())
                return;

            var result = await _shareClient.CreateDirectoryAsync(_shareDirectoryClient.Name);

            _shareDirectoryClient = result.Value;
        }


        public IFileFacade GetFile(string fileName)
        {
            return new AzureFile(fileName, _shareDirectoryClient, _shareClient, _storageOptions);
        }




    }
}
