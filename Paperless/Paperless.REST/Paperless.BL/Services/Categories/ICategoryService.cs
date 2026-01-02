using Paperless.BL.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Services.Categories
{
    public interface ICategoryService
    {
        Task PopulateCategoriesAsync(List<Category> categories);
        Task<List<Category>> GetCategoriesAsync();
        Task<Category> GetCategoryAsync(Guid id);
        Task AddCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(Guid id);
    }
}
