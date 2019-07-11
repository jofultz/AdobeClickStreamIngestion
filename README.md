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
### High-Level Flow
Overall, this is a fairly simple solution as can be seen in the following depiction:

![highlevelflow](https://raw.githubusercontent.com/jofultz/AdobeClickStreamIngestion/master/images/HighLevelFlow.png)
`Figure 01: High-Level Flow with Settings Dependencies`

When the timer trigger fires the ClickStreamController places the configured number of messages on the queue based on the application settings.  As each message becomes visible it trggers the GetClickStreamData Function.  GetClickStreamData will retrieve needed secrets from the app settings (KeyVault if configured), retrieve an authorization token, and begin to retrieve data from the data endpoint for the configured amount of time.  Each record received is added to an ICollector\<EventHub\> and at the expiry for the run duration that data is persisted to EventHub by the Binder.
### Scheduling and Concurrency
Scheduling and concurrency are managed by adjusting 4 settings: **TimerSchedule**, **ExecutionSeconds**, **MaxReaders**, and **GapIntervalSeconds**.  Never adjust one independently without considering the impact on concurrency and potential overall between running jobs and the next TimerSchedule interval.  Additionally, MaxReaders should never be more than 8 as that is the maximum allowed by Adobe.

To understand how the schedule and concurrency work, please consider the following timeline for the settings of:

* TimerSchedule = 0 \*/4 \* \* \* \*     (4 minutes)
* ExecutionSeconds = 120
* MaxReaders = 3
* GapIntervalSeconds = 60

![scheduling](https://raw.githubusercontent.com/jofultz/AdobeClickStreamIngestion/master/images/schedulingconcurrencytimeline.png)
`Figure 02:Scheduling and Concurrency`

In this configuration the total run time for each batch of 3 readers is 4 minutes.  With a GapInterval of 60 seconds each reader starts in the middle of the 120 runtime of the previously started reader, but there are only ever 2 concurrent readers retrieving data.

While one may overlapp the end of a batch with the beginning of the next batch, care must be taken to not overlap the timer schedule with the ExecutionSeconds to the degree that would cause either:
1. more than 8 readers to run concurrently as Adobe's max is 8
2. spawn readers infinitely as the duration and gap interval does not allow for completion of the readers prior completing half of the next batch of readers.

### Security Considerations
The preference should be to use KeyVault instead of direct App Settings for all of the sensitve items.  In this implementation KeyVault was used.  Additionally, **SecureString** was used for all things kept in RAM.  For example, the auth token is retrieved and placed in a static variable.  This provides a level of caching for any instance running on the same host as that static variable will have the value.  However, a **SecureString** was used to ensure that memory could not be dumped and the string retrieved.  **NOTE** that there are some gaps in that the token is retrieved over HTTPS and must be parsed from the response.  Until the it is parsed, assigned to a **SecureString**, and the response object collected by GC it could be dumped from RAM.
