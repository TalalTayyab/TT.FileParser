using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Options;

namespace TT.FileParserFunction
{
    public class AzureFileStorage : IStorageFacade
    {
        private readonly ShareClient _shareClient;
        private readonly IOptions<StorageOptions> _storageOptions;

        public AzureFileStorage(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
            _shareClient = new ShareClient(storageOptions.Value.ConnectionString, storageOptions.Value.ShareName);
            
        }

        public IDirectoryFacade GetDirectory(string directoryName)
        {
            return new AzureDirectory(_shareClient.GetDirectoryClient(directoryName), _shareClient, _storageOptions);
        }


    }
}
