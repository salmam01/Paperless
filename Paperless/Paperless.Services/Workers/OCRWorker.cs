using Paperless.Services.Models.DTOs;
using Paperless.Services.Models.OCR;
using Paperless.Services.Services.FileStorage;
using Paperless.Services.Services.MessageQueues;
using Paperless.Services.Services.OCR;
using RabbitMQ.Client.Events;

namespace Paperless.Services.Workers
{
    //  OCR - Optional Character Recognition :)
    public class OCRWorker : BackgroundService
    {
        private readonly ILogger<OCRWorker> _logger;
        private readonly OCRListener _ocrListener;
        private readonly MQPublisher _mqPublisher;
        private readonly StorageService _storageService;
        private readonly OCRService _ocrService;

        public OCRWorker(
            ILogger<OCRWorker> logger, 
            OCRListener ocrListener,
            MQPublisher mqPublisher,
            StorageService storageService,
            OCRService ocrService
        ) {
            _ocrListener = ocrListener;
            _mqPublisher = mqPublisher;
            _storageService = storageService;
            _ocrService = ocrService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //  Stream -> Temp file -> Ghostscript -> Upload -> Delete
            await _ocrListener.StartListeningAsync(HandleMessageAsync, stoppingToken);
        }

        private async Task HandleMessageAsync(string id, BasicDeliverEventArgs ea)
        {
            _logger.LogInformation(
                "Processing OCR request for document ID: {DocumentId}.",
                id
            );

            //  Download file (stream) from minIO
            MemoryStream documentContent = await _storageService.DownloadDocumentFromStorageAsync(id);
            if (documentContent.Length <= 0)
                throw new Exception("Document stream is empty.");

            //  Process file to text
            OCRResult result = _ocrService.ProcessPdf(documentContent);

            _logger.LogInformation(
                "OCR processing completed successfully. Document ID: {DocumentId}, Pages processed: {PageCount}, Content length: {ContentLength} characters.",
                id,
                result.Pages.Count,
                result.PDFContent?.Length ?? 0
            );

            DocumentDTO document = new DocumentDTO
            {
                Id = id,
                Title = _ocrService.ExtractPdfTitle(documentContent),
                OcrResult = result.PDFContent ?? "Error processing document content.",
                SummaryResult = string.Empty
            };

            //  Send OCR Result to GenAIWorker through RabbitMQ
            await _mqPublisher.PublishOcrResult(document);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OCR Worker is stopping...");
            await _ocrListener.StopListeningAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
