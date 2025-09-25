using Microsoft.EntityFrameworkCore;
using Project01_movie_lease_system.Models;
using Project01_movie_lease_system.Common;

namespace Project01_movie_lease_system.Repositories
{
    public class ReviewRepository
    {
        private readonly MovieDbContext _context;

        public ReviewRepository(MovieDbContext context)
        {
            _context = context;
        }
        public class ReviewWithStatsDto
        {
            public int ReviewId { get; set; }
            public string Content { get; set; } = string.Empty;
            public int Rating { get; set; }
            public string MemberName { get; set; } = string.Empty;

            public int TotalReviews { get; set; }
            public double AverageRating { get; set; }
        }

        public PagedResult<Review> GetPaged(int pageNumber, int pageSize)
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                throw new ArgumentException("Page number and page size must be greater than zero.");
            }
            var totalCount = _context.Reviews.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var items = _context.Reviews
                .OrderBy(r => r.Id)
                .Include(r => r.Member)
                .Include(r => r.Movie)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return new PagedResult<Review>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public Review? GetById(int id)
        {
            return _context.Reviews
                .Include(r => r.Member)
                .Include(r => r.Movie)
                .FirstOrDefault(r => r.Id == id);
        }




        public IEnumerable<Review> GetAll()
        {
            return _context.Reviews
                .Include(r => r.Member)
                .Include(r => r.Movie)
                .ToList();
        }

        public IEnumerable<ReviewWithStatsDto> GetByMovieId(int movieId)
        {
            var query = _context.Reviews
            .Where(r => r.MovieId == movieId)
            .Include(r => r.Member) // 關聯會員資料
            .Select(r => new ReviewWithStatsDto
            {
                ReviewId = r.Id,
                Content = r.Content,
                Rating = r.Rating,
                MemberName = r.Member.Name,

                // SQL 端 Window Function
                TotalReviews = _context.Reviews
                    .Where(rr => rr.MovieId == movieId)
                    .Count(),

                AverageRating = _context.Reviews
                    .Where(rr => rr.MovieId == movieId)
                    .Average(rr => rr.Rating)
            })
            .ToList();

            return query;
        }

        // 取得某位會員的所有評論
        public IEnumerable<Review> GetByMemberId(int memberId)
        {
            return _context.Reviews
                .Where(r => r.MemberId == memberId)
                .Include(r => r.Movie)
                .ToList();
        }

        public void Add(Review review)
        {
            if (review == null)
            {
                throw new ArgumentNullException(nameof(review), "Review cannot be null.");
            }
            _context.Reviews.Add(review);
            _context.SaveChanges();
        }
        public void Update(Review review)
        {
            _context.Reviews.Update(review);
            _context.SaveChanges();
        }
        public void Delete(int id)
        {
            var review = _context.Reviews.Find(id);
            if (review == null)
            {
                throw new KeyNotFoundException($"Review with ID {id} not found.");
            }
            _context.Reviews.Remove(review);
            _context.SaveChanges();
        }
        public IEnumerable<Review> GetRecentReviews(int count)
        {
            return _context.Reviews
                .Include(r => r.Member)
                .Include(r => r.Movie)
                .OrderByDescending(r => r.Rating)
                .Take(count)
                .ToList();
        }
    }
}