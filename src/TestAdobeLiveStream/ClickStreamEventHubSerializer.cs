using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;

namespace TestAdobeLiveStream
{
    //TODO: This class and it's interface need refactoring.
    class ClickStreamEventHubSerializer : IClickStreamSerializer
    {
        private ILogger logger = default;
        public bool Initialize(ILogger log)
        {
            bool retVal = true;

            try
            {
                logger = log;

            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                retVal = false;
            }

            return retVal;
        }

        public bool Serialize(string message)
        {

            throw new NotImplementedException();
        }

       //Adding records to the collector.  The Function binding will handle serialization to EventHub
        public bool Serialize(string message, 
                                ICollector<EventData> outData)
        {
            bool retVal = true;

            try
            {
                byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
                outData.Add(new EventData(messageBytes));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                retVal = false;
            }

            return retVal;
        }
    }
}
