using Microsoft.Extensions.Options;
using Minio;
using Paperless.Services.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services
{
    public class StorageService
    {
        public readonly IMinioClient Client;
        public StorageService(IOptions<MinIOConfig> config) 
        {
            Client = new MinioClient()
                .WithEndpoint(config.Value.Endpoint)
                .WithCredentials(config.Value.Username, config.Value.Password)
                .Build();
        }
    }
}
