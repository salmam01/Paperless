namespace Paperless.Services.Models.OCR
{
    public record OCRResult
    (
        List<OCRPage> Pages,
        string PDFContent
    );
}
