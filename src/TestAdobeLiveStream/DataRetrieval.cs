using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;


namespace TestAdobeLiveStream
{
    public static class DataRetrieval       
    {

        private static SecureString AuthToken = default;
        private static DateTimeOffset ExpiryOffset = default;
        private static SecureString AdobeAppID = default;
        private static SecureString AdobeAppSecret = default;
        private static string AuthUrl = default;

        private const string AdobeGrantTypeKey = "grant_type";
        private const string AdobeClientSecretKey = "client_secret";
        private const string AdobeClientIdKey = "client_id";

        private const string AdobeGrantType = "client_credentials";


        [FunctionName("GetClickStreamData")]
        public static void Run([QueueTrigger("%ControlQueueName%",Connection = "IngestControlQueueConnection")] IngestControlMessage myQueueItem, Microsoft.Azure.WebJobs.ExecutionContext ExContext, ILogger log,
                                         [EventHub("%EventHubName%",
                             Connection = "EventHubConnection")]
                                ICollector<EventData> outData)
        {

            if (AdobeAppID == default)
                AdobeAppID = SecureStringHelper.ConvertToSecureString(Environment.GetEnvironmentVariable("AdobeAppID", EnvironmentVariableTarget.Process));

            if (AdobeAppSecret == default)
                AdobeAppSecret = SecureStringHelper.ConvertToSecureString(Environment.GetEnvironmentVariable("AdobeAppSecret", EnvironmentVariableTarget.Process));

            SecureString authToken = RetrieveAuthToken(log);
            RetrieveData(log, authToken, myQueueItem, outData);

        }

        private static void RetrieveData(ILogger log, SecureString authToken, IngestControlMessage controlMessage, ICollector<EventData> outData)
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            using (HttpClient httpClient = new HttpClient(handler))
            {
                //TODO: Move AdobeURI to KeyVault
                string requestUri = Environment.GetEnvironmentVariable("AdobeURI", EnvironmentVariableTarget.Process);
                int maxConnections = controlMessage.MaxConnections < 8 ? controlMessage.MaxConnections + 1 : 8;
                //going to tell Adobe we need 1 more connection than we plan to use in case there is lag in shutdown that causes overlap
                requestUri += "?maxConnections=" + maxConnections.ToString();
               
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Add("Authorization", "Bearer " + SecureStringHelper.ConvertToUnsecureString(authToken));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

                var response = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
                var requestStream = response.Content.ReadAsStreamAsync().Result;

                using (var reader = new StreamReader(requestStream))
                {
                    DateTimeOffset loopExpiryOffset = DateTimeOffset.Now.AddSeconds(controlMessage.ExecutionSeconds);
                    ClickStreamEventHubSerializer ehSerializer = new ClickStreamEventHubSerializer();
                    ehSerializer.Initialize(log);

                    // Add records to the outData ICollector<> for the defined duration.  
                    // Once completed the binder will handle serialization to EventHub upon Function exit.
                    //WARNING: This will cause memory growth.  So, the volume over the duration needs to be kept manageable for the Function.
                    while (!reader.EndOfStream && loopExpiryOffset > DateTimeOffset.Now)
                    {
                        var currentLine = reader.ReadLine();
                        ehSerializer.Serialize(currentLine, outData);
                    }

                }

                //TODO: Log telemetry information about processing for AppInsights and debug/reconciliation 
            }
        }

        //TODO: Clean-up function dependencies and scope
        //      Specifically access to log and AuthToken
        private static SecureString RetrieveAuthToken(ILogger log)
        {
            // If the token or expiry time is missing or the expiry time is passed we'll refetch
            // This will only help on single instances of the function and providing the same worker is running.
            if (AuthToken == default || ExpiryOffset == default || DateTimeOffset.Now > ExpiryOffset)
            {
                //TODO: need a validity check for this setting
                AuthUrl = Environment.GetEnvironmentVariable("AdobeAuthURI", EnvironmentVariableTarget.Process);

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

                    var authPostBody = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>(AdobeGrantTypeKey, AdobeGrantType),
                        new KeyValuePair<string, string>(AdobeClientIdKey, SecureStringHelper.ConvertToUnsecureString(AdobeAppID)),
                        new KeyValuePair<string, string>(AdobeClientSecretKey,SecureStringHelper.ConvertToUnsecureString(AdobeAppSecret))
                    });


                    var response = httpClient.PostAsync(new Uri(AuthUrl), authPostBody).Result;

                    //TODO: Replace with custom deserializer so that the access_token member is never in plain text in memory.  
                    //      This code encrypts right after deserialization and clears the object which leaves exposure window
                    //      once the response is received and after deserialization up until the object is GC.
                    //      A custom deserializer to SecureString for the access_token will shorten the window to only the amount of time
                    //      that the response is held in RAM.
                    var jsonContent = JsonConvert.DeserializeObject<AdobeAuthResponse>(response.Content.ReadAsStringAsync().Result);

                    //assign class vars
                    AuthToken = SecureStringHelper.ConvertToSecureString(jsonContent.access_token);
                    //set expiry time
                    int seconds = int.Parse(jsonContent.expires_in);
                    ExpiryOffset = DateTimeOffset.Now.AddSeconds(seconds);

                    //try to hurry up GC
                    jsonContent = null;
                }
            }
 
            return AuthToken;
        }
    }


}
