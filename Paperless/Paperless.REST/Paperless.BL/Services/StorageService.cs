using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Paperless.BL.Exceptions;
using Paperless.Services;
using Paperless.Services.Configurations;
using System.Threading.Tasks;

namespace Paperless.BL.Services
{
    public class StorageService (
        IOptions<MinIOConfig> config,
        ILogger<StorageService> logger
    ) {
        private readonly IMinioClient _minIO = new MinioClient()
            .WithEndpoint(config.Value.Endpoint)
            .WithCredentials(config.Value.Username, config.Value.Password)
            .Build();
            
        private readonly string bucketName = config.Value.BucketName;
        private ILogger<StorageService> _logger = logger;

        // TODO: maybe handle this differently
        public async Task UploadDocumentToStorageAsync(Guid id, Stream content)
        {
            try
            {
                string bucketName = "documents";
                await CreateBucketIfNotExistsAsync(bucketName);

                await _minIO.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject($"{id}.pdf")
                    .WithStreamData(content)
                    .WithObjectSize(content.Length)
                    .WithContentType("application/pdf") 
                );

                _logger.LogInformation(
                    "Uploaded document with ID {id} to MinIO Bucket {BucketName} successfully.", 
                    id, 
                    bucketName
                );
            }
            catch (Exception ex) {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "POST", "Business", "publishing to MinIO failing."
                );
                throw new MinIOException($"Failed to publish document {id} to Storage.", ex);
            }
        }

        private async Task CreateBucketIfNotExistsAsync(string bucketName)
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
            bool found = await _minIO.BucketExistsAsync(bucketExistsArgs);

            if (!found)
            {
                var makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
                await _minIO.MakeBucketAsync(makeBucketArgs);
            }
        }
    }
}
