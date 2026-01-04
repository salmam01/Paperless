using Paperless.DAL.Entities;

namespace Paperless.DAL.Repositories.Categories
{
    public interface ICategoryRepository
    {
        Task PopulateCategoriesAsync(IEnumerable<CategoryEntity> categories);
        Task AddCategoryAsync(CategoryEntity category);
        Task<IEnumerable<CategoryEntity>> GetCategoriesAsync();
        Task<CategoryEntity> GetCategoryAsync(Guid id);
        Task UpdateCategory(CategoryEntity category);
        Task DeleteCategory(Guid id);
    }
}
