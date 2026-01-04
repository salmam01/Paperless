namespace Paperless.API.DTOs
{
    public class ServicesResultDTO
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string OcrResult { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }
}
