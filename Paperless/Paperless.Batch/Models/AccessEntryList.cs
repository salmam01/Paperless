namespace Paperless.Batch.Models
{
    public class AccessEntryList
    {
        public DateOnly AccessDate { get; set; }
        public List<AccessEntry> AccessEntries { get; set; } = [];
    }
}
