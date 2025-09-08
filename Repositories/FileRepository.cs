using Project01_movie_lease_system.Models;
using Project01_movie_lease_system.Common;
using Microsoft.EntityFrameworkCore;
using File = Project01_movie_lease_system.Models.File;

namespace Project01_movie_lease_system.Repositories
{
    public class FileRepository
    {
        private readonly MovieDbContext _context;
        public FileRepository(MovieDbContext context)
        {
            _context = context;
        }
        public void AddFile(File file)
        {
            _context.Files.Add(file);
            _context.SaveChanges();
        }
        public List<FileCategory> GetAllCategories()
        {
            return _context.FileCategories
                .OrderBy(c => c.CategoryName)
                .ToList();
        }
        public File? GetFileById(int id)
        {
            Console.WriteLine($"Repository 中的 GetFileById 方法被調用，ID: {id}");
            
            // 使用 Join 進行查詢
           var result = _context.Files
            .Join(_context.FileCategories,
                file => file.CategoryId,
                category => category.Id,
                (file, category) => new { File = file, Category = category })
            .Join(_context.Admins,
                fc => fc.File.AdminId,
                admin => admin.Id,
                (fc, admin) => new { fc.File, fc.Category, Admin = admin })
            .FirstOrDefault(x => x.File.Id == id);

            if (result != null && result.Admin != null)
            {
                result.File.Uploader = result.Admin.Username;
            }
            if (result?.Category != null)
            {
                result.File.Category = result.Category.CategoryName;
            }

            return result?.File;
        }
        
        public List<File> GetFilesByCategory(int categoryId)
        {
            // 使用 導航屬性 進行查詢
            return _context.Files
            .Include(f => f.FileCategory)
            .Where(f => f.CategoryId == categoryId)
            .ToList();
        }
        public PagedResult<File> GetFilesByPage(int pageNumber, int pageSize)
        {
            var totalItems = _context.Files.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var files = _context.Files
                .Include(f => f.FileCategory)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<File>
            {
                Items = files,
                TotalCount = totalItems,
                TotalPages = totalPages,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public List<File> GetAllFiles()
        {
            return _context.Files
            .Include(f => f.FileCategory)
            .ToList();
        }
        public void UpdateFile(File file)
        {
            _context.Files.Update(file);
            _context.SaveChanges();
        }
        public void DeleteFile(int id)
        {
            var file = _context.Files.Find(id);
            if (file != null)
            {
                _context.Files.Remove(file);
                _context.SaveChanges();
            }
        }

    }
}