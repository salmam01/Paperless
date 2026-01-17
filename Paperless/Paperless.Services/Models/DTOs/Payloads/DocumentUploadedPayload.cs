namespace Paperless.Services.Models.DTOs.Payloads
{
    public class DocumentUploadedPayload
    {
        public Guid DocumentId { get; set; }
        public string FileType { get; set; } = string.Empty;
        public List<Category> Categories { get; set; } = [];
    }
}
