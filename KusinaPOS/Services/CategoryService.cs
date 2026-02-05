using KusinaPOS.Models;
using SQLite;
using System.Diagnostics;

namespace KusinaPOS.Services
{
    public class CategoryService
    {
        private readonly SQLiteAsyncConnection _db;

        public CategoryService(IDatabaseService databaseService)
        {
            _db = databaseService.GetConnection();
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _db.CreateTableAsync<Category>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing Category table: {ex.Message}");
                throw;
            }
        }

        public async Task AddCategoryAsync(string name)
        {
            try
            {
                await InitializeAsync();
                var category = new Category
                {
                    Name = name.Trim()
                };
                await _db.InsertAsync(category);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding category: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            try
            {
                await InitializeAsync();
                await _db.UpdateAsync(category);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating category: {ex.Message}");
                throw;
            }
        }

        public async Task DeactivateCategoryAsync(int categoryId)
        {
            try
            {
                await InitializeAsync();
                var category = await _db.Table<Category>()
                                        .Where(c => c.Id == categoryId)
                                        .FirstOrDefaultAsync();
                if (category != null)
                {
                    category.IsActive = false;
                    await _db.UpdateAsync(category);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deactivating category: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Category>> GetActiveCategoriesAsync()
        {
            try
            {
                await InitializeAsync();
                return await _db.Table<Category>()
                                 .Where(c => c.IsActive)
                                .OrderBy(c => c.Name)
                                .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting active categories: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            try
            {
                await InitializeAsync();
                return await _db.Table<Category>()
                                .OrderBy(c => c.Name)
                                .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting all categories: {ex.Message}");
                throw;
            }
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            try
            {
                await InitializeAsync();
                return await _db.Table<Category>()
                                .Where(c => c.Id == id)
                                .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting category by id: {ex.Message}");
                throw;
            }
        }

        public async Task AddAllCategoriesAsync(List<Category> categories)
        {
            try
            {
                await InitializeAsync();
                await _db.InsertAllAsync(categories);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding all categories: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteCategoryAsync(Category category)
        {
            try
            {
                await InitializeAsync();
                int categoryId = category.Id;
                var existingCategory = await _db.Table<Category>()
                                        .Where(c => c.Id == categoryId)
                                        .FirstOrDefaultAsync();
                if (existingCategory != null)
                {
                    await _db.DeleteAsync(existingCategory);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting category: {ex.Message}");
                throw;
            }
        }
    }
}