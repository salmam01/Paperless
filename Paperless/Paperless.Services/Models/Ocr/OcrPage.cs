namespace Paperless.Services.Models.OCR
{
    public record OCRPage
    (
        int PageIndex,
        string Text,
        float MeanConfidence
    );
}
