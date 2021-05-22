using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace TT.FileParserFunction
{
    public class FileMonitorFunction
    {
        private FileMonitorLogic _fileMonitor;

        public FileMonitorFunction(FileMonitorLogic fileMonitor)
        {
            _fileMonitor = fileMonitor;
        }

        [FunctionName("FileMonitorFunction")]
        public async Task Run([TimerTrigger("%FileMonitorTrigger%")]TimerInfo myTimer, ILogger log)
        {
            await _fileMonitor.ProcessFiles();
        }





    }
}