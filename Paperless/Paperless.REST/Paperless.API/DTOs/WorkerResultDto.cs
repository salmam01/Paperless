namespace Paperless.API.DTOs
{
    public class WorkerResultDto
    {
        public string Id { get; set; } = string.Empty;
        public string OcrResult { get; set; } = string.Empty;
        public string SummaryResult { get; set; } = string.Empty;
    }
}
