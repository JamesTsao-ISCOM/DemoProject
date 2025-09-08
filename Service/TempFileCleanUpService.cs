public class TempFileCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 清理超過24小時的臨時檔案
                var tempZipDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tempZip");
                var tempUploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tempUpload");
                if (Directory.Exists(tempZipDir))
                {
                    var files = Directory.GetFiles(tempZipDir);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        if (DateTime.Now - fileInfo.CreationTime > TimeSpan.FromHours(24))
                        {
                            try
                            {
                                System.IO.File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                // 記錄錯誤但繼續處理其他檔案
                                Console.WriteLine($"無法刪除過期檔案 {file}: {ex.Message}");
                            }
                        }
                    }
                }
                if (Directory.Exists(tempUploadDir))
                {
                    var files = Directory.GetFiles(tempUploadDir);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        if (DateTime.Now - fileInfo.CreationTime > TimeSpan.FromHours(24))
                        {
                            try
                            {
                                System.IO.File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                // 記錄錯誤但繼續處理其他檔案
                                Console.WriteLine($"無法刪除過期檔案 {file}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理任務發生錯誤: {ex.Message}");
            }

            // 每6小時執行一次清理
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}