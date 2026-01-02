using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KusinaPOS.Helpers;
using KusinaPOS.Models;
using KusinaPOS.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Diagnostics;

namespace KusinaPOS.ViewModel
{
    public partial class POSTerminalViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Category> menuCategories = new();

        [ObservableProperty]
        private bool isLoading = true;

        private readonly CategoryService categoryService;

        public POSTerminalViewModel(CategoryService categoryService)
        {
            this.categoryService = categoryService;

            // Initialize with empty collection first
            MenuCategories = new ObservableCollection<Category>
            {
                new Category { Id = 0, Name = "All", IsSelected = true },
                new Category { Id = 1, Name = "Meals", IsSelected = false },
                new Category { Id = 2, Name = "Drinks", IsSelected = false },
                new Category { Id = 3, Name = "Desserts", IsSelected = false }
            };

            Debug.WriteLine("=== POSTerminalViewModel Constructor ===");
            _ = InitializeCollectionsAsync();
        }

        // ======================
        // INITIALIZATION
        // ======================
        private async Task InitializeCollectionsAsync()
        {
            try
            {
                Debug.WriteLine("=== Starting InitializeCollectionsAsync ===");
                IsLoading = true;

                // Load categories
                var categoryList = await categoryService.GetAllCategoriesAsync();
                Debug.WriteLine($"=== Loaded {categoryList?.Count() ?? 0} categories ===");

                // Clear and add to existing collection
                MenuCategories.Clear();

                // Add "All" category first
                MenuCategories.Add(new Category
                {
                    Id = 0,
                    Name = "All",
                    IsSelected = true
                });

                // Add rest of categories
                if (categoryList != null)
                {
                    foreach (var category in categoryList)
                    {
                        category.IsSelected = false;
                        MenuCategories.Add(category);
                        Debug.WriteLine($"=== Added category: {category.Name} ===");
                    }
                }

                Debug.WriteLine($"=== Total MenuCategories: {MenuCategories.Count} ===");
                IsLoading = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== Error loading categories: {ex.Message} ===");
                Debug.WriteLine($"=== Stack trace: {ex.StackTrace} ===");

                IsLoading = false;
                await PageHelper.DisplayAlertAsync("Error",
                    $"Failed to load data: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private void SelectCategory(Category category)
        {
            Debug.WriteLine($"=== Category selected: {category?.Name} ===");

            // Deselect all categories
            foreach (var cat in MenuCategories)
            {
                cat.IsSelected = false;
            }

            // Select the clicked category
            if (category != null)
            {
                category.IsSelected = true;
            }
        }
    }
}