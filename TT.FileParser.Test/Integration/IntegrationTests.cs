using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using TT.FileParserFunction;

namespace TT.FileParser.Test.Integration
{
    public class IntegrationTests : BaseIntegrationTests
    {
        [Test]
        public async Task GivenAMatchedFileWhenDroppedThenIsMoved()
        {
            // Arrange
            var fileMonitor = new FileMonitorLogic(LogFileMon.Object, MessageBus.Object, Options, Storage.Object);
            var fileParser = new FileParserLogic(LogFileParser.Object, Storage.Object, Options, Configuration);

            // Act
            await fileMonitor.ProcessFiles();
            await fileParser.Parse(FileInfo);

            //  Assert
            FileIncoming.Verify(c => c.Delete(), Times.Once);
            MessageBus.Verify(c => c.SendMessage(It.IsAny<FileInfo>()), Times.Once);

            FileProcessing.Verify(c => c.ChangeDirectory(Options.Value.CompletedDirectory), Times.Once);
            FileProcessing.Verify(c => c.Delete(), Times.Once);

            VerifyLog(LogFileMon, 0);
        }

        [Test]
        public async Task GivenANotmatchedFileWhenDroppedThenIsDeleted()
        {
            // Arrange
            Configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "MatchPattern", "a...d" } }).Build();

            var fileMonitor = new FileMonitorLogic(LogFileMon.Object, MessageBus.Object, Options, Storage.Object);
            var fileParser = new FileParserLogic(LogFileParser.Object, Storage.Object, Options, Configuration);

            // Act
            await fileMonitor.ProcessFiles();
            await fileParser.Parse(FileInfo);

            //  Assert
            FileIncoming.Verify(c => c.Delete(), Times.Once);
            MessageBus.Verify(c => c.SendMessage(It.IsAny<FileInfo>()), Times.Once);

            FileProcessing.Verify(c => c.ChangeDirectory(Options.Value.CompletedDirectory), Times.Never);
            FileProcessing.Verify(c => c.Delete(), Times.Once);

            VerifyLog(LogFileMon, 0);
        }

        [Test]
        public async Task GivenAFileWhenDroppedAndServiceBusIsDownThenIsNotMovedAndLogsError()
        {
            // Arrange
            MessageBus.Setup(c => c.SendMessage(It.IsAny<FileInfo>())).Returns(Task.FromResult(false));

            var fileMonitor = new FileMonitorLogic(LogFileMon.Object, MessageBus.Object, Options, Storage.Object);
            var fileParser = new FileParserLogic(LogFileParser.Object, Storage.Object, Options, Configuration);

            // Act
            await fileMonitor.ProcessFiles();

            //  Assert
            FileIncoming.Verify(c => c.Delete(), Times.Never);
            MessageBus.Verify(c => c.SendMessage(It.IsAny<FileInfo>()), Times.Once);
            VerifyLog(LogFileMon, 1);

        }

        [Test]
        public async Task GivenAFileWithExistingNameWhenDroppedThenIsNotProcessedAndLogsError()
        {
            // Arrange
            FileIncoming.Setup(c => c.ChangeDirectory(Options.Value.ProcessingDirectory)).Returns(Task.FromResult(false));
            
            var fileMonitor = new FileMonitorLogic(LogFileMon.Object, MessageBus.Object, Options, Storage.Object);
            var fileParser = new FileParserLogic(LogFileParser.Object, Storage.Object, Options, Configuration);

            // Act
            await fileMonitor.ProcessFiles();

            //  Assert
            FileIncoming.Verify(c => c.Delete(), Times.Never);
            MessageBus.Verify(c => c.SendMessage(It.IsAny<FileInfo>()), Times.Never);
            VerifyLog(LogFileMon, 1);
        }

        [Test]
        public async Task GivenAFileWhenDroppedAndNotReadyThenIsNotProcessed()
        {
            // Arrange
            FileIncoming.Setup(c => c.IsReady()).Returns(Task.FromResult(false));

            var fileMonitor = new FileMonitorLogic(LogFileMon.Object, MessageBus.Object, Options, Storage.Object);
            var fileParser = new FileParserLogic(LogFileParser.Object, Storage.Object, Options, Configuration);

            // Act
            await fileMonitor.ProcessFiles();

            //  Assert
            FileIncoming.Verify(c => c.Delete(), Times.Never);
            MessageBus.Verify(c => c.SendMessage(It.IsAny<FileInfo>()), Times.Never);

            VerifyLog(LogFileMon, 0);
        }

        [Test]
        public async Task GivenAFileWhenDroppedAndDoesNotExistWhenProcessingThenWarningIsLogged()
        {
            // Arrange
            FileProcessing.Setup(c => c.Exists()).Returns(Task.FromResult(false));

            var fileMonitor = new FileMonitorLogic(LogFileMon.Object, MessageBus.Object, Options, Storage.Object);
            var fileParser = new FileParserLogic(LogFileParser.Object, Storage.Object, Options, Configuration);

            // Act
            await fileMonitor.ProcessFiles();
            await fileParser.Parse(FileInfo);

            //  Assert
            FileIncoming.Verify(c => c.Delete(), Times.Once);
            MessageBus.Verify(c => c.SendMessage(It.IsAny<FileInfo>()), Times.Once);

            FileProcessing.Verify(c => c.ChangeDirectory(Options.Value.CompletedDirectory), Times.Never);
            FileProcessing.Verify(c => c.Delete(), Times.Never);

            VerifyLog(LogFileParser, 1, LogLevel.Warning);
        }
    }
}
