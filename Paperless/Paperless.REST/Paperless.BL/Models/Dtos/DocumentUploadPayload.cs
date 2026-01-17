using Paperless.BL.Models.Domain;

namespace Paperless.BL.Models.Dtos
{
    public class DocumentUploadPayload
    {
        public Guid DocumentId { get; set; }
        public string FileType { get; set; } = string.Empty;
        public List<Category> Categories { get; set; } = [];
    }
}
