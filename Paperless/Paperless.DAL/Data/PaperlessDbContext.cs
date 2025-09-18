using Microsoft.EntityFrameworkCore;
using Paperless.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.DAL.Data
{
    public class PaperlessDbContext : DbContext
    {
        public DbSet<DocumentEntity> Documents { get; set; }

        public PaperlessDbContext(DbContextOptions<PaperlessDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //  TODO: implement the Document entity
            modelBuilder.Entity<DocumentEntity>();

            //  TODO: Full-text search vector
        }
    }
}
