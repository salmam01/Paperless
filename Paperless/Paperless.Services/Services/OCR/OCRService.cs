using ImageMagick;
using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.OCR;
using System.Text;
using Tesseract;
using iText.Kernel.Pdf;


namespace Paperless.Services.Services.OCR
{
    public class OCRService
    {
        private readonly OCRConfig _config;
        private readonly ILogger<OCRService> _logger;

        public OCRService(IOptions<OCRConfig> config, ILogger<OCRService> logger)
        {
            _config = config.Value;
            _logger = logger;
        }
        
        public OCRResult ProcessPdf(MemoryStream documentContent)
        {
            _logger.LogInformation(
                "Processing PDF for OCR. Document size: {DocumentSize} bytes.",
                documentContent?.Length ?? 0
            );

            List<MagickImage> imageResult = ConvertPdfToImage(documentContent);

            var engineConfig = Enum.Parse<EngineMode>(_config.DefaultOem);

            using TesseractEngine engine = new("/app/tessdata", _config.DefaultLanguage, Enum.Parse<EngineMode>(_config.DefaultOem, true))
            {
                DefaultPageSegMode = Enum.Parse<PageSegMode>(_config.DefaultPsm, true)
            };

            //  Each page of the original file is converted seperately
            List<OCRPage> pages = [];

            for (int i = 0; i < imageResult.Count; i++)
            {
                //  Convert each preprocessed Magick image to a PNG stream
                using MagickImage image = imageResult[i];
                using MemoryStream imageStream = new MemoryStream();
                image.Write(imageStream, MagickFormat.Png);
                //  Convert the stream to a Pix image (tesseract internal image format)
                using Pix pix = Pix.LoadFromMemory(imageStream.ToArray());

                //  The engine recognizes the letters based on the trained language data
                using Page page = engine.Process(pix);
                string text = page.GetText();
                //  Tesseracts average certainty for all recognized characters in that page
                float mean = page.GetMeanConfidence();

                pages.Add(new OCRPage(i + 1, text, mean));

                _logger.LogInformation(
                    "======== Page {PageIndex} of PDF. Confidence: {Confidence:P1}, Text length: {TextLength} characters.",
                    pages[i].PageIndex,
                    pages[i].MeanConfidence,
                    text?.Length ?? 0
                );
            }

            StringBuilder fileContent = new();
            foreach (OCRPage page in pages)
            {
                fileContent.AppendLine(page.Text);
                fileContent.AppendLine();
            }

            string finalContent = fileContent.ToString();
            _logger.LogInformation(
                "OCR processing completed. Total pages: {PageCount}, Total content length: {ContentLength} characters.",
                pages.Count,
                finalContent.Length
            );

            return new OCRResult(pages, finalContent);
        }

        //  Convert the content Stream to Magick.NET 
        //  Magick.NET wraps Ghostscript internally to parse PDFs to images
        public List<MagickImage> ConvertPdfToImage(MemoryStream documentContent)
        {
            _logger.LogInformation(
                "Converting PDF stream to images. Document size: {DocumentSize} bytes, DPI: {Dpi}, Max pages: {MaxPages}.",
                documentContent?.Length ?? 0,
                _config.DefaultDpi,
                _config.MaxPages
            );

            List<MagickImage> images = [];

            try {
                MagickReadSettings settings = new()
                {
                    Density = new Density(_config.DefaultDpi, _config.DefaultDpi),
                    ColorSpace = ColorSpace.Gray
                };
                //  Use CropBox to exclude margins
                settings.SetDefine(MagickFormat.Pdf, "use-cropbox", "true");

                //  Read every page of the PDF as MagickImage using the previously defined settings
                using MagickImageCollection imageCollection = [];
                imageCollection.Read(documentContent, settings);

                int pageCount = Math.Min(imageCollection.Count, Math.Max(1, _config.MaxPages));
                for (int i = 0; i < pageCount; i++)
                {
                    //  Clone the image to apply preprocessing steps 
                    using var frame = (MagickImage)imageCollection[i].Clone();
                    
                    //  Use Deskew to straighten scanned images
                    if (_config.UseDeskew) frame.Deskew(new Percentage(40));

                    //  Improve image contrast to detect text more easily
                    frame.ContrastStretch(new Percentage(0.1), new Percentage(0.1));

                    //  Enhance text outlines
                    if (_config.UseSharpen) frame.AdaptiveSharpen(1, 1);

                    if (_config.UseAdaptiveThreshold)
                        frame.AdaptiveThreshold(15, 15, 5);
                    else
                    {
                        frame.ReduceNoise();
                        frame.BlackThreshold(new Percentage(50));
                        frame.WhiteThreshold(new Percentage(50));
                    }

                    frame.Format = MagickFormat.Png;
                    //  Higher DPI (resolution) means sharper text for OCR
                    frame.Density = new Density(_config.DefaultDpi);

                    images.Add((MagickImage)frame.Clone());
                }
            } catch (Exception ex) {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "POST", "Services", "an error while processing the PDF to an Image"
                );
                throw;
            }

            return images;
        }

        
        public string ExtractPdfTitle(MemoryStream documentContent)
        {
            documentContent.Position = 0;
            using PdfReader reader = new(documentContent);
            using PdfDocument pdfDocument = new(reader);
            var info = pdfDocument.GetDocumentInfo();

            return info.GetTitle() ?? "Untitled";
        }
    }
}
