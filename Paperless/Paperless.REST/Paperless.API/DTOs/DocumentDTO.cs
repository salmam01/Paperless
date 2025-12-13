namespace Paperless.API.DTOs
{
    public class DocumentDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string Summary { get; set; }
        public string FilePath { get; set; }
        public DateTime CreationDate { get; set; }
        public string Type { get; set; }
        public double Size { get; set; }

        public DocumentDTO() { }

        public DocumentDTO(Guid id, string name, string content, string summary, string filePath, DateTime creationDate, string type, double size)
        {
            Id = id;
            Name = name;
            Content = content;
            Summary = summary;
            FilePath = filePath;
            CreationDate = creationDate;
            Type = type;
            Size = size;
        }
    }
}
