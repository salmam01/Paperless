using System.ComponentModel.DataAnnotations;

namespace Paperless.DAL.Entities
{
    public class CategoryEntity
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string? Name { get; set; }
        public ICollection<DocumentEntity>? Documents { get; set; }
    }
}
