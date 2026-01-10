using System.ComponentModel.DataAnnotations;

namespace Paperless.DAL.Entities
{
    public class DailyAccessLogs
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public Guid DocumentId { get; set; }
        public DocumentEntity Document { get; set; }
        public DateOnly AccessDate { get; set; }
        public int AccessCount { get; set; }

    }
}
