using Azure;
using Project01_movie_lease_system.Common;
using Project01_movie_lease_system.Models;
namespace Project01_movie_lease_system.Repositories
{
    public class AdminRepository
    {
        private readonly MovieDbContext _context;

        public AdminRepository(MovieDbContext context)
        {
            _context = context;
        }

        public Admin? GetByUsername(string username)
        {
            return _context.Admins.FirstOrDefault(a => a.Username == username);
        }
        public Admin? GetById(int id)
        {
            return _context.Admins.FirstOrDefault(a => a.Id == id);
        }
        public IEnumerable<Admin> GetAll()
        {
            return _context.Admins.ToList();
        }
        public PagedResult<Admin> GetPaged(int pageNumber, int pageSize)
        {
            var admins = _context.Admins
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var totalCount = _context.Admins.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            return new PagedResult<Admin>
            {
                Items = admins,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }
        public void Add(Admin admin)
        {
            _context.Admins.Add(admin);
            _context.SaveChanges();
        }
        public void Update(Admin admin)
        {
            _context.Admins.Update(admin);
            _context.SaveChanges();
        }
        public void Delete(int id)
        {
            var admin = _context.Admins.Find(id);
            if (admin != null)
            {
                _context.Admins.Remove(admin);
                _context.SaveChanges();
            }
        }
        public async Task AddRangeAsync(IEnumerable<Admin> admins)
    {
        if (admins == null || !admins.Any())
        {
            return; // 如果列表為空，直接返回
        }

        try
        {
            // 使用確保資料一致性
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 批量新增到 DbSet
                    await _context.Admins.AddRangeAsync(admins);
                    
                    // 保存變更到資料庫
                    await _context.SaveChangesAsync();
                    
                    // 提交事務
                    await transaction.CommitAsync();
                    
                }catch (Exception)
                {
                    // 發生錯誤時回滾交易
                    await transaction.RollbackAsync();
                    throw; // 重新拋出異常以便外層捕獲
                }
            }
        }
        catch (Exception ex)
        {
            
            // 處理其他異常
                throw new Exception("批量插入過程中發生錯誤", ex);
        }
    }
    }
}