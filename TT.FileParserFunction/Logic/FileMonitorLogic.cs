using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace TT.FileParserFunction
{
    public class FileMonitorLogic
    {
        private readonly ILogger<FileMonitorLogic> _log;
        private readonly IMessageBus _messageBus;
        private readonly IStorageFacade _storageFacade;
        private readonly StorageOptions _storageOptions;


        public FileMonitorLogic(ILogger<FileMonitorLogic> log, IMessageBus messageBus, IOptions<StorageOptions> storageOptions, IStorageFacade storageFacade)
        {
            _log = log;
            _messageBus = messageBus;
            _storageFacade = storageFacade;
            _storageOptions = storageOptions.Value;
        }
        
        public async Task ProcessFiles()
        {
            var incomingDirectory = _storageFacade.GetDirectory(_storageOptions.IncomingDirectory);
            var processingDirectory = _storageFacade.GetDirectory(_storageOptions.ProcessingDirectory);

            await foreach (var file in incomingDirectory.GetFiles())
            {
                _log.LogInformation($"Found file {file.FileName}");

                if (!await file.IsReady())
                {
                    continue;
                }

                if (!await file.ChangeDirectory(_storageOptions.ProcessingDirectory))
                {
                    _log.LogError($"Unable to copy file from {_storageOptions.IncomingDirectory}/{file.FileName} to {_storageOptions.ProcessingDirectory}");
                     continue;
                }

                var fileInfo = new FileInfo
                {
                    DirectoryName = _storageOptions.ProcessingDirectory,
                    FileName = file.FileName,
                    ShareName = _storageOptions.ShareName
                };

                if (!await _messageBus.SendMessage(fileInfo))
                {
                    await processingDirectory.DeleteFile(fileInfo.FileName);
                    _log.LogError($"Unable to send message for file {file.FileName}.");
                }
                else
                {
                    await file.Delete();
                }
            }
        }
    }
}
