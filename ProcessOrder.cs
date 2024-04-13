using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Blobs.Models;


namespace AzArch.Function
{
    public static class CosmosOrderFunction
    {
        [FunctionName("ProcessOrders")]
        public static void Run(
            [EventGridTrigger] EventGridEvent eventGridEvent,
            [Blob("neworders", FileAccess.Read, Connection = "StorageConnectionString")] BlobContainerClient container,
            [CosmosDB(databaseName: "azarch-orders", collectionName: "orders", ConnectionStringSetting = "CosmosDBConnection")] out Order order,
            ILogger log)
        {
            order = null;

            try
            {
                log.LogInformation($"Event details: Topic: {eventGridEvent.Topic}");
                log.LogInformation($"Event data: {eventGridEvent.Data.ToString()}");

                string eventBody = eventGridEvent.Data.ToString();

                log.LogInformation("Deserializing to StorageBlobCreatedEventData...");
                var storageData = JsonSerializer.Deserialize<StorageBlobCreatedEventData>(eventBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                log.LogInformation("Done");

                log.LogInformation("Get the name of the new blob...");
                var blobName = Path.GetFileName(storageData.Url);
                log.LogInformation($"Name of file: {blobName}");

                log.LogInformation("Get blob from storage...");
                BlobClient blobClient = container.GetBlobClient(blobName);
                BlobDownloadInfo downloadInfo = blobClient.Download();
                string orderText;

                using (StreamReader reader = new StreamReader(downloadInfo.Content))
                {
                    orderText = reader.ReadToEnd();
                }
                log.LogInformation($"Order text: {orderText}");

                order = JsonSerializer.Deserialize<Order>(orderText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error in function");
            }
        }
    }
}
