using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace TT.FileParserFunction
{
    public class FileParserFunction
    {
        private readonly FileParserLogic _fileParser;
        public FileParserFunction(FileParserLogic fileParser)
        {
            _fileParser = fileParser;
        }

        [FunctionName("FileParserFunction")]
        public async Task Run([ServiceBusTrigger("%ServiceBusQueue%", Connection = "ServiceBusConnection")]FileInfo fileInfo, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {fileInfo.Path}");
            await _fileParser.Parse(fileInfo);
        }
    }
}
