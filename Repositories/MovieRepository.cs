using Project01_movie_lease_system.Models;
using Project01_movie_lease_system.Common;
using System;
using Azure;

namespace Project01_movie_lease_system.Repositories
{
    public class MovieRepository
    {
        private readonly MovieDbContext _context;

        public MovieRepository(MovieDbContext context)
        {
            _context = context;
        }

        public PagedResult<Movie> GetPaged(int pageNumber, int pageSize)
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                throw new ArgumentException("Page number and page size must be greater than zero.");
            }
            var totalCount = _context.Movies.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var items = _context.Movies
                .OrderByDescending(m => m.ReleaseDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return new PagedResult<Movie>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public Movie? GetById(int id)
        {
            return _context.Movies.Find(id);
        }
        public IEnumerable<Movie> GetAll()
        {
            return _context.Movies
                .OrderByDescending(m => m.ReleaseDate)
                .ToList();
        }

        public void Add(Movie movie)
        {
            if (movie == null)
            {
                throw new ArgumentNullException(nameof(movie), "Movie cannot be null.");
            }
            _context.Movies.Add(movie);
            _context.SaveChanges();
        }

        public void Update(Movie movie)
        {
            _context.Movies.Update(movie);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var movie = _context.Movies.Find(id);
            if (movie == null)
            {
                throw new KeyNotFoundException($"Movie with ID {id} not found.");
            }
            _context.Movies.Remove(movie);
            _context.SaveChanges();
        }

        public PagedResult<Movie> SearchByTitle(string title, int pageNumber, int pageSize)
        {
            var movies = _context.Movies
                .Where(m => m.Title.Contains(title))
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var totalCount = _context.Movies.Count(m => m.Title.Contains(title));
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            return new PagedResult<Movie>
            {
                Items = movies,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public PagedResult<Movie> FilterByType(string type, int pageNumber, int pageSize)
        {
            // 解析电影类型枚举
            if (Enum.TryParse<MovieType>(type, true, out var movieType))
            {
                var totalItems = _context.Movies.Count(m => m.Type == movieType);
                
                var movies = _context.Movies
                    .Where(m => m.Type == movieType)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                var totalCount = _context.Movies.Count(m => m.Type == movieType);
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                return new PagedResult<Movie>
                {
                    Items = movies,
                    TotalCount = totalItems,
                    TotalPages = totalPages,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            
            // 如果无法解析类型，返回空结果
            return new PagedResult<Movie>
            {
                Items = new List<Movie>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = 0
            };
        }

        public PagedResult<Movie> FilterByDateRange(DateTime startDate, DateTime endDate, int pageNumber, int pageSize)
        {
            var movies = _context.Movies
                .Where(m => m.ReleaseDate >= startDate && m.ReleaseDate <= endDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var totalCount = _context.Movies.Count(m => m.ReleaseDate >= startDate && m.ReleaseDate <= endDate);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            return new PagedResult<Movie>
            {
                Items = movies,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}