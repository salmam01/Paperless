using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Models.DTOs.Payloads
{
    public class OCRCompletedPayload
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string OCRResult { get; set; } = string.Empty;
        public CategoryList CategoryList { get; set; } = new();
    }
}
