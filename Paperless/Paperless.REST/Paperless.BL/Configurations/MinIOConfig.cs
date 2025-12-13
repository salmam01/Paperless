namespace Paperless.Services.Configurations
{
    public class MinIOConfig
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
    }
}
