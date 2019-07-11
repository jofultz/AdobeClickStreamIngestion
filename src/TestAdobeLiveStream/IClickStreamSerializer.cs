using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace TestAdobeLiveStream
{
    //ISSUE: Ad-hoc class and type definition has lead to poor interface and concerete design.  Need to refactor.
    //TODO: Implement virtual and move concretes to inherit from the virtual to handle the log initialization and private member variable in the base class
    interface IClickStreamSerializer
    {
        bool Initialize(ILogger log);
        bool Serialize(string message,  ICollector<EventData> outData);
        bool Serialize(string message);
    }
}
