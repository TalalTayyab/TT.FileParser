using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(TT.FileParserFunction.Startup))]
namespace TT.FileParserFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<StorageOptions>().Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("StorageOptions").Bind(settings);
            });
            builder.Services.AddSingleton<IMessageBus, MessageBus>();
            builder.Services.AddSingleton<IStorageFacade, AzureFileStorage>();
            builder.Services.AddSingleton<FileMonitorLogic>();
            builder.Services.AddSingleton<FileParserLogic>();

        }
    }
}
