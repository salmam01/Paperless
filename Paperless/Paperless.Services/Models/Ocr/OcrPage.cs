using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Models.Ocr
{
    public record OcrPage
    (
        int PageIndex,
        string Text,
        float MeanConfidence
    );
}
