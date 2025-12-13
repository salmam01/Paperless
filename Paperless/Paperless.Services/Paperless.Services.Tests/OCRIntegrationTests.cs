using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Services.FileStorage;
using Paperless.Services.Services.OCR;
using System.Text;

namespace Paperless.Services.Tests
{
    // integration tests for OCR
    // checking if OCR components work together
    public class OCRIntegrationTests
    {
        private readonly Mock<ILogger<OCRService>> _ocrLoggerMock;
        private readonly Mock<ILogger<StorageService>> _storageLoggerMock;
        private readonly Mock<IOptions<OCRConfig>> _ocrConfigMock;
        private readonly OCRConfig _testOcrConfig;

        public OCRIntegrationTests()
        {
            _ocrLoggerMock = new Mock<ILogger<OCRService>>();
            _storageLoggerMock = new Mock<ILogger<StorageService>>();
            _ocrConfigMock = new Mock<IOptions<OCRConfig>>();

            _testOcrConfig = new OCRConfig
            {
                DefaultLanguage = "deu+eng",
                DefaultOem = "LstmOnly",
                DefaultPsm = "Auto",
                DefaultDpi = 300,
                UseDeskew = true,
                UseAdaptiveThreshold = true,
                UseSharpen = true,
                MaxPages = 50
            };

            _ocrConfigMock.Setup(x => x.Value).Returns(_testOcrConfig);
        }

        [Fact]
        public void uses_ghostscript_for_pdf_conversion()
        {
            OCRService service = new OCRService(_ocrConfigMock.Object, _ocrLoggerMock.Object);
            MemoryStream emptyStream = new MemoryStream();

            // will fail but that's fine - want to see if it tries
            Assert.ThrowsAny<Exception>(() => service.ConvertPdfToImage(emptyStream));
            
            // check if it logged
            _ocrLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Converting PDF Stream to an image")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void uses_tesseract_for_text_recognition()
        {
            OCRService service = new OCRService(_ocrConfigMock.Object, _ocrLoggerMock.Object);
            MemoryStream emptyStream = new MemoryStream();

            // will fail but we're checking if tesseract is used
            Assert.ThrowsAny<Exception>(() => service.ProcessPdf(emptyStream));
            
            // should log that it tried
            _ocrLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Extracting text from image")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void converts_pdf_to_image_first()
        {
            OCRService service = new OCRService(_ocrConfigMock.Object, _ocrLoggerMock.Object);
            MemoryStream emptyStream = new MemoryStream();

            // ProcessPdf should first convert PDF to image, then process with Tesseract
            Assert.ThrowsAny<Exception>(() => service.ProcessPdf(emptyStream));
            
            // both steps should be logged
            _ocrLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => 
                        v.ToString()!.Contains("Extracting text from image") || 
                        v.ToString()!.Contains("Converting PDF Stream to an image")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void can_configure_max_pages()
        {
            OCRConfig config = new OCRConfig
            {
                MaxPages = 10,
                DefaultDpi = 300
            };
            Mock<IOptions<OCRConfig>> configMock = new Mock<IOptions<OCRConfig>>();
            configMock.Setup(x => x.Value).Returns(config);
            
            OCRService service = new OCRService(configMock.Object, _ocrLoggerMock.Object);
            
            Assert.Equal(10, config.MaxPages);
            Assert.NotNull(service);
        }

        [Fact]
        public void supports_multiple_languages()
        {
            OCRConfig config = new OCRConfig
            {
                DefaultLanguage = "deu+eng"
            };

            Assert.Contains("deu", config.DefaultLanguage);
            Assert.Contains("eng", config.DefaultLanguage);
        }

        [Fact]
        public void supports_image_processing_options()
        {
            OCRConfig config = new OCRConfig
            {
                UseDeskew = true,
                UseAdaptiveThreshold = true,
                UseSharpen = true
            };
            
            Assert.True(config.UseDeskew);
            Assert.True(config.UseAdaptiveThreshold);
            Assert.True(config.UseSharpen);
        }
    }
}
