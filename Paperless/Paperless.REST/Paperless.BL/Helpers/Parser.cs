using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Paperless.BL.Models;
using Paperless.BL.Exceptions;
using System.Text;

namespace Paperless.BL.Helpers
{
    public class Parser
    {
        private readonly ILogger<Parser> _logger;

        public Parser(ILogger<Parser> logger)
        {
            _logger = logger;
        }

        public void ParseDocument(Models.Document document, Stream content)
        {
            try
            {
                if (content == null || document == null)
                {
                    _logger.LogWarning("Cannot parse empty stream.");
                    return;
                }

                MemoryStream contentMs = new();
                content.CopyTo(contentMs);
                content.Position = 0;
                contentMs.Position = 0;

                ParseByDocumentType(document, contentMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}. Document ID: {id}",
                    "POST", "Business", "parsing document failing.", document.Id
                );
                throw new ServiceException("An error occurred while parsing document.", ExceptionType.Parsing);
            }
        }

        private void ParseByDocumentType (Models.Document document, MemoryStream contentMs)
        {
            switch (document.Type)
            {
                case "DOCX":
                    ParseDocx(document, contentMs);
                    break;

                case "TXT":
                    ParseTxt(document, contentMs);
                    break;

                default:
                    _logger.LogWarning(
                        "Cannot parse document with ID {id} due to unsupported document type: {fileType}.",
                        document.Id,
                        document.Type
                    );
                    throw new Exception("An error occurred while parsing document.");
            }
        }

        private void ParseDocx(Models.Document document, MemoryStream contentMs)
        {
            using (WordprocessingDocument wordprocessingDocument = WordprocessingDocument.Open(contentMs, true))
            {
                Body? body = wordprocessingDocument.MainDocumentPart?.Document.Body;
                if (body != null)
                {
                    string text = string.Concat(body.Descendants<Text>().Select(txt => txt.Text));
                    document.Content = text;

                    
                    _logger.LogInformation("Parsed document of type {fileType} successfully:\n{text}", document.Type, text);
                }
                else
                {
                    _logger.LogWarning(
                        "Cannot parse document with ID {id} due to an empty DOCX body.",
                        document.Id
                    );
                    throw new Exception("An error occurred while parsing document.");
                }
            }
        }

        private void ParseTxt(Models.Document document, MemoryStream contentMs)
        {
            byte[] buffer = contentMs.ToArray();
            document.Content = Encoding.UTF8.GetString(buffer);
        }

    }
}
