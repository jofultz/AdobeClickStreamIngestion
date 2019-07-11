using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;

namespace TestAdobeLiveStream
{
    public static class ClickStreamIngestController
    {
        [FunctionName("ClickStreamIngestController")]
        public static void Run([TimerTrigger("%TimerSchedule%")]TimerInfo myTimer, ExecutionContext ExContext, ILogger log, [Queue("%ControlQueueName%",
                             Connection = "IngestControlQueueConnection")]
                                CloudQueue outputQueue)
        {
            //Get configured number of messages to populate in the queue to spawn readers
            int maxControlMessages = default;
            string maxReaders = Environment.GetEnvironmentVariable("MaxReaders", EnvironmentVariableTarget.Process);
            if (!int.TryParse(maxReaders, out maxControlMessages))
            {
                string errorMsg = "Invalid value for MaxReaders setting.  Assigned value = " + maxReaders;
                log.LogCritical(errorMsg);
                throw new System.Exception(errorMsg);
            }

            //ensure that we don't queue more readers than concurrent max (8) for Adobe and > 0
            if (maxControlMessages > 8)
            {
                string errorMsg = "Invalid value for maxControlMessages setting.  Assigned value = " + maxControlMessages + ". Value assigned 8.";
                log.LogInformation(errorMsg);
                maxControlMessages = 8;
            }
            else if (maxControlMessages < 0)
            {
                string errorMsg = "Invalid value for maxControlMessages setting.  Assigned value = " + maxControlMessages + ". Value assigned 1.";
                log.LogInformation(errorMsg);
                maxControlMessages = 1;
            }

            //retrieve setting value passed to ingestion function to dictate how long to execute the data fetch
            int executionSeconds = default;
            string execDurationSetting = Environment.GetEnvironmentVariable("ExecutionSeconds", EnvironmentVariableTarget.Process);
            if (!int.TryParse(execDurationSetting, out executionSeconds))
            {
                string errorMsg = "Invalid value for ExecutionSeconds setting.  Assigned value = " + execDurationSetting;
                log.LogCritical(errorMsg);
                throw new System.Exception(errorMsg);
            }

            //ensuring that there is a least 1 second of run time
            if (executionSeconds < 1)
            {
                string errorMsg = "Invalid value for executionSeconds setting.  Assigned value = " + executionSeconds + ". Value assigned 1.";
                log.LogInformation(errorMsg);
                executionSeconds = 1;
            }
            

            //use this to control message visibility
            int gapIntervalSeconds = default;
            string gapIntervalSetting = Environment.GetEnvironmentVariable("GapIntervalSeconds", EnvironmentVariableTarget.Process);
            if (!int.TryParse(gapIntervalSetting, out gapIntervalSeconds))
            {
                string errorMsg = "Invalid value for GapIntervalSeconds setting.  Assigned value = " + gapIntervalSetting;
                log.LogCritical(errorMsg);
                throw new System.Exception(errorMsg);
            }


            //ensure that the value is greater than 
            log.LogInformation("ID: " + ExContext.InvocationId.ToString() + " executing with maxControlMessages = " + maxControlMessages.ToString());
            //create messages for control queue
            for (int msgCount = 0; msgCount < maxControlMessages; msgCount++)
            {
                int visibilityDelay = msgCount * gapIntervalSeconds;
                var newMessage = new IngestControlMessage { ServiceLabel = "ClickstreamIngest", ExecutionSeconds = executionSeconds, MaxConnections=maxControlMessages};
                outputQueue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(newMessage)), null, TimeSpan.FromSeconds(visibilityDelay), null, null);
            }

            log.LogInformation("ID: " + ExContext.InvocationId.ToString() + $"completed at: {DateTime.Now}");
        }
    }
}
