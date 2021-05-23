
# Getting started

Clone the project from git.

1. For direct Azure deployment click [here](##Azure-deployment)
1. For building and running locally click [here](##Local)
1. For Design consideration click [here](##Design)

## Azure Deployment

Azure CLI must be installed. Instructions [here](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?tabs=azure-cli)

1. Open powershell prompt
1. Navigate to the code folder e.g `C:\dev\TT.FileParser\`
1. Login `az login` (checked with version 2.23.0)
1. To view subscriptions `az account show --output table`
1. Set the correct subscription `az account set --subscription <name or id>` 
1. Run `.\deploy.ps1` - This will create all the resources, build and deploy the function with the configured parameters.

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

## Local

### Building

.NET Core/VS 2019 must be installed along with Azure tools.

#### Visual Studio

1. Clone the project.
1. Open `TT.FileParserFunction.csproj` in Visual Studio
1. Build the solution

#### Command prompt

1. Open command prompt or powershell
1. Navigate to `\TT.FileParser\TT.FileParserFunction`
1. Run `dotnet build`

### Tests

1. Browse to folder `\TT.FileParser\TT.FileParser.Test`
1. Run `dotnet test

There are two tests.

1. Unit test - `IntegrationTests` - covering the file parser logic
2. Integration Test `ParseLineUnitTests` - covering the whole end-to-end cycle, mocking the file share and service bus.

### Running

The settings must be updated in local.settings.json before the project can be run locally. Get the values from the Azure portal.

## Uploading files

Uploading of files can be done via Azure portal. Navigate to the file share and upload the files in the `incoming` folder.

## Design

This is a cloud native design using Azure functions, Storage and Service bus/Queues.

On a high level there are two Azure functions. The first monitors for incoming files and posts a message in the Service bus  queue, the second is triggered when a message arrives in the queue. The design allows for scaling of the message processing depending on the Azure Web App consumption plan.

Other approaches considered but not selected.

1. Use database (like Cosmos) for maintaining a state of the files. However given the simplicity of the requirement, it seemed like an overkill.
1. Use Azure Storage - the trigger functionality within Azure functions was the deciding factor.
1. Azure Event Hub - due to lack of notification from Azure file share, there was no advantage in choosing event hub over service bus.

### Azure functions

1. FileMonitorFunction :- This runs on a timer and monitors the Azure file share. Internally it just calls FileMonitorLogic. If any file is found, it will be moved to the processing directory and a message posted in Service bus Queue. Control the timer via the setting `FileMonitorTrigger` and the time to wait until file was last modified `StorageOptions:WaitSecondsUntilLastModified`.

The reason for using a timer/poll functionality is because Azure file share does not support notification when a file is uploaded.

1. FileParserFunction :- This function is triggered when a message arrives in the service bus queue. Internally it calls FileParserLogic. The design allow for multiple processing of files as they are moved into the queue. Each file is read line by line via buffered stream. If a match is found, it is moved to the completed directory, otherwise deleted. The setting `MatchPattern` can be updated for specifying the match pattern and `ServiceBusQueue` for specifying the queue to monitor for file messages.

### Logic classes

1. ParseLine :- Class that implements the matching functionality. 

### Facade

1. AzureFileStorage, AzureDirectory,AzureFile :- Wrapper around Azure file storage. This allows mocking and plugging different file system - example local file. 

1. IStorageFacade,IDirectoryFacade,IFileFacade :- Facade around the storage , allows for testability and abstract implementation details of Azure file share.

1. MessageBus, IMessageBus :- Simple message bus wrapper

## Other Points

1. Search is case insensitive
1. File reading is line by line (assuming each line is within reasonable limit). 
1. File with the same name will be overwritten if it exists in the completed directory
1. If a file with the same name is being processed and is also uploaded , it will be ignored until the processed file is completed.
1. Error handling is rudimentary - the file will stay in the directory and an error will be logged. 
1. In case of exception during processing service bus messages will be retried (depending on the policy) and moved to dead letter queue after max attempts.
1. If a file is not found during processing, a warning will be logged (this is to handle idempotent processing)
1. Ideally deployment should be via CD but powershell was easier given the time limit.
