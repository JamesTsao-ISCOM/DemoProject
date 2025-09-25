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
                .OrderByDescending(l => l.LeaseDate)
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

        public LeaseStatistics GetStatistics()
        {
            var stats = new LeaseStatistics
            {
                PendingLeases = _context.Leases.Count(l => l.Status == 0),
                ActiveLeases = _context.Leases.Count(l => l.Status == 1),
                CompletedLeases = _context.Leases.Count(l => l.Status == 2),
                CancelledLeases = _context.Leases.Count(l => l.Status == 3)
            };
            return stats;
        }
        public PagedResult<Lease> Search(int leaseId=0,string memberName="",int status=-1,DateTime? leaseDate=null,int pageNumber=1, int pageSize=10)
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                throw new ArgumentException("Page number and page size must be greater than zero.");
            }

            var query = _context.Leases
                .Include(l => l.Member)
                .Include(l => l.Movie)
                .Where(l => (leaseId == 0 || l.Id == leaseId) &&
                            (string.IsNullOrEmpty(memberName) || l.Member.Name.Contains(memberName)) &&
                            (status == -1 || l.Status == status) &&
                            (!leaseDate.HasValue || l.LeaseDate.Date == leaseDate.Value.Date));

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var items = query
                .OrderByDescending(l => l.LeaseDate)
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
        public PagedResult<Lease> GetByMemberId(int memberId, int pageNumber, int pageSize, int status=-1)
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                throw new ArgumentException("Page number and page size must be greater than zero.");
            }
    
            var totalCount = _context.Leases.Count(l => l.MemberId == memberId && (status == -1 || l.Status == status));
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            var items = _context.Leases
                .Where(l => l.MemberId == memberId && (status == -1 || l.Status == status))
                .OrderByDescending(l => l.LeaseDate)
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
        public Lease? GetById(int id)
        {
            return _context.Leases
                .Include(l => l.Member)
                .Include(l => l.Movie)
                .FirstOrDefault(l => l.Id == id);
        }
        public void Add(Lease lease)
        {
            if (lease == null)
            {
                throw new ArgumentNullException(nameof(lease), "Lease cannot be null.");
            }
            lease.Status = 0; // 設定狀態為 "待處理"
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