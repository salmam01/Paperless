using Paperless.Services.Models.OCR;
using Paperless.Services.Services.FileStorage;
using Paperless.Services.Services.Messaging.Publishers;
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
            _logger.LogInformation(
                "{WorkerType} Worker is starting ...",
                "OCR"
            );

            await _ocrListener.StartListeningAsync(HandleMessageAsync, cancellationToken);
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
        {
            try
            {
                //  Deserialize incoming payload
                OCRPayload payload = _ocrListener.ProcessPayload(ea);

                _logger.LogInformation(
                    "Processing {RequestType} request for Document with ID {Id}.",
                    "OCR",
                    payload.Id
                );

                //  Download file (stream) from MinIO
                MemoryStream documentContent = await _storageService.DownloadDocumentAsync(payload.Id);
                if (documentContent.Length <= 0)
                    throw new Exception("Document stream is empty.");

                //  Process file to text using OCR
                OCRResult result = _ocrService.ProcessPdf(documentContent);
                string title = _ocrService.ExtractPdfTitle(documentContent);

                OCRCompletedPayload ocrCompletedPayload = new OCRCompletedPayload
                {
                    Id = payload.Id,
                    Title = title,
                    OCRResult = result.PDFContent ?? "Error processing document content.",
                    Categories = payload.Categories
                };

                _logger.LogInformation(
                    "{RequestType} processing completed successfully.\n" +
                    "Document ID: {Id}, Title: {Title}, Pages processed: {PageCount}, Content length: {ContentLength} characters.",
                    "OCR",
                    ocrCompletedPayload.Id,
                    ocrCompletedPayload.Title,
                    result.Pages.Count,
                    result.PDFContent?.Length ?? 0
                );

                //  Send OCR Result to Summary Worker through RabbitMQ
                await _mqPublisher.PublishOcrResult(ocrCompletedPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{RequestType} processing failed inside {WorkerType} Worker." +
                    "\nError: {ErrorMessage}",
                    "OCR",
                    "OCR",
                    ex.Message
                );
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "{WorkerType} Worker is stopping...",
                "OCR"
            );

            await _ocrListener.StopListeningAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
