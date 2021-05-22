# Settings #
$matchPattern = "a*d"
$fileMonitorTrigger = "*/5 * * * * *"
$incomingDirectory = "incoming"
$processingDirectory = "processing"
$completedDirectory = "completed"
$waitSecondsUntilLastModified = 10

# Deployment Parameters #
$resourceGroupName = "TTResourceGroup052021"
$storageAccountName = "ttstorageaccount052021"
$location = "australiasoutheast"
$shareName = "fileshare"
$serviceBusName = "ttservicebus052021"
$queueName = "fileshare"
$functionAppName = "ttfileparsefunctionapp052021"
$projectPath = "TT.FileParserFunction\TT.FileParserFunction.csproj"



$ErrorActionPreference = "Stop"

function createArtifact {
    param(
        $projectPath
    )
    $zipPath = publish $projectPath
    
    if ($zipPath -is [array]) {
        $zipPath = $zipPath[$zipPath.Length - 1]
    }
    return $zipPath
}

function publish {
    param(
        $projectPath        
    )

    $publishDestPath = "publish/" + [guid]::NewGuid().ToString()
    Write-Host "publishing project '$($projectPath)' in folder '$($publishDestPath)' ..." -ForegroundColor Yellow -BackgroundColor DarkGreen
    dotnet.exe publish $projectPath -c Release -o $publishDestPath
    $zipArchiveFullPath = "$($publishDestPath).Zip"
    Write-Host "creating zip archive '$($zipArchiveFullPath)'" -ForegroundColor Yellow -BackgroundColor DarkGreen
    $compress = @{
        Path             = $publishDestPath + "/*"
        CompressionLevel = "Fastest"
        DestinationPath  = $zipArchiveFullPath
    }

    Compress-Archive @compress
    Write-Host "cleaning up ..."
    Remove-Item -path "$($publishDestPath)" -recurse
    return $zipArchiveFullPath
}

az group create -l australiasoutheast -n $resourceGroupName --output none
Write-Host "Azure Group created $resourceGroupName" -ForegroundColor Yellow -BackgroundColor DarkGreen

az storage account create --name $storageAccountName --resource-group $resourceGroupName --location $location --sku Standard_RAGRS --kind StorageV2 --output none
Write-Host "Azure Storage created $storageAccountName" -ForegroundColor Yellow -BackgroundColor DarkGreen

az storage share-rm create --resource-group $resourceGroupName --storage-account $storageAccountName --name $shareName --access-tier "TransactionOptimized" --quota 1024 --output none
Write-Host "Azure File Share created $shareName" -ForegroundColor Yellow -BackgroundColor DarkGreen

$storageAccountKey=az storage account keys list --resource-group $resourceGroupName --account-name $storageAccountName --query [0].value -o tsv

az storage directory create --account-name $storageAccountName --account-key $storageAccountKey --share-name $shareName --name $incomingDirectory --output none
Write-Host "Azure Directory created $incomingDirectory" -ForegroundColor Yellow -BackgroundColor DarkGreen

az storage directory create --account-name $storageAccountName --account-key $storageAccountKey --share-name $shareName --name $processingDirectory --output none
Write-Host "Azure Directory created $processingDirectory" -ForegroundColor Yellow -BackgroundColor DarkGreen

az storage directory create --account-name $storageAccountName --account-key $storageAccountKey --share-name $shareName --name $completedDirectory --output none
Write-Host "Azure Directory created $completedDirectory" -ForegroundColor Yellow -BackgroundColor DarkGreen

az servicebus namespace create --resource-group $resourceGroupName --name $serviceBusName --location $location --output none
az servicebus queue create --resource-group $resourceGroupName --namespace-name $serviceBusName --name $queueName --output none
Write-Host "Service Bus created $serviceBusName with Queue $resourceGroupName" -ForegroundColor Yellow -BackgroundColor DarkGreen

az functionapp create --name $functionAppName --storage-account $storageAccountName --consumption-plan-location $location --resource-group $resourceGroupName --functions-version 2 --output none
Write-Host "Function app created $functionAppName" -ForegroundColor Yellow -BackgroundColor DarkGreen

$zipArchiveFullPath = createArtifact -projectPath $projectPath 

az functionapp deployment source config-zip -g "$($resourceGroupName)" -n "$($functionAppName)" --src "$($zipArchiveFullPath)"   
Write-Host "Function app deployed $functionAppName" -ForegroundColor Yellow -BackgroundColor DarkGreen

$serviceBusConnectionString = az servicebus namespace authorization-rule keys list -n RootManageSharedAccessKey -g $resourceGroupName --namespace-name $serviceBusName --query primaryConnectionString -o tsv
$storageAccountConnectionString = az storage account show-connection-string -g $resourceGroupName -n $storageAccountName -o tsv
az functionapp config appsettings set --name $functionAppName --resource-group $resourceGroupName --settings ServiceBusConnection=$serviceBusConnectionString ServiceBusQueue=$queueName MatchPattern=$matchPattern FileMonitorTrigger=$fileMonitorTrigger StorageOptions:ConnectionString=$storageAccountConnectionString StorageOptions:ShareName=$shareName StorageOptions:IncomingDirectory=$incomingDirectory StorageOptions:ProcessingDirectory=$processingDirectory StorageOptions:CompletedDirectory=$completedDirectory StorageOptions:WaitSecondsUntilLastModified=$waitSecondsUntilLastModified --output none
Write-Host "Function app settings updated $functionAppName" -ForegroundColor Yellow -BackgroundColor DarkGreen

Write-Host "Completed Deployment!" -ForegroundColor Yellow -BackgroundColor DarkGreen