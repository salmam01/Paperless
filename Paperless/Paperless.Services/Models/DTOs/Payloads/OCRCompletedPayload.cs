namespace Paperless.Services.Models.DTOs.Payloads
{
    public class OCRCompletedPayload
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string OCRResult { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = [];
    }
}
