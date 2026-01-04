using System;
namespace Paperless.BL.Models.Domain
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }
        public Guid? CategoryId { get; set; }
        public string? Content { get; set; }
        public string? Summary { get; set; }
        public string? FilePath { get; set; }
        public DateTime CreationDate { get; set; }
        public string? Type { get; set; }
        public double Size { get; set; }

        public Document() { }
        public Document(
            Guid id, 
            string name,
            Guid? categoryId,
            string content, 
            string summary, 
            string filePath, 
            DateTime creationDate, 
            string type, 
            double size
        ) {
            Id = id;
            Name = name;
            CategoryId = categoryId;
            Content = content;
            Summary = summary;
            FilePath = filePath;
            CreationDate = creationDate;
            Type = type;
            Size = size;
        }
    }
}
