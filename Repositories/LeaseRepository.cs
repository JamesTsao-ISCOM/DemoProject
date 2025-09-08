using Project01_movie_lease_system.Models;
using Project01_movie_lease_system.Common;
using Microsoft.EntityFrameworkCore;

namespace Project01_movie_lease_system.Repositories
{
    public class LeaseRepository
    {
        private readonly MovieDbContext _context;

        public LeaseRepository(MovieDbContext context)
        {
            _context = context;
        }

        public PagedResult<Lease> GetPaged(int pageNumber, int pageSize)
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                throw new ArgumentException("Page number and page size must be greater than zero.");
            }
            var totalCount = _context.Leases.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var items = _context.Leases
                .OrderBy(l => l.Id)
                .Include(l => l.Member)
                .Include(l => l.Movie)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return new PagedResult<Lease>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public IEnumerable<Lease> GetAll()
        {
            return _context.Leases
                .Include(l => l.Member)
                .Include(l => l.Movie)
                .ToList();
        }

        public IEnumerable<Lease> GetByMemberId(int memberId)
        {
            return _context.Leases
                .Where(l => l.MemberId == memberId)
                .Include(l => l.Movie)
                .ToList();
        }
        public void Add(Lease lease)
        {
            if (lease == null)
            {
                throw new ArgumentNullException(nameof(lease), "Lease cannot be null.");
            }
            _context.Leases.Add(lease);
            _context.SaveChanges();
        }
        public void Update(Lease lease)
        {
            _context.Leases.Update(lease);
            _context.SaveChanges();
        }
        public void Delete(int id)
        {
            var lease = _context.Leases.Find(id);
            if (lease == null)
            {
                throw new KeyNotFoundException($"Lease with ID {id} not found.");
            }
            _context.Leases.Remove(lease);
            _context.SaveChanges();
        }
        
    }
}