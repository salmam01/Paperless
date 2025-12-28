using Microsoft.EntityFrameworkCore;
using Npgsql;
using Paperless.DAL.Database;
using Paperless.DAL.Entities;
using Paperless.DAL.Exceptions;


namespace Paperless.DAL.Repositories.Categories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly PaperlessDbContext _context;

        public CategoryRepository(PaperlessDbContext context)
        {
            _context = context;
        }

        public async Task InitializeCategories(ICollection<string> categories)
        {
            try
            {
                foreach (string category in categories)
                {
                    await AddCategory(category);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (IsDatabaseException(ex))
                    throw new DatabaseException("An Error occurred while adding predefined categories.", ex);
                else
                    throw;
            }
        }

        public async Task AddCategory(string name)
        {
            try
            {
                CategoryEntity category = new CategoryEntity();
                category.Id = Guid.NewGuid();
                category.Name = name;

                await _context.Categories.AddAsync(category);
            }
            catch (Exception ex)
            {
                if (IsDatabaseException(ex))
                    throw new DatabaseException("An Error occurred while adding predefined categories.", ex);
                else
                    throw;
            }
        }

        public async Task UpdateCategory(string name)
        {

        }

        public async Task DeleteCategory(string name)
        {
        }

        private bool IsDatabaseException(Exception ex)
        {
            return ex is DbUpdateException ||
                    ex is PostgresException ||
                    ex is InvalidOperationException;
        }
    }
}
