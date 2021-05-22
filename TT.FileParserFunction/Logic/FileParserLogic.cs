using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace TT.FileParserFunction
{
    public class FileParserLogic
    {
        private readonly IStorageFacade _storageFacade;
        private readonly ILogger<FileParserLogic> _log;
        private string _matchPattern;
        private string _completedDirectory;

        public FileParserLogic(ILogger<FileParserLogic> log, IStorageFacade storageFacade, IOptions<StorageOptions> storageOptions, IConfiguration configuration)
        {
            _log = log;
            _storageFacade = storageFacade;
            _matchPattern = configuration.GetValue<string>("MatchPattern");
            _completedDirectory = storageOptions.Value.CompletedDirectory;
        }

        public async Task Parse(FileInfo fileInfo)
        {
            var parser = new ParseLine();

            var file = _storageFacade.GetDirectory(fileInfo.DirectoryName).GetFile(fileInfo.FileName);

            await foreach (var line in file.GetFileLines())
            {
                _log.LogTrace(line);

                if (parser.IsMatch(line, _matchPattern))
                {
                    _log.LogInformation($"Found pattern {_matchPattern} in {line}. Moving file to Completed");
                    await file.ChangeDirectory(_completedDirectory);
                    break;
                }
            }

            await file.DeleteFile();
        }
    }
}
