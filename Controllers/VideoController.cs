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
                        // 【修正】修復灰度效果濾鏡 - 使用正確的 FFmpeg 灰度濾鏡
                        "grayscale" => "hue=s=0", // 更簡單且可靠的灰度濾鏡
                        // 備選方案：也可以使用 "desaturate" 或 "colorchannelmixer=0.299:0.587:0.114:0:0.299:0.587:0.114:0:0.299:0.587:0.114"
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
                    
                    // 【修正1】檢查輸入檔案完整性
                    var inputFileInfo = new FileInfo(inputFilePath);
                    if (inputFileInfo.Length == 0)
                    {
                        return StatusCode(500, "Input file is empty or corrupted");
                    }
                    
                    // 【修正2】執行轉換前先等待，確保檔案寫入完成
                    await Task.Delay(100); // 等待100ms確保檔案寫入完成
                    
                    await conversion.Start();
                    
                    // 【修正3】轉換完成後等待一段時間，確保檔案寫入完成
                    await Task.Delay(500); // 等待500ms確保FFmpeg完全釋放檔案
                    
                    // 檢查輸出檔案是否存在
                    if (!System.IO.File.Exists(outputFilePath))
                    {
                        return StatusCode(500, "Video processing failed - output file not created.");
                    }
                    
                    // 【修正4】檢查輸出檔案完整性和大小
                    var outputFileInfo = new FileInfo(outputFilePath);
                    if (outputFileInfo.Length == 0)
                    {
                        return StatusCode(500, "Video processing failed - output file is empty.");
                    }
                    
                    // 【修正5】驗證輸出檔案是否為有效的影片檔案
                    try
                    {
                        IMediaInfo outputMediaInfo = await FFmpeg.GetMediaInfo(outputFilePath);
                        if (!outputMediaInfo.VideoStreams.Any())
                        {
                            return StatusCode(500, "Video processing failed - output file has no video streams.");
                        }
                        
                        // 檢查影片時長是否合理
                        double expectedDuration = endTime - startTime;
                        double actualDuration = outputMediaInfo.Duration.TotalSeconds;
                        double durationTolerance = Math.Max(1.0, expectedDuration * 0.1); // 10% 容差
                        
                        if (Math.Abs(actualDuration - expectedDuration) > durationTolerance)
                        {
                            Console.WriteLine($"Warning: Duration mismatch. Expected: {expectedDuration}s, Actual: {actualDuration}s");
                            // 不回傳錯誤，只記錄警告，因為可能是正常的編碼差異
                        }
                    }
                    catch (Exception validationEx)
                    {
                        Console.WriteLine($"Warning: Failed to validate output file: {validationEx.Message}");
                        return StatusCode(500, "Video processing failed - output file validation failed.");
                    }

                    // 【修正6】多次嘗試讀取檔案，避免檔案鎖定問題
                    byte[] fileBytes = null;
                    int maxRetries = 3;
                    for (int retry = 0; retry < maxRetries; retry++)
                    {
                        try
                        {
                            fileBytes = await System.IO.File.ReadAllBytesAsync(outputFilePath);
                            if (fileBytes.Length > 0)
                            {
                                break; // 成功讀取
                            }
                        }
                        catch (IOException ioEx) when (retry < maxRetries - 1)
                        {
                            Console.WriteLine($"Retry {retry + 1}: Failed to read output file: {ioEx.Message}");
                            await Task.Delay(200); // 等待200ms後重試
                        }
                    }
                    
                    if (fileBytes == null || fileBytes.Length == 0)
                    {
                        return StatusCode(500, "Failed to read processed video file.");
                    }

                    // 設定 Content-Type
                    string contentType = format switch
                    {
                        "mp4" => "video/mp4",
                        "webm" => "video/webm",
                        _ => "application/octet-stream"
                    };
                    
                    // 【修正7】延遲清理臨時檔案，確保檔案已被完全讀取
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // 等待一段時間確保檔案已被使用完畢
                            await Task.Delay(2000);
                            
                            if (System.IO.File.Exists(inputFilePath))
                            {
                                System.IO.File.Delete(inputFilePath);
                                Console.WriteLine($"Successfully deleted input file: {inputFilePath}");
                            }
                            
                            if (System.IO.File.Exists(outputFilePath))
                            {
                                System.IO.File.Delete(outputFilePath);
                                Console.WriteLine($"Successfully deleted output file: {outputFilePath}");
                            }
                        }
                        catch (Exception cleanupEx)
                        {
                            Console.WriteLine($"Warning: Failed to cleanup temp files: {cleanupEx.Message}");
                        }
                    });
                    
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