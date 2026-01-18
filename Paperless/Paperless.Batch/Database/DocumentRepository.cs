using Npgsql;
using Paperless.Batch.Models;
using System.Data;

namespace Paperless.Batch.Database
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly DatabaseConnection _dbConnection;
        private readonly ILogger<DocumentRepository> _logger;

        public DocumentRepository(DatabaseConnection dbConnection, ILogger<DocumentRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        //  Store incoming Access Data inside the Postgres Database
        public async Task<bool> UpdateDocumentsAsync(AccessEntryList accessEntryList)
        {
            const string query = """
                INSERT INTO "DailyAccessLogs" ("Id", "DocumentId", "AccessDate", "AccessCount")
                VALUES (@id, @documentId, @accessDate, @count)
            """;

            await using NpgsqlConnection connection = await _dbConnection.OpenConnection();
            await using (NpgsqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    if (connection == null || connection.State != ConnectionState.Open)
                        return false;

                    await using NpgsqlCommand command = new(query, connection, transaction);
                    foreach (AccessEntry entry in accessEntryList.AccessEntries) {

                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("id", Guid.NewGuid());
                        command.Parameters.AddWithValue("documentId", entry.DocumentId);
                        command.Parameters.AddWithValue("accessDate", accessEntryList.AccessDate.ToDateTime(TimeOnly.MinValue));
                        command.Parameters.AddWithValue("count", entry.AccessCount);

                        await command.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to update access data for {Date}", accessEntryList.AccessDate);
                    return false;
                }
            }
        }
    }
}
