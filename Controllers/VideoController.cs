using Microsoft.AspNetCore.Mvc;
using Project01_movie_lease_system.Repositories;
using Project01_movie_lease_system.Models;
using System.Security.Claims;

namespace Project01_movie_lease_system.Controllers
{
    public class VideoController : Controller
    {
        private readonly VideoRecordRepository _videoRecordRepository;

        public VideoController(VideoRecordRepository videoRecordRepository)
        {
            _videoRecordRepository = videoRecordRepository;
        }
        [HttpPost]
        public IActionResult Watch([FromForm] int fileId, [FromForm] int lastPosition, [FromForm] bool isCompleted)
        {
            Console.WriteLine("影片觀看事件, fileId:", fileId, "lastPosition:", lastPosition, "isCompleted:", isCompleted);
            if (!User.Identity.IsAuthenticated || User.FindFirst(ClaimTypes.NameIdentifier) == null)
            {
                return BadRequest("User is not authenticated");
            }
            // Get the current user's admin ID
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            // Record the video watch event
            var record = new VideoWatchRecord
            {
                AdminId = adminId,
                FileId = fileId,
                LastPosition = lastPosition,
                IsCompleted = isCompleted,
                LastWatchedAt = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            var searchRecord = _videoRecordRepository.GetVideoWatchRecordById(adminId, fileId);
            if (searchRecord == null)
            {
                _videoRecordRepository.AddVideoWatchRecord(record);
                return Ok(new { message = "影片紀錄建立成功" });
            }
            else
            {
                searchRecord.LastPosition = lastPosition;
                searchRecord.IsCompleted = isCompleted;
                searchRecord.UpdatedAt = DateTime.Now;
                _videoRecordRepository.UpdateVideoWatchRecord(searchRecord);
                return Ok(new { message = "影片紀錄更新成功" });
            }
        }
        [HttpGet]
        public IActionResult GetWatchHistory(int id)
        {
            if (!User.Identity.IsAuthenticated || User.FindFirst(ClaimTypes.NameIdentifier) == null)
            {
                return BadRequest("User is not authenticated");
            }

            // Get the current user's admin ID
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Console.WriteLine($"GetWatchHistory called with adminId: {adminId}, fileId: {id}");
            // Retrieve the watch history for the user
            var watchHistory = _videoRecordRepository.GetVideoWatchRecordById(adminId, id);
            return Ok(watchHistory);
        }
    }
}