using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using Minio;
using Paperless.Services.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Services
{
    public class MinIOService
    {
        public readonly IMinioClient Client;
        public MinIOService(IOptions<MinIOConfig> config) 
        {
            Client = new MinioClient()
                .WithEndpoint(config.Value.Endpoint)
                .WithCredentials(config.Value.Username, config.Value.Password)
                .Build();
        }
    }
}
