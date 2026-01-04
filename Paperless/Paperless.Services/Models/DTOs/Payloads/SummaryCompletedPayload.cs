namespace Paperless.Services.Models.DTOs.Payloads
{
    public class SummaryCompletedPayload
    {
        public Guid DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string OCRResult { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }
}
