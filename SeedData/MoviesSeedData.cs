using Microsoft.EntityFrameworkCore;
using Project01_movie_lease_system.Models;

namespace Project01_movie_lease_system.Data
{
    public static class MoviesSeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new MovieDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<MovieDbContext>>());

            // 如果已經有資料就不插入
            if (context.Movies.Count() > 5)
                return;
            var random = new Random();
            var types = new[] {
                MovieType.Action, // 動作片
                MovieType.Comedy,  // 喜劇片
                MovieType.Drama,  // 劇情片
                MovieType.Horror,  // 恐怖片
                MovieType.SciFi,   // 科幻片
                MovieType.Documentary // 紀錄片
            };
            var directors = new[] { "張導演", "李導演", "王導演", "陳導演" };

            var movies = new List<Movie>();

            for (int i = 1; i <= 100; i++)
            {
                movies.Add(new Movie
                {
                    Title = $"測試電影 {i}",
                    Description = $"這是測試電影 {i} 的介紹。",
                    Type = types[random.Next(types.Length)],
                    ReleaseDate = DateTime.Now.AddDays(-random.Next(0, 1000)),
                    Director = directors[random.Next(directors.Length)],
                    Price = random.Next(100, 500),
                    Stock = random.Next(1, 50),
                    YoutubeTrailerUrl = "https://www.youtube.com/embed/SL83f7Nzxr0",
                    ImageFileName = "default.jpg" // 這裡可以放預設圖片
                });
            }

            context.Movies.AddRange(movies);
            context.SaveChanges();
        }
    }
}
