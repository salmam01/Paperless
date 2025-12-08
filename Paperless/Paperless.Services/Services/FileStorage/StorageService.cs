using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Paperless.Services.Configurations;

namespace Paperless.Services.Services.FileStorage
{
    public class StorageService
    {
        private readonly IMinioClient _minIO;
        private readonly string _bucketName;
        private readonly ILogger<StorageService> _logger;
        public StorageService(IOptions<MinIoConfig> config, ILogger<StorageService> logger) 
        {
            _minIO = new MinioClient()
                .WithEndpoint(config.Value.Endpoint)
                .WithCredentials(config.Value.Username, config.Value.Password)
                .Build();
            _bucketName = config.Value.BucketName;

            _logger = logger;
        }

        public async Task<MemoryStream> DownloadDocumentFromStorageAsync(string id)
        {
            _logger.LogInformation(
                "Downloading document from storage. Document ID: {DocumentId}, Bucket: {BucketName}.",
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
                "Document downloaded from storage successfully. Document ID: {DocumentId}, Stream size: {StreamSize} bytes.",
                id,
                stream.Length
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
