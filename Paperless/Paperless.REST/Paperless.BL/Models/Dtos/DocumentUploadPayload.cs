using Paperless.BL.Models.Domain;

namespace Paperless.BL.Models.Dtos
{
    public class DocumentUploadPayload
    {
        public Guid Id { get; set; }
        public List<Category> Categories { get; set; } = [];
    }
}
