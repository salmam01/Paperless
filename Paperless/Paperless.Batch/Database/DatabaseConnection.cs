using Npgsql;

namespace Paperless.Batch.Database
{
    public class DatabaseConnection
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseConnection> _logger;

        public DatabaseConnection(ILogger<DatabaseConnection> logger, string connectionString)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<NpgsqlConnection> OpenConnection()
        {
            try
            {
                NpgsqlConnection connection = new(_connectionString);
                await connection.OpenAsync();
                return connection;
            }
            catch (NpgsqlException e)
            {
                _logger.LogError("Failed to connect to Database: {Message}", e.Message);
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to connect to Database: {Message}", e.Message);
                throw;
            }
        }
    }
}
