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

        public async Task UploadDocumentToStorageAsync(Guid id, string fileType, Stream content)
        {
            try
            {
                await CreateBucketIfNotExistsAsync(_bucketName);

                string ft = fileType.ToLowerInvariant();

                await _minIO.PutObjectAsync(
                    new PutObjectArgs()
                        .WithBucket(_bucketName)
                        .WithObject($"{id}.{ft}")
                        .WithStreamData(content)
                        .WithObjectSize(content.Length)
                        .WithContentType($"application/{ft}") 
                );

                _logger.LogInformation(
                    "Uploaded document with ID {id} to MinIO Bucket {BucketName} successfully.", 
                    id,
                    _bucketName
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

        public async Task DeleteDocumentAsync(Guid id, string fileType)
        {
            try
            {
                await CreateBucketIfNotExistsAsync(_bucketName);

                string ft = fileType.ToLowerInvariant();

                await _minIO.RemoveObjectAsync(
                    new RemoveObjectArgs()
                        .WithBucket(_bucketName)
                        .WithObject($"{id}.{ft}")
                );

                _logger.LogInformation(
                    "Deleted document with ID {id} from MinIO Bucket {BucketName} successfully.",
                    id,
                    _bucketName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document/{id} failed in {layer} Layer due to {reason}.",
                    "DELETE", id, "Business", "deletion from MinIO failing."
                );
                throw new MinIOException($"Failed to delete document {id} from Storage.", ex);
            }
        }

        public async Task DeleteDocumentsAsync()
        {
            try
            {
                await CreateBucketIfNotExistsAsync(_bucketName);

                ListObjectsArgs listArgs = new ListObjectsArgs()
                    .WithBucket(_bucketName);

                List<string> objectKeys = [];
                await foreach (var obj in _minIO.ListObjectsEnumAsync(listArgs))
                {
                    objectKeys.Add(obj.Key);
                }

                if (objectKeys.Count == 0)
                {
                    _logger.LogWarning(
                        "Cannot delete documents from MinIO because the Bucket {BucketName} is empty.",
                        _bucketName
                    );
                    return;
                }

                RemoveObjectsArgs removeArgs = new RemoveObjectsArgs()
                    .WithBucket(_bucketName)
                    .WithObjects(objectKeys);

                await _minIO.RemoveObjectsAsync(removeArgs);

                _logger.LogInformation(
                    "Deleted all documents from MinIO Bucket {BucketName} successfully.",
                    _bucketName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "DELETE", "Business", "deletion from MinIO failing."
                );
                throw new MinIOException($"Failed to delete documents from Storage.", ex);
            }
        }

        private async Task CreateBucketIfNotExistsAsync(string bucketName)
        {
            bool found = await _minIO.BucketExistsAsync(
                new BucketExistsArgs()
                    .WithBucket(bucketName));

            if (!found)
            {
                await _minIO.MakeBucketAsync(
                    new MakeBucketArgs()
                        .WithBucket(bucketName));
            }
        }
    }
}
