using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Models.Dtos
{
    public class DocumentDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string OcrResult { get; set; } = string.Empty;
        public string SummaryResult { get; set; } = string.Empty;
    }
}
