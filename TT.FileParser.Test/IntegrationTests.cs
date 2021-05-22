using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TT.FileParserFunction;

namespace FileLogicTest
{
    public class IntegrationTests
    {
        IConfiguration _configuration;
        Mock<ILogger<FileMonitorLogic>> _logFileMon;
        Mock<ILogger<FileParserLogic>> _logFileParser;
        Mock<IMessageBus> _messageBus;
        Mock<IStorageFacade> _storage;
        Mock<IDirectoryFacade> _directory;
        Mock<IFileFacade> _fileIncoming;
        Mock<IFileFacade> _fileProcessing;
        IOptions<StorageOptions> _options;
        string _fileName = "f1.txt";
        FileInfo _fileInfo;


        [SetUp]
        public void Setup()
        {
            _options = Options.Create(new StorageOptions() { IncomingDirectory = "incoming", ProcessingDirectory = "processing", CompletedDirectory = "completed", ShareName = "share" });
            _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "MatchPattern", "a*d" } }).Build();
            _fileInfo = new FileInfo() { DirectoryName = _options.Value.ProcessingDirectory, FileName = _fileName };

            _messageBus = new Mock<IMessageBus>();
            _logFileMon = new Mock<ILogger<FileMonitorLogic>>();
            _logFileParser = new Mock<ILogger<FileParserLogic>>();
            _storage = new Mock<IStorageFacade>();
            _directory = new Mock<IDirectoryFacade>();
            _fileIncoming = new Mock<IFileFacade>();
            _fileProcessing = new Mock<IFileFacade>();

            // Incoming
            _storage.Setup(c => c.GetDirectory(_options.Value.IncomingDirectory)).Returns(_directory.Object);
            _directory.Setup(c => c.GetFiles()).Returns(GetFiles(_fileIncoming));
            _fileIncoming.Setup(c => c.FileName).Returns(_fileName);
            _fileIncoming.Setup(c => c.IsFileReady()).Returns(Task.FromResult(true));
            _fileIncoming.Setup(c => c.ChangeDirectory(_options.Value.ProcessingDirectory)).Returns(Task.FromResult(true));

            //Processing
            _storage.Setup(c => c.GetDirectory(_options.Value.ProcessingDirectory)).Returns(_directory.Object);
            _directory.Setup(c => c.GetFile(_fileName)).Returns(_fileProcessing.Object);
            _fileProcessing.Setup(c => c.GetFileLines()).Returns(GetFileLines());

            _messageBus.Setup(c => c.SendMessage(It.IsAny<FileInfo>())).Returns(Task.FromResult(true));
        }

        [Test]
        public async Task GivenAMatchedFileWhenDroppedThenIsMoved()
        {
            // Arrange
            var fileMonitor = new FileMonitorLogic(_logFileMon.Object, _messageBus.Object, _options, _storage.Object);
            var fileParser = new FileParserLogic(_logFileParser.Object, _storage.Object, _options, _configuration);

            // Act
            await fileMonitor.ProcessFiles();
            await fileParser.Parse(_fileInfo);

            //  Assert
            _fileIncoming.Verify(c => c.DeleteFile(), Times.Once);
            _messageBus.Verify(c => c.SendMessage(It.IsAny<FileInfo>()), Times.Once);

            _fileProcessing.Verify(c => c.ChangeDirectory(_options.Value.CompletedDirectory), Times.Once);
            _fileProcessing.Verify(c => c.DeleteFile(), Times.Once);

            VerifyError(0);
        }

        [Test]
        public async Task GivenANotmatchedFileWhenDroppedThenIsDeleted()
        {
            _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "MatchPattern", "a...d" } }).Build();

            // Arrange
            var fileMonitor = new FileMonitorLogic(_logFileMon.Object, _messageBus.Object, _options, _storage.Object);
            var fileParser = new FileParserLogic(_logFileParser.Object, _storage.Object, _options, _configuration);

            // Act
            await fileMonitor.ProcessFiles();
            await fileParser.Parse(_fileInfo);

            //  Assert
            _fileIncoming.Verify(c => c.DeleteFile(), Times.Once);
            _messageBus.Verify(c => c.SendMessage(It.IsAny<FileInfo>()), Times.Once);

            _fileProcessing.Verify(c => c.ChangeDirectory(_options.Value.CompletedDirectory), Times.Never);
            _fileProcessing.Verify(c => c.DeleteFile(), Times.Once);

            VerifyError(0);
        }

        [Test]
        public async Task GivenAFileWhenDroppedAndServiceBusIsDownThenIsNotMovedAndLogsError()
        {
            _messageBus.Setup(c => c.SendMessage(It.IsAny<FileInfo>())).Returns(Task.FromResult(false));

            // Arrange
            var fileMonitor = new FileMonitorLogic(_logFileMon.Object, _messageBus.Object, _options, _storage.Object);
            var fileParser = new FileParserLogic(_logFileParser.Object, _storage.Object, _options, _configuration);

            // Act
            await fileMonitor.ProcessFiles();

            //  Assert
            _fileIncoming.Verify(c => c.DeleteFile(), Times.Never);
            _messageBus.Verify(c => c.SendMessage(It.IsAny<FileInfo>()), Times.Once);
            VerifyError(1);

        }

        [Test]
        public async Task GivenAFileWithExistingNameWhenDroppedThenIsNotProcessedAndLogsError()
        {
            _fileIncoming.Setup(c => c.ChangeDirectory(_options.Value.ProcessingDirectory)).Returns(Task.FromResult(false));

            // Arrange
            var fileMonitor = new FileMonitorLogic(_logFileMon.Object, _messageBus.Object, _options, _storage.Object);
            var fileParser = new FileParserLogic(_logFileParser.Object, _storage.Object, _options, _configuration);

            // Act
            await fileMonitor.ProcessFiles();

            //  Assert
            _fileIncoming.Verify(c => c.DeleteFile(), Times.Never);
            _messageBus.Verify(c => c.SendMessage(It.IsAny<FileInfo>()), Times.Never);
            VerifyError(1);
        }

        [Test]
        public async Task GivenAFileWhenDroppedAndNotReadyThenIsNotProcessed()
        {
            _fileIncoming.Setup(c => c.IsFileReady()).Returns(Task.FromResult(false));

            // Arrange
            var fileMonitor = new FileMonitorLogic(_logFileMon.Object, _messageBus.Object, _options, _storage.Object);
            var fileParser = new FileParserLogic(_logFileParser.Object, _storage.Object, _options, _configuration);

            // Act
            await fileMonitor.ProcessFiles();

            //  Assert
            _fileIncoming.Verify(c => c.DeleteFile(), Times.Never);
            _messageBus.Verify(c => c.SendMessage(It.IsAny<FileInfo>()), Times.Never);

            VerifyError(0);
        }

        private void VerifyError(int errorTimes)
        {
            _logFileMon.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<FormattedLogValues>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()), 
                Times.Exactly(errorTimes));
        }

        private async IAsyncEnumerable<IFileFacade> GetFiles(IMock<IFileFacade> file)
        {
            yield return file.Object;
            await Task.CompletedTask;
        }

        private async IAsyncEnumerable<string> GetFileLines()
        {
            yield return "This is line 1";
            yield return "This is line 2 abc";
            yield return "This is line 3 abd matched";
            await Task.CompletedTask;
        }
    }



}
