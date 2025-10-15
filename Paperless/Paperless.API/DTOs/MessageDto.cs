namespace Paperless.API.DTOs
{
    public class MessageDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string Summary { get; set; }

        public MessageDto(Guid id, string name, string content, string summary)
        {
            Id = id;
            Name = name;
            Content = content;
            Summary = summary;
        }
    }
}
