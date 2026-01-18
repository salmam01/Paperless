using Npgsql;
using Paperless.Batch.Models;
using System.Data;

namespace Paperless.Batch.Database
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly DatabaseConnection _dbConnection;
        public DocumentRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        //  Store incoming Access Data inside the Postgres Database
        public async Task<bool> UpdateDocumentsAsync(AccessEntryList accessEntryList)
        {
            const string query = """
                INSERT INTO daily_access_logs (id, document_id, access_date, access_count)
                VALUES (@id, @documentId, @accessDate, @count)
                ON CONFLICT (document_id, access_date)
                DO UPDATE
                SET access_count = daily_access_logs.access_count + EXCLUDED.access_count;
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
                        command.Parameters.AddWithValue("accessDate", accessEntryList.AccessDate);
                        command.Parameters.AddWithValue("count", entry.AccessCount);

                        await command.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            }
        }
    }
}
