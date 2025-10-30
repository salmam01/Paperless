using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Models.OCR
{
    public record class PdfImageResult
    (
        List<MagickImage> Images,       //  actual MagickImage objects to be fed into Tesseract.
        List<string> Thumbnails,        //  all preview images (base64-encoded)
        List<string> FullImages         //  resized “full-page” preview images (base64-encoded)
    );
}
