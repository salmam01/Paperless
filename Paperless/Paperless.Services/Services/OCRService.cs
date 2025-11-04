using ImageMagick;
using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Ocr;
using System.Text;
using Tesseract;


namespace Paperless.Services.Services
{
    public class OcrService
    {
        private readonly OcrConfig _config;
        private readonly ILogger<OcrService> _logger;

        public OcrService(IOptions<OcrConfig> config, ILogger<OcrService> logger)
        {
            _config = config.Value;
            _logger = logger;
        }

        //  Use Tesseract to parse PDF to image
        public OcrResult ProcessPdf(MemoryStream documentContent)
        {
            _logger.LogInformation(
                "Converting temporary image to human readable text..."
            );

            PdfImage imageResult = ConvertPdfToImage(documentContent);

            var engineConfig = Enum.Parse<EngineMode>(_config.DefaultOem);

            using TesseractEngine engine = new("/app/tessdata", _config.DefaultLanguage, Enum.Parse<EngineMode>(_config.DefaultOem, true))
            {
                DefaultPageSegMode = Enum.Parse<PageSegMode>(_config.DefaultPsm, true)
            };

            //  Each page of the original file is converted seperately
            List<OcrPage> pages = [];

            for (int i = 0; i < imageResult.Images.Count; i++)
            {
                using MagickImage image = imageResult.Images[i];
                using MemoryStream imageStream = new MemoryStream();
                image.Write(imageStream, MagickFormat.Png);
                using Pix pix = Pix.LoadFromMemory(imageStream.ToArray());

                using Page page = engine.Process(pix);
                string text = page.GetText();
                float mean = page.GetMeanConfidence();

                pages.Add(new OcrPage(i + 1, text, mean));

                _logger.LogInformation(
                    "Processed Page {pageIndex} with confidence {Confidence:P1}",
                    pages[i].PageIndex,
                    pages[i].MeanConfidence
                );
            }

            StringBuilder fileContent = new();
            foreach(OcrPage page in pages)
            {
                fileContent.AppendLine($"---> Page: {page.PageIndex} (Confidence: {page.MeanConfidence:P1}) <---");
                fileContent.AppendLine(page.Text);
                fileContent.AppendLine();
            }

            return new OcrResult(pages, fileContent.ToString());
        }

        //  Use Magic.NET which uses Ghostscript to parse PDF to image
        public PdfImage ConvertPdfToImage(MemoryStream documentContent)
        {
            _logger.LogInformation(
                "Converting temporary PDF to a temporary image..."
            );

            List<MagickImage> images = [];
            List<string> thumbnails = [];
            List<string> fullImages = [];

            try {
                MagickReadSettings settings = new()
                {
                    Density = new Density(_config.DefaultDpi, _config.DefaultDpi),
                    ColorSpace = ColorSpace.Gray
                };
                settings.SetDefine(MagickFormat.Pdf, "use-cropbox", "true");

                using MagickImageCollection imageCollection = [];
                imageCollection.Read(documentContent, settings);

                int pageCount = Math.Min(imageCollection.Count, Math.Max(1, _config.MaxPages));
                for (int i = 0; i < pageCount; i++)
                {
                    using var frame = (MagickImage)imageCollection[i].Clone();
                    if (_config.UseDeskew) frame.Deskew(new Percentage(40));

                    frame.ContrastStretch(new Percentage(0.1), new Percentage(0.1));

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
                    frame.Density = new Density(_config.DefaultDpi);

                    using var fullImage = (MagickImage)frame.Clone();
                    if (fullImage.Width > 1200) fullImage.Resize(1200, 0);

                    using MemoryStream imageStream = new();
                    fullImage.Write(imageStream, MagickFormat.Png);
                    fullImages.Add($"data:image/png;base64,{Convert.ToBase64String(imageStream.ToArray())}");

                    using var thumbnail = (MagickImage)frame.Clone();
                    thumbnail.Resize(new MagickGeometry("300x"));
                    using MemoryStream thumbnailStream = new();
                    thumbnail.Write(thumbnailStream, MagickFormat.Png);
                    thumbnails.Add($"data:image/png;base64,{Convert.ToBase64String(thumbnailStream.ToArray())}");

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

            return new PdfImage(images, thumbnails, fullImages);
        }
    }
}
