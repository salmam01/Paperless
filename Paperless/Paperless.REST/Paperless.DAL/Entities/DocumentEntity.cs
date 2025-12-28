using System.ComponentModel.DataAnnotations;

namespace Paperless.DAL.Entities
{
    public class DocumentEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        //  Content
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? Content { get; set; }
        [Required]
        public string? Summary { get; set; }

        //  Storage
        [Required]
        public string? FilePath { get; set; }

        //  Meta Data
        [Required]
        public DateTime CreationDate { get; set; }
        [Required]
        public string? Type { get; set; }
        [Required]
        public double Size { get; set; }

        [Required]
        public Guid CategoryId { get; set; }
        [Required]
        public CategoryEntity? Category { get; set; }
    }
}
