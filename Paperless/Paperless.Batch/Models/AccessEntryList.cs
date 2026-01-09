namespace Paperless.Batch.Models
{
    public class AccessEntryList
    {
        public DateTime AccessDate { get; set; }
        public List<AccessEntry> AccessEntries { get; set; } = [];
    }
}
