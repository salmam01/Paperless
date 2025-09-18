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
        private readonly string _connectionString = "Host=localhost;Port=5432;Database=PaperlessDB;Username=swen3;Password=paperless123";
        public DbSet<DocumentEntity> Documents { get; set; }

        public PaperlessDbContext() { }

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
