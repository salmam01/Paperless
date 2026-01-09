using Microsoft.Extensions.Options;
using Paperless.Batch.Configuration;
using Paperless.Batch.Models;
using System.Xml.Serialization;

namespace Paperless.Batch
{
    public class AccessDataBatchProcessor
    {
        private readonly ILogger<AccessDataBatchProcessor> _logger;
        private readonly string _inputFolder;
        private readonly string _archiveFolder;
        private readonly string _searchPattern;

        public AccessDataBatchProcessor(
            ILogger<AccessDataBatchProcessor> logger,
            IOptions<AccessDataConfiguration> config,
            string basePath
        ) {
            _logger = logger;

            _inputFolder = Path.Combine(basePath, config.Value.InputFolder);
            _archiveFolder = Path.Combine(basePath, config.Value.ArchiveFolder);
            _searchPattern = config.Value.SearchPattern;
        }

        //  multiple files, each have a name
        //  each file has multiple entries and a filename (extract date from filename)
        //  each entry has an id and access count
        public List<AccessEntryList> StartProcessing()
        {
            //  Create new directory if it doesn't exist
            Directory.CreateDirectory(_inputFolder);
            Directory.CreateDirectory(_archiveFolder);

            string[] xmlFiles = Directory.GetFiles(_inputFolder, _searchPattern);
            List<AccessEntryList> files = [];

            if (xmlFiles.Length == 0)
            {
                _logger.LogWarning("No Access Files to process.");
                return files;
            }

            _logger.LogInformation(
                "Found {Count} Access Files for batch processing.",
                xmlFiles.Length
            );

            foreach(string file in xmlFiles)
            {
                List<AccessEntry> entries = DeserializeFile(file);
                DateTime accessDate = ExtractFileDate(file);

                files.Add(new AccessEntryList
                {
                    AccessDate = accessDate,
                    AccessEntries = entries
                });
            }
                
            return files;
        }

        private List<AccessEntry> DeserializeFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            List<AccessEntry> entries = new();

            try
            {
                XmlSerializer serializer = new(typeof(List<AccessEntry>));

                using (StreamReader reader = new(filePath))
                {
                    entries = (List<AccessEntry>)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Failed to parse file {fileName} due to an error",
                    fileName
                );
            }

            return entries;
        }

        private DateTime ExtractFileDate(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string[] substrings = fileName.Split('_');

            DateTime.TryParse(
                substrings[1],
                out DateTime accessDate
            );

            return accessDate;
        }
    }
}
