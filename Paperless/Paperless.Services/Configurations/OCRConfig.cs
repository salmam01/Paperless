using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Configurations
{
    public class OCRConfig
    {
        //  Language codes used by Tesseract (e.g. "deu", "eng", "deu+eng").
        public string DefaultLanguage { get; set; } = "deu+eng";

        //  OCR engine mode, mapped to Tesseract.EngineMode.
        public string DefaultOem { get; set; } = "LstmOnly";

        //  Page segmentation mode, mapped to Tesseract.PageSegMode.
        public string DefaultPsm { get; set; } = "Auto";

        //  Rasterization DPI for PDF pages (300–400 is typical).
        public int DefaultDpi { get; set; } = 300;

        //  Enables deskewing of scanned pages.
        public bool UseDeskew { get; set; } = true;

        //  Enables adaptive thresholding for uneven lighting.
        public bool UseAdaptiveThreshold { get; set; } = true;

        //  Applies light sharpening to improve edge contrast.
        public bool UseSharpen { get; set; } = true;

        //  Maximum number of pages processed per document.
        public int MaxPages { get; set; } = 50;
    }
}
