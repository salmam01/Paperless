namespace Paperless.BL.Configurations
{
    public class ElasticSearchConfig
    {
        public string Url { get; set; } = string.Empty;
        public string Index { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
