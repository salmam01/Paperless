using Microsoft.EntityFrameworkCore;
using Paperless.DAL.Entities;

namespace Paperless.DAL.Database
{
    public class PaperlessDbContext : DbContext
    {
        private readonly string _connectionString = "Host=paperless-db;Port=5432;Database=PaperlessDB;Username=swen3;Password=paperless123";
        public DbSet<DocumentEntity> Documents { get; set; }

        public PaperlessDbContext() { }
        
        //  For Unit tests
        public PaperlessDbContext(DbContextOptions<PaperlessDbContext> options): base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(_connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DocumentEntity>();

            //  TODO: Full-text search vector
        }
    }
}
