using ImageResizeWebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageResizeWebApp.Helpers
{
    public static class StorageHelper
    {       
        public static async Task<string> UploadFileToStorage(Stream fileStream, string fileName, AzureStorageConfig _storageConfig, Action<Double> progressCallback)
        {
            // Create storagecredentials object by reading the values from the configuration (appsettings.json)
            StorageCredentials storageCredentials = new StorageCredentials(_storageConfig.AccountName, _storageConfig.AccountKey);

            // Create cloudstorage account by passing the storagecredentials
            CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Get reference to the blob container by passing the name by reading the value from the configuration (appsettings.json)
            CloudBlobContainer container = blobClient.GetContainerReference(_storageConfig.ImageContainer);

            // Get the reference to the block blob from the container
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

            var blockSize = 20 * 1024;

            blockBlob.StreamWriteSizeInBytes = blockSize;
            long bytesToUpload = fileStream.Length;
            long fileSize = bytesToUpload;

            List<string> blockIds = new List<string>();
            int index = 1;
            long startPosition = 0;
            long bytesUploaded = 0;

            do
            {
                var bytesToRead = Math.Min(blockSize, bytesToUpload);
                var blobContents = new byte[bytesToRead];

                fileStream.Position = startPosition;
                fileStream.Read(blobContents, 0, (int)bytesToRead);

                ManualResetEvent manualResetEvent = new ManualResetEvent(false);
                var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(index.ToString("d6")));
                blockIds.Add(blockId);
                var blockAsync = blockBlob.PutBlockAsync(blockId, new MemoryStream(blobContents), null);
                await blockAsync.ContinueWith(t =>
                {
                    bytesUploaded += bytesToRead;
                    bytesToUpload -= bytesToRead;
                    startPosition += bytesToRead;
                    index++;
                    double percentComplete = (double)bytesUploaded / (double)fileSize;
                    Console.WriteLine("Percent complete = " + percentComplete.ToString("P"));
                    progressCallback(percentComplete * 100);
                    manualResetEvent.Set();
                });
                manualResetEvent.WaitOne();
            }
            while (bytesToUpload > 0);

            var blockListAsync = blockBlob.PutBlockListAsync(blockIds);

            await blockListAsync.ContinueWith(t =>
            {
                Console.WriteLine("Blob uploaded.");
            });

            // Upload the file
            //await blockBlob.UploadFromStreamAsync(fileStream);

            return await Task.FromResult(blockBlob.Uri.AbsoluteUri);
        }
    }
}
