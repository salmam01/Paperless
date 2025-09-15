using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.DAL.Entities
{
    //  Document should be stored somewhere on the filesystem (with reference to ID)
    public class DocumentEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? Content { get; set; }
        [Required]
        public string? Summary { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }
        [Required]
        public string? Type { get; set; }
        public double Size { get; set; }
    }
}
