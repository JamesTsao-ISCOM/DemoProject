using Project01_movie_lease_system.Models;
using Project01_movie_lease_system.Common;
namespace Project01_movie_lease_system.Repositories
{
    public class MemberRepository
    {
        private readonly MovieDbContext _context;
        public MemberRepository(MovieDbContext context)
        {
            _context = context;
        }
        public PagedResult<Member> GetPaged(int pageNumber, int pageSize)
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                throw new ArgumentException("Page number and page size must be greater than zero.");
            }
            var totalCount = _context.Members.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var items = _context.Members
                .OrderBy(m => m.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return new PagedResult<Member>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public IEnumerable<Member> GetAll()
        {
            return _context.Members.ToList();
        }
        public Member? GetById(int id)
        {
            return _context.Members.Find(id);
        }
        public IEnumerable<Member> GetByPhone(string phoneNumber)
        {
            return _context.Members
                .Where(m => m.PhoneNumber.Contains(phoneNumber))
                .ToList();
        }
        public IEnumerable<Member> GetByEmail(string email)
        {
            return _context.Members
                .Where(m => m.Email.Contains(email))
                .ToList();
        }

        public void Add(Member member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member), "Member cannot be null.");
            }
            _context.Members.Add(member);
            _context.SaveChanges();
        }
        public void Update(Member member)
        {
            _context.Members.Update(member);
            _context.SaveChanges();
        }
        public void Delete(int id)
        {
            var member = _context.Members.Find(id);
            if (member == null)
            {
                throw new KeyNotFoundException($"Member with ID {id} not found.");
            }
            _context.Members.Remove(member);
            _context.SaveChanges();
        }

    }
}