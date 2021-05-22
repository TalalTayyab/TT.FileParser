using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TT.FileParserFunction
{
    public class AzureFile : IFileFacade
    {
        private readonly ShareDirectoryClient _shareDirectoryClient;
        private readonly ShareClient _shareClient;
        private readonly int _waitSecondsUntilLastModified;

        private ShareFileClient FileClient => _shareDirectoryClient.GetFileClient(FileName);

        public string DirectoryName => _shareDirectoryClient.Name;
        public string FileName { get; }

        public AzureFile() { }

        public AzureFile(string fileName, ShareDirectoryClient shareDirectoryClient, ShareClient shareClient, IOptions<StorageOptions> storageOptions)
        {
            FileName = fileName;
            _shareDirectoryClient = shareDirectoryClient;
            _shareClient = shareClient;
            _waitSecondsUntilLastModified = storageOptions.Value.WaitSecondsUntilLastModified;
        }


        public async Task<bool> ChangeDirectory(string destinationDirectory)
        {
            var destDirectory = _shareClient.GetDirectoryClient(destinationDirectory);

            var destFile = destDirectory.GetFileClient(FileName);

            if (!await FileClient.ExistsAsync())
            {
                return false;
            }

            if (await destFile.ExistsAsync())
            {
                return false;
            }

            var response = await destFile.StartCopyAsync(FileClient.Uri);

            return response.Value.CopyStatus == CopyStatus.Success;
        }

        public async Task<bool> DeleteFile()
        {
            if (!await FileClient.ExistsAsync())
            {
                return false;
            }

            await FileClient.DeleteAsync();

            return true;
        }

        public async IAsyncEnumerable<string> GetFileLines()
        {
            //using (var stream = await FileClient.OpenReadAsync())
            //{
            //    using (var sr = new StreamReader(stream))
            //    {
            //        var line = string.Empty;
            //        while ((line = sr.ReadLine()) != null)
            //        {
            //            yield return line;
            //        }
            //    }
            //}

            using (var stream = await FileClient.OpenReadAsync())
            using (BufferedStream bs = new BufferedStream(stream))
            using (StreamReader sr = new StreamReader(bs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        public async Task<bool> IsFileReady()
        {
            var properties = await FileClient.GetPropertiesAsync();
            var diff = DateTime.UtcNow - properties.Value.LastModified.UtcDateTime;
            if (diff.TotalSeconds < _waitSecondsUntilLastModified) return false;
            return true;
        }
    }
}
