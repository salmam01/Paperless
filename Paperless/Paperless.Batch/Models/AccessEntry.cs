namespace Paperless.Batch.Models
{
    public class AccessEntry
    {
        public Guid DocumentId { get; set; }
        public int AccessCount { get; set; }
    }
}
