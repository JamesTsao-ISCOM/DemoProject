using Project01_movie_lease_system.Models;
using Project01_movie_lease_system.Common;
using System;
using Azure;
using Microsoft.EntityFrameworkCore;

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

        public MovieDetails? GetMovieDetailsById(int id)
        {
            var movie = _context.Movies
                .Include(m => m.Reviews)
                .FirstOrDefault(m => m.Id == id);

            if (movie == null)
                return null;

            return new MovieDetails
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                Type = movie.Type,
                ReleaseDate = movie.ReleaseDate,
                Director = movie.Director,
                Price = movie.Price,
                ImageFileName = movie.ImageFileName,
                YoutubeTrailerUrl = movie.YoutubeTrailerUrl,
                Stock = movie.Stock,
                AverageRating = movie.Reviews.Any() ? movie.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = movie.Reviews.Count(),
                Reviews = movie.Reviews
            };
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

        public MovieDetails? GetMovieDetails(int id)
        {
            var movie = _context.Movies
                .Where(m => m.Id == id)
                .GroupJoin(_context.Reviews,
                m => m.Id,
                r => r.MovieId,
                (movie, reviews) => new MovieDetails
                {
                    Id = movie.Id,
                    Title = movie.Title,
                    Description = movie.Description,
                    Type = movie.Type,
                    ReleaseDate = movie.ReleaseDate,
                    Director = movie.Director,
                    Price = movie.Price,
                    ImageFileName = movie.ImageFileName,
                    YoutubeTrailerUrl = movie.YoutubeTrailerUrl,
                    Stock = movie.Stock,
                    AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                    ReviewCount = reviews.Count()
                }).ToList().FirstOrDefault();

            if (movie == null)
                return null;

            return new MovieDetails
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                Type = movie.Type,
                ReleaseDate = movie.ReleaseDate,
                Director = movie.Director,
                Price = movie.Price,
                ImageFileName = movie.ImageFileName,
                YoutubeTrailerUrl = movie.YoutubeTrailerUrl,
                Stock = movie.Stock,
                AverageRating = movie.Reviews.Any() ? movie.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = movie.Reviews.Count(),
                Reviews = movie.Reviews
            };
        }
        public IEnumerable<MovieDetails> GetFeaturedMovies(int count)
        {
            var featuredMovies = _context.Movies
                .GroupJoin(_context.Reviews,
                          m => m.Id,
                          r => r.MovieId,
                          (movie, reviews) => new MovieDetails
                          {
                              Id = movie.Id,
                              Title = movie.Title,
                              Description = movie.Description,
                              Type = movie.Type,
                              ReleaseDate = movie.ReleaseDate,
                              Director = movie.Director,
                              Price = movie.Price,
                              ImageFileName = movie.ImageFileName,
                              YoutubeTrailerUrl = movie.YoutubeTrailerUrl,
                              Stock = movie.Stock,
                              AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                              ReviewCount = reviews.Count()
                          })
                .OrderByDescending(movieDetail => movieDetail.AverageRating)
                .ThenByDescending(movieDetail => movieDetail.ReleaseDate)
                .Take(count)
                .ToList();
            foreach (var movie in featuredMovies)
            {
                Console.WriteLine($"Movie: {movie.Title}, Avg Rating: {movie.AverageRating}, Reviews: {movie.ReviewCount}");
            }
            return featuredMovies;
        }
        public PagedResult<MovieDetails> GetMovieDetailsList(int pageNumber, int pageSize)
        {
            var movies = _context.Movies
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .GroupJoin(_context.Reviews,
                          m => m.Id,
                          r => r.MovieId,
                          (movie, reviews) => new MovieDetails
                          {
                              Id = movie.Id,
                              Title = movie.Title,
                              Description = movie.Description,
                              Type = movie.Type,
                              ReleaseDate = movie.ReleaseDate,
                              Director = movie.Director,
                              Price = movie.Price,
                              ImageFileName = movie.ImageFileName,
                              YoutubeTrailerUrl = movie.YoutubeTrailerUrl,
                              Stock = movie.Stock,
                              AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                              ReviewCount = reviews.Count()
                          })
                .ToList();

            var totalCount = _context.Movies.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PagedResult<MovieDetails>
            {
                Items = movies,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public PagedResult<MovieDetails> SearchMovies(string? movieName, MovieType? type, DateTime? startDate, DateTime? endDate, int pageNumber, int pageSize)
        {
            var searchMovies = _context.Movies
                .Where(m => (string.IsNullOrEmpty(movieName) || m.Title.Contains(movieName)) &&
                            (!type.HasValue || m.Type == type.Value) &&
                            (!startDate.HasValue || m.ReleaseDate.Date >= startDate.Value.Date) &&
                            (!endDate.HasValue || m.ReleaseDate.Date <= endDate.Value.Date))
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .GroupJoin(_context.Reviews,
                          m => m.Id,
                          r => r.MovieId,
                          (movie, reviews) => new MovieDetails
                          {
                              Id = movie.Id,
                              Title = movie.Title,
                              Description = movie.Description,
                              Type = movie.Type,
                              ReleaseDate = movie.ReleaseDate,
                              Director = movie.Director,
                              Price = movie.Price,
                              ImageFileName = movie.ImageFileName,
                              YoutubeTrailerUrl = movie.YoutubeTrailerUrl,
                              Stock = movie.Stock,
                              AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                              ReviewCount = reviews.Count()
                          })
                .ToList();
            return new PagedResult<MovieDetails>
            {
                Items = searchMovies,
                TotalCount = searchMovies.Count,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)searchMovies.Count / pageSize)
            };
        }
    }
}