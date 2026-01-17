using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Models.Dtos
{
    public class UpdateDocumentCategoryPayload
    {
        public Guid DocumentId { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}
