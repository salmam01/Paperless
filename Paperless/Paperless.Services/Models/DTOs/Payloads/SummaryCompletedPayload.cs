namespace Paperless.Services.Models.DTOs.Payloads
{
    public class SummaryCompletedPayload
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string OCRResult { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }
}
