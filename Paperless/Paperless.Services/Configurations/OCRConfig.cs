using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Configurations
{
    public class OcrConfig
    {
        public string DefaultLanguage { get; set; } = "deu+eng";

        //  Engine mode
        public string DefaultOem { get; set; } = "LstmOnly";

        //  Page segmentation mode
        public string DefaultPsm { get; set; } = "Auto";

        //  Rasterization DPI for PDF pages
        public int DefaultDpi { get; set; } = 300;

        //  Deskewing means correcting a tilt or a slant
        public bool UseDeskew { get; set; } = true;

        //  Adaptive thresholding to fix uneven lighting
        public bool UseAdaptiveThreshold { get; set; } = true;

        //  Light sharpening for improving edge contrast
        public bool UseSharpen { get; set; } = true;

        public int MaxPages { get; set; } = 50;
    }
}
