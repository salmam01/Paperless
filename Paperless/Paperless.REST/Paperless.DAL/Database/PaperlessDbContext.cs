using Microsoft.EntityFrameworkCore;
using Paperless.DAL.Entities;

namespace Paperless.DAL.Database
{
    public class PaperlessDbContext : DbContext
    {
        public DbSet<DocumentEntity> Documents { get; set; }
        public DbSet<CategoryEntity> Categories { get; set; }

        public PaperlessDbContext(DbContextOptions<PaperlessDbContext> options)
            : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DocumentEntity>()
                .HasOne(d => d.Category)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
