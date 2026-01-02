using AutoMapper;
using Microsoft.Extensions.Logging;
using Paperless.BL.Models.Domain;
using Paperless.DAL.Entities;
using Paperless.DAL.Repositories.Categories;


namespace Paperless.BL.Services.Categories
{
    public class CategoryService : ICategoryService
    {
        private readonly ILogger<CategoryService> _logger;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public CategoryService(
            ICategoryRepository categoryRepository, 
            IMapper mapper,
            ILogger<CategoryService> logger
        ) {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task PopulateCategoriesAsync(List<Category> categories)
        {
            IEnumerable<CategoryEntity> entities = _mapper.Map<IEnumerable<CategoryEntity>>(categories);
            await _categoryRepository.PopulateCategoriesAsync(entities);
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            IEnumerable<CategoryEntity> entities = await _categoryRepository.GetCategoriesAsync();
            List<Category> categories = _mapper.Map<List<Category>>(entities);
            return categories;
        }

        public async Task<Category> GetCategoryAsync(Guid id)
        {
            CategoryEntity entity = await _categoryRepository.GetCategoryAsync(id);
            Category category = _mapper.Map<Category>(entity);
            return category;
        }

        public async Task AddCategoryAsync(Category category)
        {
            CategoryEntity entity = _mapper.Map<CategoryEntity>(category);
            await _categoryRepository.AddCategoryAsync(entity);
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            CategoryEntity entity = _mapper.Map<CategoryEntity>(category);
            await _categoryRepository.UpdateCategory(entity);
        }

        public async Task DeleteCategoryAsync(Guid id)
        {
            await _categoryRepository.DeleteCategory(id);
        }
    }
}
