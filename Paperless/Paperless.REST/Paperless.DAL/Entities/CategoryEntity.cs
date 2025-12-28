using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
