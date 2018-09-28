using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ImageResizeWebApp.Models;
using Microsoft.Extensions.Options;

using System.IO;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using ImageResizeWebApp.Helpers;
using Microsoft.AspNetCore.SignalR;
using ImageResizeWebApp.Hubs;

namespace ImageResizeWebApp.Controllers
{
    [Route("api/[controller]")]
    public class FilesController : Controller
    {
        
        private readonly AzureStorageConfig storageConfig = null;

        private readonly IHubContext<MessageHub> _hubContext;

        public FilesController(IOptions<AzureStorageConfig> config, IHubContext<MessageHub> hubContext)
        {
            storageConfig = config.Value;
            _hubContext = hubContext;
        }

        // POST /api/files/upload
        [HttpPost("[action]")]
        public async Task<IActionResult> Upload(ICollection<IFormFile> files)
        {
            string uploadedUrl = null;

            try
            {
                if (files.Count == 0)

                    return BadRequest("No files received from the upload");

                if (storageConfig.AccountKey == string.Empty || storageConfig.AccountName == string.Empty)

                    return BadRequest("sorry, can't retrieve your azure storage details from appsettings.js, make sure that you add azure storage details there");

                if (storageConfig.ImageContainer == string.Empty)

                    return BadRequest("Please provide a name for your image container in the azure blob storage");

                foreach (var formFile in files)
                {
                    if (formFile.Length > 0)
                    {
                        Action<Double> progressCallback = delegate (double progress)
                        {
                            _hubContext.Clients.All.SendAsync("ShowProgress", progress);
                        };

                        using (Stream stream = formFile.OpenReadStream())
                        {
                            uploadedUrl = await StorageHelper.UploadFileToStorage(stream, formFile.FileName, storageConfig, progressCallback);
                        }
                    }
                }

                if (uploadedUrl != null)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", "John", uploadedUrl);
                    return new AcceptedResult();
                }
                else
                    return BadRequest("Look like the image couldnt upload to the storage");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }        
    }
}