using Project01_movie_lease_system.Repositories;
using Microsoft.AspNetCore.Mvc;
using Project01_movie_lease_system.Models;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using SharpCompress.Writers;
using System.Security.Claims;
using Spire.Doc;
using Spire.Xls;
using File = Project01_movie_lease_system.Models.File;
using System.Text; // for UTF-8 encoding in zip
using Project01_movie_lease_system.Service;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;
using System.Text.RegularExpressions;

namespace Project01_movie_lease_system.Controllers
{
    public class FilesController : Controller
    {
        private readonly FileRepository _fileRepository;
        private readonly IEmailService _emailService;
        private readonly VideoRecordRepository _videoRecordRepository;
        
        public FilesController(FileRepository fileRepository, IEmailService emailService, VideoRecordRepository videoRecordRepository)
        {
            _fileRepository = fileRepository;
            _emailService = emailService;
            _videoRecordRepository = videoRecordRepository;
        }
        [HttpGet]
        public IActionResult GetPagedFiles(int pageNumber=1, int pageSize=10)
        {
            var files = _fileRepository.GetFilesByPage(pageNumber, pageSize);
            return PartialView("_FileList", files);
        }
        // 上傳暫存檔案
        [HttpPost]
        public IActionResult UploadTemp(IFormFile file)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入系統");
            }
            if (file == null || file.Length == 0)
            {
                return BadRequest("檔案不可為空，請重新選擇檔案後上傳");
            }

