using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Services.Categories
{
    public interface ICategoryService
    {
        Task InitializeCategories(List<string> categories);
        Task AddCategory(string category);
        Task UpdateCategory(string category);
        Task DeleteCategory(string category);
    }
}
