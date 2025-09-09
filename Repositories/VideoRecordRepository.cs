using Project01_movie_lease_system.Models;
using Microsoft.EntityFrameworkCore;
using File = Project01_movie_lease_system.Models.File;

namespace Project01_movie_lease_system.Repositories
{
    public class VideoRecordRepository
    {
        private readonly MovieDbContext _context;
        public VideoRecordRepository(MovieDbContext context)
        {
            _context = context;
        }
        public void AddVideoWatchRecord(VideoWatchRecord record)
        {
            _context.VideoWatchRecords.Add(record);
            _context.SaveChanges();
        }
        public void UpdateVideoWatchRecord(VideoWatchRecord record)
        {
            _context.VideoWatchRecords.Update(record);
            _context.SaveChanges();
        }
        public VideoWatchRecord? GetVideoWatchRecordById(int adminId, int fileId)
        {
            return _context.VideoWatchRecords
                .FirstOrDefault(v => v.AdminId == adminId && v.FileId == fileId);
        }
        
        public List<VideoWatchRecord> GetVideoWatchRecordsByFileId(int fileId)
        {
            return _context.VideoWatchRecords
                .Where(v => v.FileId == fileId)
                .ToList();
        }
        
        public void DeleteVideoWatchRecordsByFileId(int fileId)
        {
            var records = _context.VideoWatchRecords.Where(v => v.FileId == fileId).ToList();
            if (records.Any())
            {
                _context.VideoWatchRecords.RemoveRange(records);
                _context.SaveChanges();
            }
        }
    }
}