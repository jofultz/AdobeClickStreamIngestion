using System;
using System.Collections.Generic;
using System.Text;

namespace TestAdobeLiveStream
{
    //TODO: Move to common library since used by both controller and ingestion functions
    public class IngestControlMessage
    {
        public string ServiceLabel = default;
        public int ExecutionSeconds = 60;
        public int MaxConnections = 1;
    }
}

