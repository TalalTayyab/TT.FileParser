using System.Threading.Tasks;

namespace TT.FileParserFunction
{
    public interface IMessageBus
    {
        Task<bool> SendMessage(FileInfo fileInfo);
    }
}