using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Ocr;
using Paperless.Services.Services;
using System.Text;

namespace Paperless.Services.Tests
{
    public class OCRServiceTests
    {
        private readonly Mock<ILogger<OcrService>> _loggerMock;
        private readonly Mock<IOptions<OcrConfig>> _configMock;
        private readonly OcrConfig _testConfig;

        public OCRServiceTests()
        {
            _loggerMock = new Mock<ILogger<OcrService>>();
            _configMock = new Mock<IOptions<OcrConfig>>();
            
            _testConfig = new OcrConfig
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

            _configMock.Setup(x => x.Value).Returns(_testConfig);
        }

        [Fact]
        public void can_create_service()
        {
            OcrService service = new OcrService(_configMock.Object, _loggerMock.Object);
            Assert.NotNull(service);
        }

        [Fact]
        public void throws_when_stream_is_empty()
        {
            OcrService service = new OcrService(_configMock.Object, _loggerMock.Object);
            MemoryStream emptyStream = new MemoryStream();
            Assert.ThrowsAny<Exception>(() => service.ConvertPdfToImage(emptyStream));
        }

        [Fact]
        public void logs_something_when_converting_pdf()
        {
            OcrService service = new OcrService(_configMock.Object, _loggerMock.Object);
            MemoryStream emptyStream = new MemoryStream();
            
            // will fail but that's fine
            try
            {
                service.ConvertPdfToImage(emptyStream);
            }
            catch
            {
                // whatever
            }
            
            // check if it actually logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Converting PDF Stream to an image")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void throws_when_processing_pdf_with_empty_stream()
        {
            OcrService service = new OcrService(_configMock.Object, _loggerMock.Object);
            MemoryStream emptyStream = new MemoryStream();
            Assert.ThrowsAny<Exception>(() => service.ProcessPdf(emptyStream));
        }

        [Fact]
        public void logs_something_when_processing_pdf()
        {
            OcrService service = new OcrService(_configMock.Object, _loggerMock.Object);
            MemoryStream emptyStream = new MemoryStream();

            // will fail but don't care
            try
            {
                service.ProcessPdf(emptyStream);
            }
            catch
            {
                // as expected
            }

            // check if log was called - should log "Extracting text from image"
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Extracting text from image")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void has_reasonable_defaults()
        {
            OcrConfig config = new OcrConfig();
            
            Assert.Equal("deu+eng", config.DefaultLanguage);
            Assert.Equal("LstmOnly", config.DefaultOem);
            Assert.Equal("Auto", config.DefaultPsm);
            Assert.Equal(300, config.DefaultDpi);
            Assert.True(config.UseDeskew);
            Assert.True(config.UseAdaptiveThreshold);
            Assert.True(config.UseSharpen);
            Assert.Equal(50, config.MaxPages);
        }

        [Fact]
        public void can_be_configured()
        {
            OcrConfig config = new OcrConfig
            {
                DefaultLanguage = "eng",
                DefaultOem = "TesseractOnly",
                DefaultPsm = "SingleBlock",
                DefaultDpi = 600,
                UseDeskew = false,
                UseAdaptiveThreshold = false,
                UseSharpen = false,
                MaxPages = 10
            };

            // check if values were actually set
            Assert.Equal("eng", config.DefaultLanguage);
            Assert.Equal("TesseractOnly", config.DefaultOem);
            Assert.Equal("SingleBlock", config.DefaultPsm);
            Assert.Equal(600, config.DefaultDpi);
            Assert.False(config.UseDeskew);
            Assert.False(config.UseAdaptiveThreshold);
            Assert.False(config.UseSharpen);
            Assert.Equal(10, config.MaxPages);
        }
    }
}
