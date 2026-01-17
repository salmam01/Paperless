using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace Paperless.Services.Services.Helpers
{
    public class Parser
    {
        private readonly ILogger<Parser> _logger;

        public Parser(ILogger<Parser> logger)
        {
            _logger = logger;
        }

        public string ParseDocument(string fileType, Stream content)
        {
            _logger.LogInformation(
                "Parsing document with Content size: {ContentSize} bytes.",
                content?.Length ?? 0
            );

            string documentContent = string.Empty;
            try
            {
                if (content == null || content.Length <= 0)
                {
                    _logger.LogWarning("Cannot parse empty stream.");
                    return documentContent;
                }

                MemoryStream contentMs = new();
                content.CopyTo(contentMs);
                content.Position = 0;
                contentMs.Position = 0;

                documentContent = ParseByType(fileType, contentMs);

                return documentContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "POST", "Business", "parsing document failing."
                );
                throw;
            }
        }

        private string ParseByType(string fileType, MemoryStream contentMs)
        {
            string documentContent = string.Empty;

            switch (fileType)
            {
                case "DOCX":
                    documentContent = ParseDocx(contentMs);
                    break;

                case "TXT":
                    documentContent = ParseTxt(contentMs);
                    break;

                default:
                    _logger.LogWarning(
                        "Cannot parse document due to unsupported document type: {fileType}.",
                        fileType
                    );
                    break;
            }

            return documentContent;
        }

        private string ParseDocx(MemoryStream contentMs)
        {
            string documentContent = string.Empty;
            using (WordprocessingDocument wordprocessingDocument = WordprocessingDocument.Open(contentMs, true))
            {
                Body? body = wordprocessingDocument.MainDocumentPart?.Document.Body;
                if (body != null)
                {
                    string text = string.Concat(body.Descendants<Text>().Select(txt => txt.Text));
                    documentContent = text;

                    _logger.LogInformation("Parsed document of successfully:\n{text}", text);
                }
                else
                {
                    _logger.LogWarning(
                        "Cannot parse document due to an empty DOCX body."
                    );
                }
            }
            return documentContent;
        }

        private string ParseTxt(MemoryStream contentMs)
        {
            byte[] buffer = contentMs.ToArray();
            return Encoding.UTF8.GetString(buffer);
        }

    }
}
