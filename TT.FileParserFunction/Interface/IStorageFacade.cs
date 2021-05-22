namespace TT.FileParserFunction
{
    public interface IStorageFacade
    {
        IDirectoryFacade GetDirectory(string directoryName);
    }
}