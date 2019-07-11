# Adobe LiveStream Data Ingestion 
> Using Azure Functions and EventHubs

The Function App contains to Functions: 

|Function Name|File Location|Trigger Type|Description|
| :--- | :--- | :--- | :--- | 
|ClickStreamIngestController|ClickStreamIngestController.cs|Timer|Controls the ingestion Function by placing messages on the queue.|
|GetClickStreamData|DataRetrieval.cs|Queue|Reads data from the Adobe LiveStream endpoint for the configured duration of time and sends the messages to an EventHub|

## Dependencies and Setup

This uses Azure Functions, Azure EventHubs, Azure Storage, and Azure

## Runtime Operation


## Operational Notes
