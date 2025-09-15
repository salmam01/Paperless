using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Models
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }
        public string? Content { get; set; }
        public string? Summary { get; set; }
        public DateTime CreationDate { get; set; }
        public string? Type { get; set; }
        public double Size { get; set; }
    }
}
