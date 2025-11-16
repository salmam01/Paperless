using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Ocr;
using Paperless.Services.Services;
using Paperless.Services.Services.MessageQueue;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Paperless.Services.Workers
{
    //  OCR - Optional Character Recognition :)
    public class OcrWorker : BackgroundService
    {
        private readonly ILogger<OcrWorker> _logger;
        private readonly MQListener _mqListener;
        private readonly MQPublisher _mqPublisher;
        private readonly StorageService _storageService;
        private readonly OcrService _ocrService;

        public OcrWorker(
            ILogger<OcrWorker> logger, 
            [FromKeyedServices("OcrListener")] MQListener mqListener,
            MQPublisher mqPublisher,
            StorageService storageService,
            OcrService ocrService
        ) {
            _mqListener = mqListener;
            _mqPublisher = mqPublisher;
            _storageService = storageService;
            _ocrService = ocrService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //  Stream -> Temp file -> Ghostscript -> Upload -> Delete
            await _mqListener.StartListeningAsync(HandleMessageAsync, stoppingToken);
        }

        private async Task HandleMessageAsync(string id, BasicDeliverEventArgs ea)
        {
            //  Download file (stream) from minIO
            MemoryStream documentContent = await _storageService.DownloadDocumentFromStorageAsync(id);
            if (documentContent.Length <= 0)
                throw new Exception("Document stream is empty.");

            //  Process file to text
            OcrResult result = _ocrService.ProcessPdf(documentContent);

            _logger.LogInformation(
                "Processed document from Message Queue successfully.\n*** Result ***\n{content}",
                result.PdfContent
            );

            //  Send OCR Result to GenAIWorker through RabbitMQ
            await _mqPublisher.PublishOcrResult(id, result.PdfContent);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OCR Worker is stopping...");
            await _mqListener.StopListeningAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
