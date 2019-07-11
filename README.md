# Adobe LiveStream Data Ingestion 
> Using Azure Functions and EventHubs

The Function App contains two Functions: 

|Function Name|File Location|Trigger Type|Description|
| :--- | :--- | :--- | :--- | 
|ClickStreamIngestController|ClickStreamIngestController.cs|Timer|Controls the ingestion Function by placing messages on the queue.|
|GetClickStreamData|DataRetrieval.cs|Queue|Reads data from the Adobe LiveStream endpoint for the configured duration of time and sends the messages to an EventHub|

## Dependencies and Setup
### Adobe Dependencies
An endpoint must be setup by Adobe to retrieve the LiveStream data.  Along with that setup one should receive a specific App ID, App Secret, and URI which are all required to authenticate and fetch data.  
### Azure Dependencies
Several Azure components are used:
1. Azure Function App - Consumption Plan
2. Azure Storage - Used for the Function App Storage and Storage Queue for ingestion operation
3. Azure EventHub - Used as the initial landing location for the ingestion Function
4. Azure KeyVault - Stores secrets.  In particular, the Adobe client specific information mentioned previously and connection strings for Storage and EventHub.


This uses Azure Functions, Azure EventHubs, Azure Storage, and Azure

## Runtime Operation


## Operational Notes
