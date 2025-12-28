using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.DAL.Repositories.Categories
{
    public interface ICategoryRepository
    {
        Task InitializeCategories(ICollection<string> categories);
        Task AddCategory(string name);
        Task UpdateCategory(string name);
        Task DeleteCategory(string name);
    }
}
