namespace TT.FileParserFunction
{
    public class StorageOptions
    {
        public string ConnectionString { get; set; }
        public string ShareName { get; set; }
        public string IncomingDirectory { get; set; }
        public string ProcessingDirectory { get; set; }
        public string CompletedDirectory { get; set; }
        public int WaitSecondsUntilLastModified { get; set; }
    }
}
