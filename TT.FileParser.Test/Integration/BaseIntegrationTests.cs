using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TT.FileParserFunction;

namespace TT.FileParser.Test.Integration
{
    public class BaseIntegrationTests
    {
        protected IConfiguration Configuration { get; set; }
        protected Mock<ILogger<FileMonitorLogic>> LogFileMon { get; set; }
        protected Mock<ILogger<FileParserLogic>> LogFileParser { get; set; }
        protected Mock<IMessageBus> MessageBus { get; set; }
        protected Mock<IStorageFacade> Storage { get; set; }
        protected Mock<IDirectoryFacade> Directory { get; set; }
        protected Mock<IFileFacade> FileIncoming { get; set; }
        protected Mock<IFileFacade> FileProcessing { get; set; }
        protected IOptions<StorageOptions> Options { get; set; }
        protected string FileName { get; set; } = "f1.txt";
        protected FileInfo FileInfo { get; set; }

        [SetUp]
        public void Setup()
        {
            Options = Microsoft.Extensions.Options.Options.Create(new StorageOptions() { IncomingDirectory = "incoming", ProcessingDirectory = "processing", CompletedDirectory = "completed", ShareName = "share" });
            Configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "MatchPattern", "a*d" } }).Build();
            FileInfo = new FileInfo() { DirectoryName = Options.Value.ProcessingDirectory, FileName = FileName };

            MessageBus = new Mock<IMessageBus>();
            LogFileMon = new Mock<ILogger<FileMonitorLogic>>();
            LogFileParser = new Mock<ILogger<FileParserLogic>>();
            Storage = new Mock<IStorageFacade>();
            Directory = new Mock<IDirectoryFacade>();
            FileIncoming = new Mock<IFileFacade>();
            FileProcessing = new Mock<IFileFacade>();

            // Incoming
            Storage.Setup(c => c.GetDirectory(Options.Value.IncomingDirectory)).Returns(Directory.Object);
            Directory.Setup(c => c.GetFiles()).Returns(GetFiles(FileIncoming));
            FileIncoming.Setup(c => c.FileName).Returns(FileName);
            FileIncoming.Setup(c => c.IsReady()).Returns(Task.FromResult(true));
            FileIncoming.Setup(c => c.ChangeDirectory(Options.Value.ProcessingDirectory)).Returns(Task.FromResult(true));

            //Processing
            Storage.Setup(c => c.GetDirectory(Options.Value.ProcessingDirectory)).Returns(Directory.Object);
            Directory.Setup(c => c.GetFile(FileName)).Returns(FileProcessing.Object);
            FileProcessing.Setup(c => c.GetLines()).Returns(GetFileLines());
            FileProcessing.Setup(c => c.Exists()).Returns(Task.FromResult(true));

            MessageBus.Setup(c => c.SendMessage(It.IsAny<FileInfo>())).Returns(Task.FromResult(true));
        }

        protected void VerifyLog<T>(Mock<ILogger<T>> log, int times, LogLevel logLevel = LogLevel.Error)
        {
            log.Verify(x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()),
                Times.Exactly(times));
        }

        protected async IAsyncEnumerable<IFileFacade> GetFiles(IMock<IFileFacade> file)
        {
            yield return file.Object;
            await Task.CompletedTask;
        }

        protected async IAsyncEnumerable<string> GetFileLines()
        {
            yield return "This is line 1";
            yield return "This is line 2 abc";
            yield return "This is line 3 abd matched";
            await Task.CompletedTask;
        }
    }
}
