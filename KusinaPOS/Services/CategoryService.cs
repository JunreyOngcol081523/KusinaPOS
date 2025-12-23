using KusinaPOS.Models;
using SQLite;

namespace KusinaPOS.Services
{
    public class CategoryService
    {
        private readonly SQLiteAsyncConnection _db;

        public CategoryService(IDatabaseService databaseService)
        {
            _db = databaseService.GetConnection();
        }

        // 🔹 Create table (call on app startup)
        public async Task InitializeAsync()
        {
            await _db.CreateTableAsync<Category>();
        }

        // 🔹 Add new category
        public async Task AddCategoryAsync(string name)
        {
            var category = new Category
            {
                Name = name.Trim()
            };

            await _db.InsertAsync(category);
        }

        // 🔹 Update category name
        public async Task UpdateCategoryAsync(Category category)
        {
            await _db.UpdateAsync(category);
        }

        // 🔹 Soft delete
        public async Task DeactivateCategoryAsync(int categoryId)
        {
            var category = await _db.Table<Category>()
                                    .Where(c => c.Id == categoryId)
                                    .FirstOrDefaultAsync();

            if (category != null)
            {
                
                await _db.UpdateAsync(category);
            }
        }

        // 🔹 Get active categories only
        public async Task<List<Category>> GetActiveCategoriesAsync()
        {
            return await _db.Table<Category>()
                             .Where(c => c.IsActive)
                            .OrderBy(c => c.Name)
                            .ToListAsync();
        }

        // 🔹 Get all categories (admin view)
        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _db.Table<Category>()
                            .OrderBy(c => c.Name)
                            .ToListAsync();
        }

        // 🔹 Get by Id
        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _db.Table<Category>()
                            .Where(c => c.Id == id)
                            .FirstOrDefaultAsync();
        }

    }
}
