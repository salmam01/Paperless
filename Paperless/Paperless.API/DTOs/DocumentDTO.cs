namespace Paperless.API.DTOs
{
    public class DocumentDTO
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Content { get; set; }
        public string? Summary { get; set; }
        public DateTime CreationDate { get; set; }
        public string? Type { get; set; }
        public double Size { get; set; }
    }
}
