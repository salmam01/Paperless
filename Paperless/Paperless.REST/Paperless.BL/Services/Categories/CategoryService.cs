using Microsoft.Extensions.Logging;
using Paperless.DAL.Repositories.Categories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Services.Categories
{
    public class CategoryService : ICategoryService
    {
        private readonly ILogger<CategoryService> _logger;
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public async Task InitializeCategories(List<string> categories)
        {
            await _categoryRepository.InitializeCategories(categories);
        }

        public async Task AddCategory(string category)
        {
            await _categoryRepository.AddCategory(category);
        }

        public async Task UpdateCategory(string category)
        {
            await _categoryRepository.UpdateCategory(category);
        }

        public async Task DeleteCategory(string category)
        {
            await _categoryRepository.DeleteCategory(category);
        }
    }
}
