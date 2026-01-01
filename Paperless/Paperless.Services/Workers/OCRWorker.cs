using Paperless.Services.Models.DTOs;
using Paperless.Services.Models.OCR;
using Paperless.Services.Services.FileStorage;
using Paperless.Services.Services.Messaging.Publishers;
using Paperless.Services.Services.Messaging.Base;
using Paperless.Services.Services.OCR;
using RabbitMQ.Client.Events;
using Paperless.Services.Services.Messaging.Listeners;
using Paperless.Services.Models.DTOs.Payloads;

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

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            //  Stream -> Temp file -> Ghostscript -> Upload -> Delete
            await _ocrListener.StartListeningAsync(HandleMessageAsync, cancellationToken);
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
        {
            OCRPayload payload = _ocrListener.ProcessPayload(ea);

            _logger.LogInformation(
                "Processing OCR request for document ID: {DocumentId}.",
                payload.Id
            );

            //  Download file (stream) from minIO
            MemoryStream documentContent = await _storageService.DownloadDocumentAsync(payload.Id);
            if (documentContent.Length <= 0)
                throw new Exception("Document stream is empty.");

            //  Process file to text
            OCRResult result = _ocrService.ProcessPdf(documentContent);

            _logger.LogInformation(
                "OCR processing completed successfully. Document ID: {DocumentId}, Pages processed: {PageCount}, Content length: {ContentLength} characters.",
                payload.Id,
                result.Pages.Count,
                result.PDFContent?.Length ?? 0
            );

            OCRCompletedPayload ocrCompletedPayload = new OCRCompletedPayload
            {
                Id = payload.Id,
                Title = _ocrService.ExtractPdfTitle(documentContent),
                OCRResult = result.PDFContent ?? "Error processing document content.",
                CategoryList = payload.CategoryList,
            };

            //  Send OCR Result to GenAIWorker through RabbitMQ
            await _mqPublisher.PublishOcrResult(ocrCompletedPayload);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OCR Worker is stopping...");
            await _ocrListener.StopListeningAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
