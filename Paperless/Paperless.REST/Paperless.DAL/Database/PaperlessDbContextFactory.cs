/*
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Paperless.DAL.Database
{
    
    public class PaperlessDbContextFactory : IDesignTimeDbContextFactory<PaperlessDbContext>
    {
        public PaperlessDbContext CreateDbContext(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, false)
                .Build();

            DatabaseConfig dbConfig = config.GetSection("Database").Get<DatabaseConfig>();

            if (dbConfig == null)
                throw new InvalidOperationException("Connection string is missing or empty in [appsettings.json].");

            string connectionString = dbConfig.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string is missing or empty in [appsettings.json].");

            DbContextOptions<PaperlessDbContext> options = new DbContextOptionsBuilder<PaperlessDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new PaperlessDbContext(options);
        }
    }
}
*/