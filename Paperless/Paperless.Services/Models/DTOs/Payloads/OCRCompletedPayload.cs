namespace Paperless.Services.Models.DTOs.Payloads
{
    public class OCRCompletedPayload
    {
        public Guid DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OCRResult { get; set; } = string.Empty;
        public List<Category> Categories { get; set; } = [];
    }
}
