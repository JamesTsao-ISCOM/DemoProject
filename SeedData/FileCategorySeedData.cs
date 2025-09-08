using Microsoft.EntityFrameworkCore;
using Project01_movie_lease_system.Models;

namespace Project01_movie_lease_system.Data
{
    public static class FileCategorySeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new MovieDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<MovieDbContext>>());

            var categories = new List<FileCategory>
            {
                new FileCategory {CategoryName = "document", Description = "文件" },
                new FileCategory {CategoryName = "image", Description = "圖片" },
                new FileCategory {CategoryName = "spreadsheet", Description = "試算表" },
                new FileCategory {CategoryName = "presentation", Description = "簡報" },
                new FileCategory {CategoryName = "video", Description = "影片" },
                new FileCategory {CategoryName = "audio", Description = "音訊" },
                new FileCategory {CategoryName = "zip", Description = "壓縮檔" },
                new FileCategory {CategoryName = "other", Description = "其他" }
            };
            for (int i = 0; i < categories.Count; i++)
            {
                if (!context.FileCategories.Any(c => c.CategoryName == categories[i].CategoryName))
                {
                    context.FileCategories.Add(categories[i]);
                }
            }
            context.SaveChanges();
        }
    }
}
