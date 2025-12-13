namespace Paperless.Services.Models.DTOs
{
    public class DocumentDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string OcrResult { get; set; } = string.Empty;
        public string SummaryResult { get; set; } = string.Empty;
    }
}
