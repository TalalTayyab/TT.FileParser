## Building

.NET Core must be installed.

1. Clone the project.
1. Open `TT.FileParserFunction.csproj` in Visual Studio
1. Build the solution

or 

1. Open command prompt or powershell
1. Navigate to `\TT.FileParser\TT.FileParserFunction`
1. Run `dotnet build`

## Tests
1. Browse to folder `\TT.FileParser\TT.FileParser.Test`
1. Run `dotnet test

There are two tests.
1. Unit test - `IntegrationTests` - covering the file parser logic
2. Integration Test `ParseLineUnitTests` - covering the whole end-to-end cycle, mocking the file share and service bus.

## Azure Deployment

Azure CLI must be installed. Instructions [here](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?tabs=azure-cli)

1. Open powershell prompt
1. Navigate to the code folder e.g `C:\dev\TT.FileParser\`
1. Login `az login`
1. To view subscriptions `az account show --output table`
1. Set the correct subscription `az account set --subscription <name or id>` 
1. Run `.\deploy.ps1` - This will create all the resources and deploy the function with the correct settings.

### Configurable Parameters

|Name|Value|Description
|-|-|-
|$matchPattern|"a*d"|Match pattern - configure this value
|$fileMonitorTrigger|"*/5 * * * * *|CRON - default is once every 5 seconds
|$incomingDirectory|incoming|Folder where files should be dropped
|$processingDirectory|processing|Folder where files are moved while processing
|$completedDirectory|completed|Folder where files are moved if match is found
|$waitSecondsUntilLastModified|10|Number of seconds to wait until the last modified time before the file is considered completely uploaded and will be processed. 



### Deployment Parameters

|Name|Value|Description
|-|-|-
|$resourceGroupName |TTResourceGroup052021|Name of resource group
|$storageAccountName|ttstorageaccount052021|Name of storage account
|$location|australiasoutheast|Location
|$shareName|fileshare|File share name
|$serviceBusName|ttservicebus052021|Name of service bus
|$queueName|fileshare|Name of queue
|$functionAppName|ttfileparsefunctionapp052021|Function app name
|$projectPath|TT.FileParserFunction\TT.FileParserFunction.csproj|Path to project - used for deployment



## Uploading files
Uploading of files can be done via Azure portal. Navigate to the file share and upload the files in the `incoming` folder.

## Points

1. Search is case insensitive
1. File reading is line by line (assuming each line is within reasonable limit). 
1. File with the same name will be overwritten if it exists in the completed directory
1. If a file with the same name is being processed and is also uploaded , it will be ignored until the processed file is completed.
1. Error handling is rudimentary - the file will stay in the directory and an error will be logged. 
1. Service bus messages will be retried depending on the policy and moved to dead letter queue.
1. Ideally deployment should be via CD but powershell was easier given the time limit.
