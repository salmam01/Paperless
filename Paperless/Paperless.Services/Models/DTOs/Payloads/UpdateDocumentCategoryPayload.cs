namespace Paperless.Services.Models.DTOs.Payloads
{
    public class UpdateDocumentCategoryPayload
    {
        public Guid DocumentId { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}
