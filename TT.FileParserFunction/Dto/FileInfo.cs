namespace TT.FileParserFunction
{
    public class FileInfo
    {
        public string FileName { get; set; }
        public string ShareName { get; set; }
        public string DirectoryName { get; set; }
        public string Path => $"{DirectoryName}/{FileName}";
    }
}
