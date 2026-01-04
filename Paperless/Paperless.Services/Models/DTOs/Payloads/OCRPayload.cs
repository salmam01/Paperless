namespace Paperless.Services.Models.DTOs.Payloads
{
    public class OCRPayload
    {
        public Guid DocumentId { get; set; }
        public List<Category> Categories { get; set; } = [];
    }
}
