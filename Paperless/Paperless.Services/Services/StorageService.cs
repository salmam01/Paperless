using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using Minio;
using Minio.DataModel.Args;
using Paperless.Services.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Services
{
    public class StorageService
    {
        private readonly IMinioClient _minIO;
        private readonly string _bucketName;
        private readonly ILogger<StorageService> _logger;
        public StorageService(IOptions<MinIOConfig> config, ILogger<StorageService> logger) 
        {
            _minIO = new MinioClient()
                .WithEndpoint(config.Value.Endpoint)
                .WithCredentials(config.Value.Username, config.Value.Password)
                .Build();
            _bucketName = config.Value.BucketName;

            _logger = logger;
        }

        public async Task<Stream> DownloadDocumentFromStorageAsync(string id)
        {
            _logger.LogInformation(
                "Downloading Document with ID {id} from Storage Bucket {BucketName}",
                id,
                _bucketName
            );
            if (!await CheckIfBucketExists())
            {
                _logger.LogWarning(
                    "Bucket with name {BucketName} does not exist.",
                    _bucketName
                );
                throw new Exception("Bucket not found.");
            }

            MemoryStream stream = new();

            var documentObject = await _minIO.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject($"{id}.pdf")
                    .WithCallbackStream(s => s.CopyTo(stream))
            );

            stream.Position = 0;

            _logger.LogInformation(
                "Document with ID {id} downloaded from Storage successfully.",
                id
            );
            return stream;
        }

        private async Task<bool> CheckIfBucketExists()
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucketName);
            bool found = await _minIO.BucketExistsAsync(bucketExistsArgs);

            return found;
        }
    }
}
