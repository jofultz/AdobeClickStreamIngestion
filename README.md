# Adobe LiveStream Data Ingestion 
> Using Azure Functions, EventHub, and KeyVault

The Function App contains two Functions: 

|Function Name|File Location|Trigger Type|Description|
| :--- | :--- | :--- | :--- | 
|ClickStreamIngestController|ClickStreamIngestController.cs|Timer|Controls the ingestion Function by placing messages on the queue.|
|GetClickStreamData|DataRetrieval.cs|Queue|Reads data from the Adobe LiveStream endpoint for the configured duration of time and sends the messages to an EventHub|

## Dependencies and Setup
### Adobe Dependencies
An endpoint must be setup by Adobe to retrieve the LiveStream data.  Along with that setup one should receive a specific App ID, App Secret, and URI which are all required to authenticate and fetch data.  
### Azure Dependencies
In order to configure the application to run all of the dependencies must be configured.  While the Adobe configuration is more an output of a business interaction, the Azure dependencies require setup.  This will not provide detailed setup guidance for the dependencies.  An overview below of what is need is provided.

Azure components:
* Azure Storage
  * Use this account for the Function App Storage and Queues
  * Create a Storage Queue for ingestion operation control
  * The **ControlQueueName** and the **IngestControlQueueConnection** string will be needed
* Azure Function App
  * Use the previously configured Storage Account
  * Use a Consumption Plan
  * Create an identity.  I used a System Managed identity.
* Azure EventHub
  * For testing I used 4 partitions
  * For production I used 12 partitions as 4 lead to throttling when both producers and consumers were active
  * The **EventHubName** and the **EventHubConnection** string will be needed
* Azure KeyVault
  * All of the sensitive configuration values are stored in KeyVault
  * Create an Access Policy and give the Azure Function identity List and Get for Secrets
  * The **KeyVaultSecretsProvision.sh** contains script to provision secrets, but you will need to add your KeyVault name and setting values
  * Each of the secret URIs will be needed for the secrets in KeyVault
  

## Runtime Operation
### Application Settings
The run-time behavior of both Functions is determined by the following settings that must be present in your local settings file or in the App Settings for the Function App:

|Setting Key|Setting Value|Description|
| :--- | :--- | :--- | 
|EventHubName| [*your EH name*] | Target EventHub for incoming messages.|
|EventHubConnection| [@Microsoft.KeyVault(SecretUri=*your URI*) or *connection string*]| Used by the Binder to establish connection to EventHub|
|AdobeAuthURI| *https://api.omniture.com/token*| Used to retrieve auth token.  The known public endpoint is noted here, but one should double check there hasn't been a change.|
|IngestControlQueueConnection| [@Microsoft.KeyVault(SecretUri=*your URI*) or *connection string*]| Used by the Binder to connect to the Storage Queue.| 
|ControlQueueName| *your queue name*| Used by the Binder to connect to the proper Storage Queue.|
|AdobeAppID| [@Microsoft.KeyVault(SecretUri=*your URI*) or *AdobeAppID value*]| Used in the Authentication process.|
|AdobeAppSecret| [@Microsoft.KeyVault(SecretUri=*your URI*) or *Adobe App Secret*]| Used in the Authentication process.|
|AdobeURI| [@Microsoft.KeyVault(SecretUri=*your URI*) or *your Adobe URI*]| This is the endpoint provided by Adobe from which to retrieve data.|
|TimerSchedule|*cron expression*, e.g., "0 \*/4 \* \* \* \*"| Used by the Binder to control the firing of the **ClickStreamIngestController** Function.|
|ExecutionSeconds| *duration in seconds, > 0*| Used by the **GetClickStreamData** Function to determine how long to fetch data from Adobe.|
|MaxReaders| *number of readers, 0<MaxReaders<9*| Controls the number of messages populated on the queue to trigger acquistion Functions. This must be > 0 and Adobe allows no more than 8 concurrent readers.|
|GapIntervalSeconds| *seconds visibility delay*| This controls the number of seconds before each subsequent message on the queue is visible.  The interval is multiplied by the message number with index origin of 0.|

## Operational Notes
