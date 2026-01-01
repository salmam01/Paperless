namespace Paperless.Services.Models.DTOs.Payloads
{
    public class OCRPayload
    {
        public string Id { get; set; } = string.Empty;
        public CategoryList CategoryList { get; set; } = new();
    }
}
