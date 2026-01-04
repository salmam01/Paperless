namespace Paperless.Services.Models.DTOs.Payloads
{
    public class OCRPayload
    {
        public string Id { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = [];
    }
}
