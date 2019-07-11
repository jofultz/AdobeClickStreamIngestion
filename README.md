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
  * For testing one I used 4 partitions
  * For production I used 12 partitions as 4 lead to throttling when both producers and consumers were active
  * The **EventHubName** and the **EventHubConnection** string will be needed
* Azure KeyVault
  * All of the sensitive configuration values are stored in KeyVault
  * Create an Access Policy and give the Azure Function identity List and Get for Secrets
  * The **KeyVaultSecretsProvision.sh** contains script to provision secrets, but you will need to add your KeyVault name and setting values
  * Each of the secret URIs will be needed for the secrets in KeyVault
  




## Runtime Operation


## Operational Notes
