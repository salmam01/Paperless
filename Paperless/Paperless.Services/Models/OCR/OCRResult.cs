using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Models.OCR
{
    public record OCRResult
    (
        List<OCRPageResult> Pages,
        string PdfContent
    );
}
