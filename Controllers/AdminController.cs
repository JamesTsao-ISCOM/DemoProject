using Project01_movie_lease_system.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Project01_movie_lease_system.Models;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Project01_movie_lease_system.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminRepository _adminRepository;
        private readonly MovieRepository _movieRepository;
        private readonly MemberRepository _memberRepository;
        private readonly FileRepository _fileRepository;
        private readonly LeaseRepository _leaseRepository;
        public AdminController(AdminRepository adminRepository, MovieRepository movieRepository,
        MemberRepository memberRepository, FileRepository fileRepository, LeaseRepository leaseRepository)
        {
            _adminRepository = adminRepository;
            _movieRepository = movieRepository;
            _memberRepository = memberRepository;
            _fileRepository = fileRepository;
            _leaseRepository = leaseRepository;
        }
        public IActionResult Index()
        {
            return RedirectToAction("Dashboard", "Admin");
        }
        public IActionResult Dashboard()
        {
            // 檢查是否已登入
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            var movies = _movieRepository.GetPaged(1, 9);
            return View(movies);
        }
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated && User.FindFirstValue(ClaimTypes.Role) != "Member")
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }
        public IActionResult FilesManagement()
        {
            // 檢查是否已登入
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            var files = _fileRepository.GetFilesByPage(1, 10);
            var categories = _fileRepository.GetAllCategories();
            ViewBag.Categories = categories;
            return View(files);
        }
        [HttpGet]
        public IActionResult StaffsManagement()
        {
            // 檢查是否已登入
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            var admins = _adminRepository.GetPaged(1, 10);
            return View(admins);
        }
        [HttpGet]
        public IActionResult MembersManagement()
        {
            // 檢查是否已登入
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            var members = _memberRepository.GetPaged(1, 10);
            return View(members);
        }
        [HttpGet]
        public IActionResult LeasesManagement(int pageNumber = 1, int pageSize = 10)
        {
            // 檢查是否已登入
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null && User.FindFirstValue(ClaimTypes.Role) != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            var leases = _leaseRepository.GetPaged(pageNumber, pageSize);
            return View(leases);
    }
    public IActionResult VideoProcess()
    {
        if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string userName, string password)
        {
            var admin = _adminRepository.GetByUsername(userName);
            if (admin == null || !BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            {
                ViewBag.ErrorMessage = "無效的使用者名稱或密碼";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Role, admin.Role.ToString()) // 角色
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // 設定為 true 以保持登入狀態
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) // Cookie 有效時間 30 分鐘
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Dashboard", "Admin");
        }
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Admin");
        }
        [HttpPost]
        public async Task<IActionResult> Add(Admin admin)
        {
            Console.WriteLine("Adding admin:", admin.Username, admin.PasswordHash, admin.Role);
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {

                return BadRequest("請先登入");
            }
            if (User.FindFirstValue(ClaimTypes.Role) != "Admin")
            {
                return BadRequest("您無此權限，請聯絡系統管理員");
            }

            if (_adminRepository.GetByUsername(admin.Username) != null)
            {
                return BadRequest("使用者名稱已存在");
            }
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(admin.PasswordHash);
            admin.CreatedAt = DateTime.Now;
            _adminRepository.Add(admin);
            return PartialView("_AdminRow", admin);
        }
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return BadRequest("請先登入");
            }
            var admin = _adminRepository.GetById(id);
            if (admin == null)
            {
                return BadRequest("管理員不存在");
            }
            return PartialView("_EditAdminForm", admin);
        }
        [HttpPut]
        public async Task<IActionResult> Edit(int id, Admin admin)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入");
            }
            if (User.FindFirstValue(ClaimTypes.Role) == "Admin" ||
            User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
            {
                var existingAdmin = _adminRepository.GetById(id);
                if (existingAdmin == null)
                {
                    return NotFound("管理員不存在");
                }
                if (existingAdmin.Username != admin.Username &&
                    _adminRepository.GetByUsername(admin.Username) != null)
                {
                    return BadRequest("使用者名稱已存在");
                }
                existingAdmin.Username = admin.Username;
                if (!string.IsNullOrEmpty(admin.PasswordHash))
                {
                    existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(admin.PasswordHash);
                }
                existingAdmin.Role = admin.Role;
                _adminRepository.Update(existingAdmin);
                return PartialView("_AdminRow", existingAdmin);
            }
            return BadRequest(new { success = false, message = "無效的管理員資料" });
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入");
            }
            if (User.FindFirstValue(ClaimTypes.Role) != "Admin")
            {
                return Forbid("您無此權限，請聯絡系統管理員");
            }
            var existingAdmin = _adminRepository.GetById(id);
            if (existingAdmin == null)
            {
                return NotFound("管理員不存在");
            }

            // 檢查是否要刪除自己的帳號
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isDeletingSelf = currentUserId == id.ToString();

            _adminRepository.Delete(id);

            // 只有在刪除自己的帳號時才登出
            if (isDeletingSelf)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            return Ok(new { success = true, message = "管理員刪除成功" });
        }
        [HttpPost]
        public IActionResult ResetPassword(int id)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入");
            }
            if (User.FindFirstValue(ClaimTypes.Role) != "Admin")
            {
                return BadRequest("您無此權限，請聯絡系統管理員");
            }
            var existingAdmin = _adminRepository.GetById(id);
            if (existingAdmin == null)
            {
                return BadRequest("管理員不存在");
            }
            existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234");
            _adminRepository.Update(existingAdmin);
            return Ok(new { success = true, message = "密碼重設成功" });
        }
        [HttpGet]
        public IActionResult ExportXlsx()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            // 檢查是否已登入
            if (!User.Identity.IsAuthenticated && User.FindFirstValue(ClaimTypes.Role) != "Admin")
            {
                return RedirectToAction("Login");
            }
            var admins = _adminRepository.GetAll();
            // 將資料轉換為 Excel 格式
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("員工名單");
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "使用者名稱";
            worksheet.Cell(1, 3).Value = "職位";
            worksheet.Cell(1, 4).Value = "電子郵件";
            worksheet.Cell(1, 5).Value = "創建時間";
            var roleMap = new Dictionary<string, string>
            {
                { "Admin", "管理員" },
                { "Moderator", "版主" },
                { "Support", "支援人員" }
            };
            var row = 2;
            foreach (var admin in admins)
            {
                worksheet.Cell(row, 1).Value = admin.Id;
                worksheet.Cell(row, 2).Value = admin.Username;
                worksheet.Cell(row, 3).Value = roleMap.ContainsKey(admin.Role.ToString()) ? roleMap[admin.Role.ToString()] : admin.Role.ToString();
                worksheet.Cell(row, 4).Value = admin.Email;
                worksheet.Cell(row, 5).Value = admin.CreatedAt;
                row++;
            }
            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "員工名單.xlsx");
        }
        private TableCell CreateCell(string text, bool isHeader = false)
        {
            var cell = new TableCell();
            var runProps = new RunProperties();

            // 如果是表頭，套用粗體樣式
            if (isHeader) runProps.Append(new Bold());

            // 建立段落並加入文字
            var paragraph = new Paragraph(
                new Run(new Text(text)) { RunProperties = runProps }
            );

            // 將段落加入儲存格
            cell.Append(paragraph);

            return cell;
        }
        [HttpGet]
        public IActionResult ExportWord()
        {
            var admins = _adminRepository.GetAll();
            using var stream = new MemoryStream();
            // 3. 建立 Word 文件，指定為 .docx 格式
            try
            {
                using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
                {
                    // 4. 加入主要文件部分 (MainDocumentPart)
                    var mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();

                    // 5. 建立文件內容的 Body 區塊
                    var body = new Body();

                    // 6. 標題段落（粗體）
                    var titleParagraph = new Paragraph(
                        new Run(new Text("員工名單")) { RunProperties = new RunProperties(new Bold()) }
                    );
                    body.Append(titleParagraph);

                    // 7. 建立表格
                    var table = new Table();

                    // 8. 設定表格的邊框樣式
                    var tableProps = new TableProperties(
                        new TableBorders(
                            new TopBorder { Val = BorderValues.Single, Size = 4 },
                            new BottomBorder { Val = BorderValues.Single, Size = 4 },
                            new LeftBorder { Val = BorderValues.Single, Size = 4 },
                            new RightBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                        )
                    );

                    table.AppendChild(tableProps);
                    // 9. 表頭列（粗體）
                    var headerRow = new TableRow();
                    headerRow.Append(
                        CreateCell("員工ID", true),
                        CreateCell("姓名", true),
                        CreateCell("電子郵件", true),
                        CreateCell("職位", true)
                    );
                    table.Append(headerRow);

                    // 10. 每一筆員工資料建立一列
                    foreach (var admin in admins)
                    {
                        var row = new TableRow();
                        row.Append(
                            CreateCell(admin.Id.ToString()),
                            CreateCell(admin.Username),
                            CreateCell(admin.Email),
                            CreateCell(admin.Role.ToString())
                        );
                        table.Append(row);
                    }

                    // 11. 將表格加入文件內容
                    body.Append(table);

                    // 12. 將 Body 加入主文件
                    mainPart.Document.Append(body);

                    // 13. 儲存文件
                    mainPart.Document.Save();
                }
            }
            catch (Exception ex)
            {
                // 處理例外情況
                Console.WriteLine("Error generating Word document: " + ex.Message);
                return StatusCode(500, "內部伺服器錯誤");
            }
            // 14. 將記憶體串流轉為 byte[] 並回傳給瀏覽器下載
            return File(
                stream.ToArray(), // 檔案內容
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // MIME 類型
                "員工名單.docx" // 下載檔案名稱
            );
        }
        [HttpGet]
        public IActionResult ExportPdf()
        {
            try
            {
                var admins = _adminRepository.GetAll();
                using var stream = new MemoryStream();

                // 定義角色映射
                var roleMap = new Dictionary<string, string>
            {
                { "Admin", "管理員" },
                { "Moderator", "版主" },
                { "Support", "支援人員" }
            };

                var document = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(20);
                        page.Header().Text("員工名單")
                            .FontSize(20)
                            .Bold()
                            .AlignCenter();
                        page.Content().Table(table =>
                        {
                            // 定義 5 列
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);  // ID 欄位
                                columns.RelativeColumn(2);   // 使用者名稱
                                columns.RelativeColumn(2);   // 電子郵件
                                columns.RelativeColumn(1);   // 職位
                                columns.RelativeColumn(2);   // 創建時間
                            });

                            // 表頭
                            table.Header(header =>
                            {
                                header.Cell().Text("ID").Bold();
                                header.Cell().Text("使用者名稱").Bold();
                                header.Cell().Text("電子郵件").Bold();
                                header.Cell().Text("職位").Bold();
                                header.Cell().Text("創建時間").Bold();
                            });

                            // 資料
                            foreach (var admin in admins)
                            {
                                table.Cell().Text(admin.Id.ToString());
                                table.Cell().Text(admin.Username);
                                table.Cell().Text(admin.Email ?? "");
                                table.Cell().Text(roleMap.ContainsKey(admin.Role.ToString()) ?
                                    roleMap[admin.Role.ToString()] : admin.Role.ToString());
                                table.Cell().Text(admin.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                            }
                        });
                    });
                });

                document.GeneratePdf(stream);

                // 確保流指針回到開頭
                stream.Position = 0;
                var content = stream.ToArray();

                return File(content,
                    "application/pdf",
                    "員工名單.pdf");
            }
            catch (Exception ex)
            {
                // 處理例外情況
                Console.WriteLine("Error generating PDF: " + ex.Message);
                return StatusCode(500, "內部伺服器錯誤");
            }
        }
        [HttpGet]
        public IActionResult DownloadEmptyExcel()
        {
            var customFilesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files", "templates");
            if (!Directory.Exists(customFilesPath))
            {
                Directory.CreateDirectory(customFilesPath);
            }
            var filePath = Path.Combine(customFilesPath, "員工名單_空白.xlsx");
            // 檢查檔案是否存在
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("找不到指定的檔案");
            }
            // 根據檔案類型設置正確的 MIME 類型
            var contentType = "application/octet-stream"; // 預設為二進制流
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            // 這裡可以根據需要實現下載空白 Excel 檔案的邏輯
            return File(fileBytes, contentType, "員工名單_空白.xlsx");
        }
        // 一次性讀入員工資料
        [HttpPost]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            // 檢查請求是否包含檔案
            Console.WriteLine($"Importing Excel file: {file?.FileName}, Content Type: {file?.ContentType}, Length: {file?.Length}");
            Console.WriteLine($"Request Content Type: {Request.ContentType}");
            Console.WriteLine($"Form Files Count: {Request.Form.Files.Count}");

            foreach (var f in Request.Form.Files)
            {
                Console.WriteLine($"File in request: {f.Name} - {f.FileName}");
            }

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized("請先登入");
            }
            if (User.FindFirstValue(ClaimTypes.Role) != "Admin")
            {
                return Forbid("您無此權限，請聯絡系統管理員");
            }
            if (file == null || file.Length == 0)
            {
                // 嘗試從 Request.Form.Files 中獲取檔案
                if (Request.Form.Files.Count > 0)
                {
                    file = Request.Form.Files[0];
                }
                else
                {
                    return BadRequest("請選擇一個有效的 Excel 檔案");
                }
            }
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
            {
                return BadRequest("僅支援 .xlsx及 .xls 格式的檔案");
            }
            var admins = new List<Admin>();
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + extension);
            var successCount = 0;
            var errors = new List<string>();
            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                using (var workbook = new XLWorkbook(tempFilePath))
                {
                    var worksheet = workbook.Worksheet(1);
                    var initTitles = new[] { "使用者名稱", "電子郵件", "職位", "密碼" };
                    var titles = worksheet.Row(1).Cells().Select(c => c.GetString().Trim()).ToList();
                    Console.WriteLine("Excel Titles: " + string.Join(", ", titles));
                    for (int i = 0; i < titles.Count; i++)
                    {
                        if (titles[i] != initTitles[i])
                        {
                            return BadRequest("Excel 標題欄位錯誤，請下載範本檔案後再進行匯入");
                        }
                    }
                    var rows = worksheet.RowsUsed().Skip(1); // 跳過標題列
                    var roleMap = new Dictionary<string, string>
                    {
                        { "admin",  "Admin"},
                        { "moderator", "Moderator" },
                        { "support", "Support" },
                        { "Admin",  "Admin"},
                        { "Moderator", "Moderator" },
                        { "Support", "Support" },
                        { "管理員", "Admin" },
                        { "版主", "Moderator" },
                        { "支援人員", "Support" },

                    };
                    foreach (var row in rows)
                    {
                        try
                        {
                            // 讀取每行資料
                            string username = row.Cell(1).GetString().Trim();
                            string email = row.Cell(2).IsEmpty() ? "" : row.Cell(2).GetString().Trim();
                            string role = row.Cell(3).IsEmpty() ? "" : roleMap[row.Cell(3).GetString().Trim()];
                            string password = row.Cell(4).IsEmpty() ? "1234" : row.Cell(4).GetString();
                            // 資料驗證
                            if (string.IsNullOrEmpty(username))
                            {
                                errors.Add($"第 {row.RowNumber()} 行: 用戶名稱不能為空");
                                continue;
                            }
                            if (string.IsNullOrEmpty(role) || !roleMap.ContainsKey(role))
                            {
                                errors.Add($"第 {row.RowNumber()} 行: 職位欄位錯誤，請填寫 管理員、版主 或 支援人員");
                                continue;
                            }
                            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                            {
                                errors.Add($"第 {row.RowNumber()} 行: 電子郵件格式錯誤");
                                continue;
                            }
                            // 檢查用戶名是否存在
                            if (_adminRepository.GetByUsername(username) != null)
                            {
                                errors.Add($"第 {row.RowNumber()} 行: 用戶名 '{username}' 已存在，不做變更");
                                continue;
                            }
                            // 新增員工資料
                            var admin = new Admin
                            {
                                Username = username,
                                Email = email,
                                Role = Enum.TryParse<StaffRole>(role, out var staffRole) ? staffRole : StaffRole.Support,
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                                CreatedAt = DateTime.Now
                            };
                            _adminRepository.Add(admin);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("was not present in the dictionary"))
                            {
                                errors.Add($"第 {row.RowNumber()} 行: 職位 '{row.Cell(3).GetString().Trim()}' 不存在");
                            }
                            else
                            {
                                errors.Add($"第 {row.RowNumber()} 列資料錯誤: {ex.Message}");
                            }
                        }
                    }
                }
                // 步驟 3: 處理完成後刪除臨時檔案
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
                // 回傳結果
                return Json(new
                {
                    successCount,
                    errors,
                    message = successCount > 0
                        ? $"成功匯入 {successCount} 筆資料" + (errors.Count > 0 ? $"，{errors.Count} 筆資料有錯誤" : "")
                        : "沒有資料被匯入"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "處理檔案時發生錯誤: " + ex.Message);
            }
        }
        // 分段讀入員工資料
        public async Task<IActionResult> ImportExcelByBatch(IFormFile file)
        {
            // 檔案驗證和權限檢查（與原始和優化程式碼相同，略過）
            if (User.Identity == null || !User.Identity.IsAuthenticated) { return Unauthorized("請先登入"); }
            if (User.FindFirstValue(ClaimTypes.Role) != "Admin") { return Forbid("您無此權限，請聯絡系統管理員"); }
            if (file == null || file.Length == 0)
            {
                if (Request.Form.Files.Count > 0) { file = Request.Form.Files[0]; }
                else { return BadRequest("請選擇一個有效的 Excel 檔案"); }
            }
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls") { return BadRequest("僅支援 .xlsx及 .xls 格式的檔案"); }
            var chunkSize = 20; // 每批次處理 20 筆
            var batchSize = 20; // 每次讀取 20 行
            var successCount = 0;
            var errors = new List<string>();
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + extension);
            try
            {
                // 儲存檔案至temp路徑
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                using (var workbook = new XLWorkbook(tempFilePath))
                {
                    var worksheet = workbook.Worksheet(1);
                    var initTitles = new[] { "使用者名稱", "電子郵件", "職位", "密碼" };
                    var titles = worksheet.Row(1).Cells().Select(c => c.GetString().Trim()).ToList();
                    for (int i = 0; i < titles.Count; i++)
                    {
                        if (titles[i] != initTitles[i])
                        {
                            return BadRequest("Excel 標題欄位錯誤，請下載範本檔案後再進行匯入");
                        }
                    }
                    var roleMap = new Dictionary<string, string>
                    {
                        { "admin", "Admin" }, { "moderator", "Moderator" }, { "support", "Support" },
                        { "Admin", "Admin" }, { "Moderator", "Moderator" }, { "Support", "Support" },
                        { "管理員", "Admin" }, { "版主", "Moderator" }, { "支援人員", "Support" }
                    };
                    var admins = new List<Admin>();
                    int totalRows = worksheet.LastRowUsed()?.RowNumber()-1 ?? 1; // 確保不為 null // 減去標題列
                    int currentRow = 2; // 從第二行開始讀取
                    while (currentRow <= totalRows + 1)
                    {
                        int endRow = Math.Min(currentRow + batchSize - 1, totalRows + 1);
                        var rows = worksheet.Rows(currentRow, endRow);
                        foreach (var row in rows)
                        {
                            try
                            {
                                // 讀取和驗證資料
                                string username = row.Cell(1).GetString().Trim();
                                string email = row.Cell(2).IsEmpty() ? "" : row.Cell(2).GetString().Trim();
                                string role = row.Cell(3).IsEmpty() ? "" : roleMap[row.Cell(3).GetString().Trim()];
                                string password = row.Cell(4).IsEmpty() ? "1234" : row.Cell(4).GetString();

                                if (string.IsNullOrEmpty(username)) { errors.Add($"第 {row.RowNumber()} 行: 用戶名稱不能為空"); continue; }
                                if (string.IsNullOrEmpty(role) || !roleMap.ContainsValue(role)) { errors.Add($"第 {row.RowNumber()} 行: 職位欄位錯誤"); continue; }
                                if (string.IsNullOrEmpty(email) || !email.Contains("@")) { errors.Add($"第 {row.RowNumber()} 行: 電子郵件格式錯誤"); continue; }
                                if (_adminRepository.GetByUsername(username) != null || admins.Any(a => a.Username == username)) { errors.Add($"第 {row.RowNumber()} 行: 用戶名 '{username}' 已存在"); continue; }

                                var admin = new Admin
                                {
                                    Username = username,
                                    Email = email,
                                    Role = Enum.TryParse<StaffRole>(role, out var staffRole) ? staffRole : StaffRole.Support,
                                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                                    CreatedAt = DateTime.Now
                                };
                                admins.Add(admin);
                                // 每累積 chunkSize 筆資料就批次新增
                                if (admins.Count >= chunkSize)
                                {
                                    await _adminRepository.AddRangeAsync(admins);
                                    successCount += admins.Count;
                                    admins.Clear();
                                    GC.Collect();
                                    GC.WaitForPendingFinalizers();
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"第 {row.RowNumber()} 行: 資料錯誤 - {ex.Message}");
                            }
                        }
                        currentRow += chunkSize;
                        if (admins.Any())
                        {
                            await _adminRepository.AddRangeAsync(admins);
                            successCount += admins.Count;
                            admins.Clear();
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                    return Json(new
                    {
                        successCount,
                        errors,
                        message = successCount > 0
                            ? $"成功匯入 {successCount} 筆資料" + (errors.Count > 0 ? $"，{errors.Count} 筆資料有錯誤" : "")
                            : "沒有資料被匯入"
                    });
                }

            }
            catch (Exception ex)
            {
                return BadRequest("處理檔案時發生錯誤: " + ex.Message);
            }
        }
        
    }
}