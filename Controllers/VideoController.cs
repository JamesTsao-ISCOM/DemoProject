using Microsoft.AspNetCore.Mvc;
using Project01_movie_lease_system.Repositories;
using Project01_movie_lease_system.Models;
using System.Security.Claims;
using Xabe.FFmpeg;

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

        [HttpPost]
        [RequestSizeLimit(500 * 1024 * 1024)] // 500 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 500 * 1024 * 1024)] // 500 MB
        public async Task<IActionResult> ProcessVideo(
            IFormFile file,
            [FromForm] double startTime,
            [FromForm] double endTime,
            [FromForm] string format,
            [FromForm] string effect
        )
        {
            Console.WriteLine("=== ProcessVideo Method Called ===");
            Console.WriteLine($"File: {file?.FileName ?? "null"} (Size: {file?.Length ?? 0} bytes)");
            Console.WriteLine($"StartTime: {startTime}");
            Console.WriteLine($"EndTime: {endTime}");
            Console.WriteLine($"Format: {format ?? "null"}");
            Console.WriteLine($"Effect: {effect ?? "null"}");
            Console.WriteLine($"Request ContentType: {Request.ContentType}");
            Console.WriteLine($"Request Method: {Request.Method}");
            Console.WriteLine("=== Form Data ===");
            if (file == null || file.Length == 0)
            {
                Console.WriteLine("ERROR: No file uploaded or file is empty");
                return BadRequest("No file uploaded");
            }
            if (startTime < 0 || endTime <= startTime)
                return BadRequest("Invalid startTime or endTime.");
            if (!new[] { "mp4", "webm" }.Contains(format))
                return BadRequest("Invalid format. Supported: mp4, webm.");
            if (!new[] { "blur", "brightness", "grayscale","none"}.Contains(effect))
                return BadRequest("Invalid effect. Supported: blur, brightness, grayscale.");
            try
            {
                // 確保臨時目錄存在
                string tempPath = Path.GetTempPath();
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                string inputFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string inputFilePath = Path.Combine(tempPath, inputFileName);
                
                // 確保輸入檔案路徑的目錄存在
                string inputDirectory = Path.GetDirectoryName(inputFilePath);
                if (!Directory.Exists(inputDirectory))
                {
                    Directory.CreateDirectory(inputDirectory);
                }

                await using (var stream = new FileStream(inputFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                string outputFileName = Guid.NewGuid().ToString() + "." + format;
                string outputFilePath = Path.Combine(tempPath, outputFileName);
                
                // 確保輸出檔案路徑的目錄存在
                string outputDirectory = Path.GetDirectoryName(outputFilePath);
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputFilePath);
                IVideoStream? videoStream = mediaInfo.VideoStreams.FirstOrDefault();
                if (videoStream == null)
                {
                    return BadRequest("No video stream found in the uploaded file.");
                }
                double videoDuration = mediaInfo.Duration.TotalSeconds;
                if (endTime > videoDuration)
                {
                    return BadRequest($"End time ({endTime}s) exceeds video duration ({videoDuration}s).");
                }
                if (videoStream != null)
                {
                    string filter = effect switch
                    {
                        "blur" => "boxblur=5:1",
                        "brightness" => "eq=brightness=0.1",
                        "grayscale" => "colorchannelmixer=.3:.4:.3:0:.3:.4:.3:0:.3:.4:.3",
                        _ => ""
                    };
                    var conversion = FFmpeg.Conversions.New();
                    conversion.AddParameter($"-i \"{inputFilePath}\"");
                    conversion.AddParameter($"-ss {TimeSpan.FromSeconds(startTime)}");
                    conversion.AddParameter($"-t {TimeSpan.FromSeconds(endTime - startTime)}");
                    
                    // 設定影片編碼和解析度
                    if (format == "mp4")
                    {
                        conversion.AddParameter("-c:v libx264");
                    }
                    else if (format == "webm")
                    {
                        conversion.AddParameter("-c:v libvpx-vp9");
                    }
                    
                    
                    
                    // 只在有濾鏡效果時才加入 -vf 參數
                    if (!string.IsNullOrEmpty(filter))
                    {
                        conversion.AddParameter($"-vf \"{filter}\"");
                    }
                    
                    conversion.AddParameter($"\"{outputFilePath}\"");
                    
                    Console.WriteLine($"FFmpeg command will be executed with input: {inputFilePath}, output: {outputFilePath}");
                    
                    // 檢查輸入檔案是否存在
                    if (!System.IO.File.Exists(inputFilePath))
                    {
                        return StatusCode(500, $"Input file not found: {inputFilePath}");
                    }
                    
                    await conversion.Start();
                    // 檢查輸出檔案是否存在
                    if (!System.IO.File.Exists(outputFilePath))
                    {
                        return StatusCode(500, "Video processing failed - output file not created.");
                    }

                    // 讀取處理後的檔案
                    byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(outputFilePath);

                    // 設定 Content-Type
                    string contentType = format switch
                    {
                        "mp4" => "video/mp4",
                        "webm" => "video/webm",
                        _ => "application/octet-stream"
                    };
                    // 清理臨時檔案
                    try
                    {
                        System.IO.File.Delete(inputFilePath);
                        System.IO.File.Delete(outputFilePath);
                    }
                    catch (Exception cleanupEx)
                    {
                        // 記錄清理錯誤，但不影響回應
                        Console.WriteLine($"Warning: Failed to cleanup temp files: {cleanupEx.Message}");
                    }
                    return File(fileBytes, contentType, $"processed_video.{format}");
                }
            }
            catch (DirectoryNotFoundException dirEx)
            {
                Console.WriteLine($"Directory not found error: {dirEx.Message}");
                return StatusCode(500, $"Directory error: {dirEx.Message}");
            }
            catch (UnauthorizedAccessException accessEx)
            {
                Console.WriteLine($"Access denied error: {accessEx.Message}");
                return StatusCode(500, $"Access denied: {accessEx.Message}");
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"IO error: {ioEx.Message}");
                return StatusCode(500, $"File system error: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error in ProcessVideo: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Error processing video: {ex.Message}");
            }
            return StatusCode(500, "Unknown error occurred during video processing.");
        }
    }
}