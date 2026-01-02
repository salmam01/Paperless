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

        public async Task PopulateCategoriesAsync(IEnumerable<CategoryEntity> categories)
        {
            try
            {
                await _context.Categories.AddRangeAsync(categories);
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

        public async Task AddCategoryAsync(CategoryEntity category)
        {
            try
            {
                await _context.Categories.AddAsync(category);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (IsDatabaseException(ex))
                    throw new DatabaseException("An Error occurred while adding a new Category.", ex);
                else
                    throw;
            }
        }

        public async Task<IEnumerable<CategoryEntity>> GetCategoriesAsync()
        {
            try
            {
                return await _context.Categories.AsNoTracking().ToListAsync();
            }
            catch (Exception ex)
            {
                if (IsDatabaseException(ex))
                    throw new DatabaseException("An Error occurred while retrieving all Categories.", ex);
                else
                    throw;
            }
        }

        public async Task<CategoryEntity> GetCategoryAsync(Guid id)
        {
            try
            {
                CategoryEntity? category = await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == id);

                return category ?? throw new KeyNotFoundException($"Category {id} not found");
            }
            catch (Exception ex)
            {
                if (IsDatabaseException(ex))
                    throw new DatabaseException("An Error occurred while retrieving Category by ID.", ex);
                else
                    throw;
            }
        }

        public async Task UpdateCategory(CategoryEntity category) 
        {

        }

        public async Task DeleteCategory(Guid id)
        {
            try
            {
                CategoryEntity? category = await _context.Categories.FindAsync(id);
                if (category == null)
                    throw new ArgumentNullException(nameof(category), "DeleteCategory: Document doesn't exist!");

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (IsDatabaseException(ex))
                    throw new DatabaseException("An Error occurred while deleting a category.", ex);
                else
                    throw;
            }
        }

        private bool IsDatabaseException(Exception ex)
        {
            return  ex is DbUpdateException ||
                    ex is PostgresException ||
                    ex is InvalidOperationException;
        }
    }
}