            // 檢查檔案類型，拒絕 MP4 影片檔案
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var blockedExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm" };
            if (blockedExtensions.Contains(fileExtension))
            {
                return BadRequest($"不支援上傳影片檔案格式 ({fileExtension})，請選擇其他類型的檔案。");
            }
            var tempId = Guid.NewGuid().ToString();
            var tempPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tempUpload", tempId);
            Directory.CreateDirectory(tempPath);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(tempPath, fileName);
            // 複製到暫存路徑
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }
            var fileList = new List<FilePreview>();
            if (file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = SharpCompress.Archives.Zip.ZipArchive.Open(filePath);
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    if (entry.IsEncrypted || entry == null)
                    {
                        fileList.Add(new FilePreview
                        {
                            FileName = "未知檔案",
                            FileType = "unknown",
                            LastModifiedDate = entry.LastModifiedTime ?? DateTime.MinValue
                        });
                        continue; // 跳過加密檔案
                    }
                    fileList.Add(new FilePreview
                    {
                        FileName = entry.Key,
                        FileType = entry.Key.Contains('.') ? Path.GetExtension(entry.Key) : "unknown",
                        LastModifiedDate = entry.LastModifiedTime ?? DateTime.MinValue
                    });
                    fileList = fileList.OrderBy(f => f.FileName).ToList();
                }
            }
            else if (file.FileName.EndsWith(".rar", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = RarArchive.Open(filePath);
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    if (entry.IsEncrypted || entry == null)
                    {
                        fileList.Add(new FilePreview
                        {
                            FileName = "未知檔案",
                            FileType = "unknown",
                            LastModifiedDate = entry.LastModifiedTime ?? DateTime.MinValue
                        });
                        continue; // 跳過加密檔案
                    }
                    fileList.Add(new FilePreview
                    {
                        FileName = entry.Key,
                        FileType = entry.Key.Contains('.') ? Path.GetExtension(entry.Key) : "unknown",
                        LastModifiedDate = entry.LastModifiedTime ?? DateTime.MinValue
                    });
                    fileList = fileList.OrderBy(f => f.FileName).ToList();
                }
            }
            else
            {
                // 單一檔案
                fileList.Add(new FilePreview
                {
                    FileName = file.FileName,
                    FileType = file.FileName.Contains('.') ? Path.GetExtension(file.FileName) : "unknown",
                    LastModifiedDate = DateTime.Now
                });
            }
            return Ok(new { TempId = tempId, FilesList = fileList });
        }
        // 確認上傳
        [HttpPost]
        public IActionResult ConfirmUpload(string tempId, File model)
        {
            var fileInfo = new File();
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                Console.WriteLine("ConfirmUpload: User is not authenticated");
                return Unauthorized("請先登入系統");
            }
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
            {
                Console.WriteLine("ConfirmUpload: User ID is not found");
                return RedirectToAction("Login", "Admin");
            }
            if (string.IsNullOrEmpty(tempId))
            {
                Console.WriteLine("ConfirmUpload: TempId is null or empty");
                return BadRequest("無效的暫存檔案識別碼");
            }
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var tempPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tempUpload", tempId);
            var storePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "files");

            // 確保目標目錄存在
            if (!Directory.Exists(storePath))
            {
                try
                {
                    Directory.CreateDirectory(storePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ConfirmUpload: Failed to create store directory: {ex.Message}");
                    return BadRequest($"無法創建儲存目錄: {ex.Message}");
                }
            }

            if (Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(storePath);
                foreach (var file in Directory.GetFiles(tempPath))
                {
                    try
                    {
                        var fileName = $"{Path.GetFileNameWithoutExtension(file)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file)}";
                        var destFile = Path.Combine(storePath, fileName);
                        if (System.IO.File.Exists(destFile))
                        {
                            // 若檔案已存在，則跳過或重新命名
                            fileName = $"{Path.GetFileNameWithoutExtension(file)}_{Guid.NewGuid()}{Path.GetExtension(file)}";
                            destFile = Path.Combine(storePath, fileName);
                        }
                        fileInfo = new File
                        {
                            FileName = model.FileName,
                            StoredFileName = fileName,
                            Description = model.Description,
                            FileType = Path.GetExtension(file),
                            FileSize = new FileInfo(file).Length,
                            CategoryId = model.CategoryId, // 預設為其他類別
                            AdminId = userId,
                            UploadDate = DateTime.Now
                        };
                        _fileRepository.AddFile(fileInfo);
                        System.IO.File.Move(file, destFile);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ConfirmUpload: Failed to move file: {ex.Message}");
                        // 處理檔案移動過程中的例外
                        return BadRequest($"檔案處理失敗: {ex.Message}");
                    }
                }
                // 在移動檔案成功後，設定一個後台任務來刪除暫存目錄
                // 這樣可以避免立即刪除目錄時可能發生的鎖定問題
                Task.Run(async () =>
                {
                    try
                    {
                        // 等待一段較長的時間，確保所有檔案操作完成
                        await Task.Delay(2000);

                        // 強制釋放所有資源
                        for (int i = 0; i < 3; i++)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }

                        // 嘗試刪除目錄
                        if (Directory.Exists(tempPath))
                        {
                            Directory.Delete(tempPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 記錄錯誤，但不影響主流程
                        Console.WriteLine($"後台刪除暫存目錄失敗: {ex.Message}");
                        // 最後的嘗試：創建一個臨時清理檔案，在應用程式重啟時處理
                        try
                        {
                            string cleanupPath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot",
                                "uploads",
                                "cleanup.txt");

                            using (StreamWriter sw = System.IO.File.AppendText(cleanupPath))
                            {
                                sw.WriteLine(tempPath);
                            }
                        }
                        catch { }
                    }
                });
            }
            else
            {
                return BadRequest("找不到對應的暫存檔案");
            }
            return PartialView("_FileRow", fileInfo);
        }
        [HttpPost]
        public IActionResult CancelUpload(string tempId)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入系統");
            }
            if (string.IsNullOrEmpty(tempId))
            {
                return BadRequest("無效的暫存檔案識別碼");
            }
            var tempPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tempUpload", tempId);
            if (Directory.Exists(tempPath))
            {
                // 在取消上傳時，使用與確認上傳相同的後台任務方法來處理刪除操作
                Task.Run(async () =>
                {
                    try
                    {
                        // 等待一段較長的時間
                        await Task.Delay(2000);

                        // 強制釋放所有資源
                        for (int i = 0; i < 3; i++)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }

                        // 嘗試刪除目錄
                        if (Directory.Exists(tempPath))
                        {
                            Directory.Delete(tempPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 記錄錯誤
                        Console.WriteLine($"後台刪除暫存目錄失敗: {ex.Message}");

                        // 記錄到清理檔案
                        try
                        {
                            string cleanupPath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot",
                                "uploads",
                                "cleanup.txt");

                            using (StreamWriter sw = System.IO.File.AppendText(cleanupPath))
                            {
                                sw.WriteLine(tempPath);
                            }
                        }
                        catch { }
                    }
                });
            }
            return Ok("上傳已取消");
        }

        [HttpDelete]
        public IActionResult DeleteFile(int id)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入系統");
            }
            try
            {
                var file = _fileRepository.GetFileById(id);
                if (file == null)
                {
                    return NotFound("找不到指定的檔案");
                }

                Console.WriteLine($"準備刪除檔案: {file.FileName} (ID: {id})");

                // 1. 先檢查並刪除相關的 VideoWatchRecords
                var videoWatchRecords = _videoRecordRepository.GetVideoWatchRecordsByFileId(id);
                if (videoWatchRecords.Any())
                {
                    Console.WriteLine($"發現 {videoWatchRecords.Count} 筆相關的觀看記錄，準備刪除");
                    _videoRecordRepository.DeleteVideoWatchRecordsByFileId(id);
                    Console.WriteLine("相關觀看記錄已刪除");
                }

                // 2. 刪除實體檔案
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "files", file.StoredFileName);
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                        Console.WriteLine($"實體檔案已刪除: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"刪除實體檔案失敗: {ex.Message}");
                        return BadRequest($"檔案刪除失敗: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"實體檔案不存在: {filePath}");
                }

                // 3. 最後刪除檔案記錄
                _fileRepository.DeleteFile(id);
                Console.WriteLine("檔案記錄已從資料庫刪除");
                Console.WriteLine($"檔案 {file.FileName} 刪除完成");

                return Ok("檔案刪除成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"刪除檔案時發生錯誤: {ex.Message}");
                Console.WriteLine($"錯誤堆疊: {ex.StackTrace}");
                return BadRequest($"刪除檔案時發生錯誤: {ex.Message}");
            }
        }
        [HttpGet]
        public IActionResult GetFileById(int id)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入系統");
            }
            Console.WriteLine($"進入 GetFileById 方法: {id}");
            var file = _fileRepository.GetFileById(id);
            Console.WriteLine(file);
            if (file == null)
            {
                return NotFound("找不到指定的檔案");
            }
            return PartialView("_FileDetailForm", file);
        }
        [HttpPost]
        public async Task<IActionResult> DownloadMultipleFiles([FromBody] List<int> fileIds)
        {
            Console.WriteLine($"進入 DownloadMultipleFiles 方法: {(fileIds != null ? string.Join(", ", fileIds) : "null")}");
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入系統");
            }
            if (fileIds == null || fileIds.Count == 0)
            {
                return BadRequest("未選取任何檔案");
            }
            var tempZipId = Guid.NewGuid().ToString();
            var tempZipDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tempZip");
            if (!Directory.Exists(tempZipDir))
            {
                Directory.CreateDirectory(tempZipDir);
            }
            var zipFilePath = Path.Combine(tempZipDir, $"Files_{DateTime.Now:yyyyMMddHHmmss}.zip");
            try
            {
                using (var zipArchive = ZipArchive.Create())
                {
                    var memoryStreams = new List<MemoryStream>();
                    var fileContents = new List<(string FileName, byte[] Content)>();
                    var usedFileNames = new HashSet<string>(); // 追蹤已使用的檔案名稱

                    foreach (var id in fileIds)
                    {
                        var file = _fileRepository.GetFileById(id);
                        if (file == null)
                            continue;

                        var filePath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "uploads",
                            "files",
                            file.StoredFileName
                        );

                        if (!System.IO.File.Exists(filePath))
                        {
                            Console.WriteLine($"檔案不存在: {filePath}");
                            continue;
                        }

                        try
                        {
                            // 一次讀取整個檔案到 byte[]
                            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                            // 確保檔名安全（去掉目錄）
                            var originalFileName = Path.GetFileName(file.FileName);
                            var safeFileName = originalFileName;
                            var counter = 1;

                            // 處理同名檔案問題
                            while (usedFileNames.Contains(safeFileName))
                            {
                                var fileExtension = Path.GetExtension(originalFileName);
                                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
                                safeFileName = $"{fileNameWithoutExtension}({counter}){fileExtension}";
                                counter++;
                            }

                            usedFileNames.Add(safeFileName);
                            fileContents.Add((safeFileName, fileBytes));

                            Console.WriteLine($"已讀取: {safeFileName}, 大小: {fileBytes.Length} bytes");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"讀取檔案失敗: {filePath}，錯誤: {ex.Message}");
                        }
                    }

                    // 添加所有檔案到壓縮檔
                    foreach (var (fileName, content) in fileContents)
                    {
                        Console.WriteLine($"添加到ZIP: {fileName}, 大小: {content.Length} 位元組");

                        var memoryStream = new MemoryStream(content); // 不要用 using，先存著
                        memoryStreams.Add(memoryStream);

                        zipArchive.AddEntry(fileName, memoryStream, false);
                    }

                    Console.WriteLine($"保存ZIP檔案到: {zipFilePath}");
                    // Ensure UTF-8 for entry names to keep non-ASCII (e.g., Chinese) filenames
                    var writerOptions = new WriterOptions(CompressionType.Deflate)
                    {
                        ArchiveEncoding = new ArchiveEncoding(Encoding.UTF8, Encoding.UTF8)
                    };
                    zipArchive.SaveTo(zipFilePath, writerOptions);

                    // SaveTo 完成後再釋放 stream
                    foreach (var ms in memoryStreams)
                    {
                        ms.Dispose();
                    }
                    return PhysicalFile(zipFilePath, "application/zip", Path.GetFileName(zipFilePath));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DownloadMultipleFiles發生例外: {ex.Message}");
                return BadRequest($"壓縮檔案失敗: {ex.Message}");
            }
        }
        [HttpGet]
        public IActionResult DownloadFile(int id)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入系統");
            }
            var file = _fileRepository.GetFileById(id);
            if (file == null)
            {
                return NotFound("找不到指定的檔案");
            }
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "files", file.StoredFileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("檔案不存在");
            }
            var extension = GetContentType(filePath);
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, extension, file.FileName);
        }
        [HttpPost]
        public async Task<IActionResult> ConvertExcelToPdf(IFormFile file)
        {
            Console.WriteLine("開始轉換 Excel 檔案");
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入系統");
            }
            if (file == null || file.Length == 0)
            {
                return BadRequest("請上傳有效的 Excel 檔案");
            }
            if (file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) == false &&
               file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) == false)
            {
                return BadRequest("僅支援 Excel 檔案 (.xls, .xlsx)");
            }
            var inputPath = Path.Combine(Path.GetTempPath(), file.FileName);
            var outputPath = Path.Combine(Path.GetTempPath(), "output_excel.pdf");
            using (var stream = new FileStream(inputPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            // 轉換為 PDF
            using (var workbook = new Workbook())
            {
                workbook.LoadFromFile(inputPath);
                workbook.ConverterSetting.SheetFitToPage = true; // 確保內容適配頁面
                workbook.SaveToFile(outputPath, Spire.Xls.FileFormat.PDF);
                workbook.Dispose(); //保釋放所有資源（託管和非託管），避免記憶體洩漏或檔案鎖定。
            }
            var pdfBytes = await System.IO.File.ReadAllBytesAsync(outputPath);
            System.IO.File.Delete(inputPath); // 清理暫存檔案
            System.IO.File.Delete(outputPath);
            return File(pdfBytes, "application/pdf", "output_excel.pdf");
        }
        [HttpPost]
        public async Task<IActionResult> ConvertWordToPdf(IFormFile file)
        {
            Console.WriteLine("開始轉換 Word 檔案");
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入系統");
            }
            if (file == null || file.Length == 0)
            {
                return BadRequest("請上傳有效的 Word 檔案");
            }
            if (file.FileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase) == false &&
                file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) == false)
            {
                return BadRequest("僅支援 Word 檔案 (.doc, .docx)");
            }
            var inputPath = Path.Combine(Path.GetTempPath(), file.FileName);
            using (var stream = new FileStream(inputPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var outputPath = Path.Combine(Path.GetTempPath(), "output_word.pdf");
            var document = new Spire.Doc.Document();
            document.LoadFromFile(inputPath);
            document.SaveToFile(outputPath, Spire.Doc.FileFormat.PDF);
            document.Dispose(); //保釋放所有資源（託管和非託管），避免記憶體洩漏或檔案鎖定。

            // 回傳 PDF 檔案
            var pdfBytes = await System.IO.File.ReadAllBytesAsync(outputPath);
            System.IO.File.Delete(inputPath); // 清理暫存檔案
            System.IO.File.Delete(outputPath);

            return File(pdfBytes, "application/pdf", "output_word.pdf");
        }
        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".zip" => "application/zip",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".jpg" => "image/jpeg",
                ".png" => "image/png",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                _ => "application/octet-stream" // 默認值
            };
        }
        [HttpPost]
        public IActionResult GetFileNames([FromBody] List<int> fileIds)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入系統");
            }
            var fileNames = new List<string>();
            foreach (var fileId in fileIds)
            {
                var file = _fileRepository.GetFileById(fileId);
                if (file != null)
                {
                    fileNames.Add(file.FileName);
                }
            }
            return Ok(fileNames);
        }
        [HttpPost]
        public async Task<IActionResult> SendEmailWithAttachments(string receptor, string subject, string body, List<int> attachmentIds, string? cc = null)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入系統");
            }
            var attachmentPaths = new Dictionary<string, string>();
            Console.WriteLine($"Preparing email attachments...{receptor}, {subject}, {body}, {attachmentIds?.Count ?? 0} attachments");
            if (attachmentIds != null)
            {
                for (int i = 0; i < attachmentIds.Count; i++)
                {
                    var attachmentId = attachmentIds[i];
                    var file = _fileRepository.GetFileById(attachmentId);
                    if (file != null)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "files", file.StoredFileName);
                        
                        // 處理同名檔案問題
                        var fileName = file.FileName;
                        var originalFileName = fileName;
                        var counter = 1;
                        
                        // 如果檔名已存在，自動加上編號
                        while (attachmentPaths.ContainsKey(fileName))
                        {
                            var fileExtension = Path.GetExtension(originalFileName);
                            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
                            fileName = $"{fileNameWithoutExtension}({counter}){fileExtension}";
                            counter++;
                        }
                        
                        attachmentPaths.Add(fileName, filePath);
                        Console.WriteLine($"Added Email attachment: {fileName} -> {filePath}");
                    }
                }
            }

            // Parse multiple receptors 切割字串
            IEnumerable<string> receptorList = new[] { receptor };
            if (!string.IsNullOrWhiteSpace(receptor))
                receptorList = receptor.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());

            IEnumerable<string>? ccList = null;
            if (!string.IsNullOrWhiteSpace(cc))
                ccList = cc.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());

            Console.WriteLine($"send Email to {string.Join(", ", receptorList)}");
            await _emailService.SendEmail(receptorList, subject, body, attachmentPaths, ccList);

            return Ok("Email sent successfully");
        }

        [HttpGet]
        public async Task<IActionResult> ExportFileDetailToWord(int id)
        {
            Console.WriteLine($"Exporting file details to Word for file ID: {id}");
            var selectedFiles = _fileRepository.GetFileById(id);
            Console.WriteLine($"Selected file: {selectedFiles?.FileName}");
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "template.docx");
            byte[] templateBytes = System.IO.File.ReadAllBytes(templatePath);
            // 1. 複製一份到記憶體（保護原檔）
            using var memStream = new MemoryStream();
            memStream.Write(templateBytes, 0, templateBytes.Length);
            // 2. 在記憶體中開啟 Word
            using (var wordDoc = WordprocessingDocument.Open(memStream, true))
            {
                // 3. 讀取文件文字
                var docText = string.Empty;
                using (var reader = new StreamReader(wordDoc.MainDocumentPart.GetStream()))
                {
                    docText = reader.ReadToEnd();
                }
                Console.WriteLine($"Read document text: {docText}");
                // 4. 取代關鍵字    
                Console.WriteLine($"Processing document text: {docText}");
                Console.WriteLine($"Replacing placeholders with: {selectedFiles.FileName}, {selectedFiles.UploadDate}, {selectedFiles.FileSize}, {selectedFiles.Description}, {selectedFiles.Uploader}");
                docText = docText.Replace("File", selectedFiles.FileName)
                                 .Replace("{{Time}}", selectedFiles.UploadDate.ToString("yyyy-MM-dd HH:mm:ss"))
                                 .Replace("{{Size}}", selectedFiles.FileSize.ToString())
                                 .Replace("{{Description}}", selectedFiles.Description)
                                 .Replace("{{Uploader}}", selectedFiles.Uploader);
                // ComplexReplace(wordDoc, "{{MemberName}}", selectedFiles.FileName);
                // ComplexReplace(wordDoc, "{{Time}}", selectedFiles.UploadDate.ToString("yyyy-MM-dd HH:mm:ss"));
                // ComplexReplace(wordDoc, "{{Size}}", selectedFiles.FileSize.ToString());
                // ComplexReplace(wordDoc, "{{Description}}", selectedFiles.Description);
                // ComplexReplace(wordDoc, "{{Uploader}}", selectedFiles.Uploader);
                Console.WriteLine($"Processed document text: {docText}");
                // 5. 寫回文件
                using (var writer = new StreamWriter(wordDoc.MainDocumentPart.GetStream(FileMode.Create)))
                {
                    writer.Write(docText);
                }
            }
            // 6. 回傳新檔案（不影響原範本）
            return File(memStream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"檔案資料_{DateTime.Now:yyyyMMdd}.docx");

        }
        private void ComplexReplace(WordprocessingDocument doc, string placeholder, string replacement)
        {
            var body = doc.MainDocumentPart.Document.Body;
            foreach (DocumentFormat.OpenXml.Wordprocessing.Paragraph para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                string paraText = string.Join("", para.Descendants<Text>().Select(t => t.Text));
                string pattern = $@"\{{\{{\s*{Regex.Escape(placeholder)}\s*\}}\}}";
                if (Regex.IsMatch(paraText, pattern))
                {
                    string newText = Regex.Replace(paraText, pattern, replacement);
                    SetParagraphText(para, newText);
                }
            }
        }
        private void SetParagraphText(DocumentFormat.OpenXml.Wordprocessing.Paragraph para, string newText)
        {
            // 刪除舊內容
            para.RemoveAllChildren<DocumentFormat.OpenXml.Wordprocessing.Run>();

            // 建立新的 Run/Text (最簡化版本)
            var run = new DocumentFormat.OpenXml.Wordprocessing.Run();
            var text = new DocumentFormat.OpenXml.Wordprocessing.Text(newText);
            run.AppendChild(text);
            para.AppendChild(run);
        }
        // 上傳的影片分片
        [HttpPost]
        public IActionResult UploadVideoChunk([FromForm] string fileId, [FromForm] int chunkIndex, [FromForm] int totalChunks, [FromForm] string fileName, [FromForm] IFormFile chunk)
        {
            // 處理上傳的檔案片段
            if (chunk != null && chunk.Length > 0)
            {
                var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "temp", fileId);
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                var chunkPath = Path.Combine(tempDir, $"{chunkIndex}.part");
                using (var stream = new FileStream(chunkPath, FileMode.Create))
                {
                    chunk.CopyTo(stream);
                }
                return Ok("檔案片段上傳成功");
            }
            return BadRequest("無效的檔案片段");
        }
        //組合分片
        [HttpPost]
        public IActionResult CompleteVideoUpload(string fileId, string fileName)
        {
            var tempId = Guid.NewGuid().ToString();
            var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "temp", fileId);
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tempUpload", tempId);
            var videoPath = Path.Combine(uploadDir, fileName);

            Console.WriteLine($"temp dir: {tempDir}");
            Console.WriteLine($"video path: {videoPath}");

            if (!Directory.Exists(tempDir))
            {
                return BadRequest("無效的檔案片段");
            }

            // 確保上傳目錄存在
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var partFiles = Directory.GetFiles(tempDir)
                            .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                            .ToList();

            using (var finalStream = new FileStream(videoPath, FileMode.Create))
            {
                foreach (var partFile in partFiles)
                {
                    using (var partStream = new FileStream(partFile, FileMode.Open))
                    {
                        partStream.CopyTo(finalStream);
                    }
                    // 刪除已處理的分塊檔案
                    System.IO.File.Delete(partFile);
                }
            }
            // 清理暫存目錄
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理暫存目錄失敗: {ex.Message}");
            }
            return Ok(new { Message = "檔案合併完成", FilePath = videoPath, TempId = tempId, videoUrl = $"/uploads/tempUpload/{tempId}/{fileName}" });
        }
        // 確認上傳影片
        [HttpPost]
        public IActionResult ConfirmVideoUpload([FromForm] string tempId, [FromForm] string videoName, [FromForm] string videoType, [FromForm] long videoSize, [FromForm] string videoDescription, [FromForm] int categoryId)
        {
            var fileInfo = new File();
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                Console.WriteLine("ConfirmUpload: User is not authenticated");
                return Unauthorized("請先登入系統");
            }
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
            {
                Console.WriteLine("ConfirmUpload: User ID is not found");
                return RedirectToAction("Login", "Admin");
            }
            if (string.IsNullOrEmpty(tempId))
            {
                Console.WriteLine("ConfirmUpload: TempId is null or empty");
                return BadRequest("無效的暫存檔案識別碼");
            }
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var tempPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tempUpload", tempId);
            var storePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "files");

            // 確保目標目錄存在
            if (!Directory.Exists(storePath))
            {
                try
                {
                    Directory.CreateDirectory(storePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ConfirmUpload: Failed to create store directory: {ex.Message}");
                    return BadRequest($"無法創建儲存目錄: {ex.Message}");
                }
            }

            if (Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(storePath);
                foreach (var file in Directory.GetFiles(tempPath))
                {
                    try
                    {
                        var fileName = $"{Path.GetFileNameWithoutExtension(file)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file)}";
                        var destFile = Path.Combine(storePath, fileName);
                        if (System.IO.File.Exists(destFile))
                        {
                            // 若檔案已存在，則跳過或重新命名
                            fileName = $"{Path.GetFileNameWithoutExtension(file)}_{Guid.NewGuid()}{Path.GetExtension(file)}";
                            destFile = Path.Combine(storePath, fileName);
                        }
                        fileInfo = new File
                        {
                            FileName = videoName,
                            StoredFileName = fileName,
                            Description = videoDescription,
                            FileType = videoType,
                            FileSize = videoSize,
                            CategoryId = categoryId, // 預設為其他類別
                            AdminId = userId,
                            UploadDate = DateTime.Now
                        };
                        _fileRepository.AddFile(fileInfo);
                        System.IO.File.Move(file, destFile);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ConfirmUpload: Failed to move file: {ex.Message}");
                        // 處理檔案移動過程中的例外
                        return BadRequest($"檔案處理失敗: {ex.Message}");
                    }
                }
                // 在移動檔案成功後，設定一個後台任務來刪除暫存目錄
                // 這樣可以避免立即刪除目錄時可能發生的鎖定問題
                Task.Run(async () =>
                {
                    try
                    {
                        // 等待一段較長的時間，確保所有檔案操作完成
                        await Task.Delay(2000);

                        // 強制釋放所有資源
                        for (int i = 0; i < 3; i++)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }

                        // 嘗試刪除目錄
                        if (Directory.Exists(tempPath))
                        {
                            Directory.Delete(tempPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 記錄錯誤，但不影響主流程
                        Console.WriteLine($"後台刪除暫存目錄失敗: {ex.Message}");
                        // 最後的嘗試：創建一個臨時清理檔案，在應用程式重啟時處理
                        try
                        {
                            string cleanupPath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot",
                                "uploads",
                                "cleanup.txt");

                            using (StreamWriter sw = System.IO.File.AppendText(cleanupPath))
                            {
                                sw.WriteLine(tempPath);
                            }
                        }
                        catch { }
                    }
                });
            }
            else
            {
                return BadRequest("找不到對應的暫存檔案");
            }
            return PartialView("_FileRow", fileInfo);
        }
    }
}