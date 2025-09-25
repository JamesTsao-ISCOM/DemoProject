using Microsoft.AspNetCore.Mvc;
using Project01_movie_lease_system.Models;
using Project01_movie_lease_system.Repositories;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using System.Security.Claims;
using Project01_movie_lease_system.Common;

namespace Project01_movie_lease_system.Controllers
{
    public class MoviesController : Controller
    {
        private readonly MovieRepository _movieRepository;
        private readonly MovieImageUploadSetting _movieImageUploadSettings;
        public MoviesController(MovieRepository movieRepository, MovieImageUploadSetting movieImageUploadSettings)
        {
            _movieRepository = movieRepository;
            _movieImageUploadSettings = movieImageUploadSettings;
        }
        public IActionResult Index(string searchTitle, string searchType, DateTime? startDate, DateTime? endDate, int pageNumber = 1)
        {
            PagedResult<MovieDetails> movies;
            int pageSize = 9;

            // 設置 ViewBag 來保持搜尋條件
            ViewBag.SearchTitle = searchTitle;
            ViewBag.SearchType = searchType;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            
            MovieType? movieType = null;
            if (searchType == "All" || string.IsNullOrEmpty(searchType))
            {
                movieType = null;
            }
            else
            {
                movieType = (MovieType)Enum.Parse(typeof(MovieType), searchType);
            }
            // 根據搜尋條件進行查詢
            if (!string.IsNullOrEmpty(searchTitle) && movieType != null && startDate.HasValue && endDate.HasValue)
            {
                // 綜合搜尋：名稱 + 類型 + 日期
                movies = _movieRepository.SearchMovies(searchTitle, movieType, startDate, endDate, pageNumber, pageSize);
            }
            else if (!string.IsNullOrEmpty(searchTitle) && movieType != null)
            {
                // 名稱 + 類型搜尋
                movies = _movieRepository.SearchMovies(searchTitle, movieType, null, null, pageNumber, pageSize);
            }
            else if (!string.IsNullOrEmpty(searchTitle) && startDate.HasValue && endDate.HasValue)
            {
                // 名稱 + 日期搜尋
                movies = _movieRepository.SearchMovies(searchTitle, null, startDate, endDate, pageNumber, pageSize);
            }
            else if (movieType != null && startDate.HasValue && endDate.HasValue)
            {
                // 類型 + 日期搜尋
                movies = _movieRepository.SearchMovies(null, movieType, startDate, endDate, pageNumber, pageSize);
            }
            else if (!string.IsNullOrEmpty(searchTitle))
            {
                // 僅名稱搜尋
                movies = _movieRepository.SearchMovies(searchTitle, null, null, null, pageNumber, pageSize);
            }
            else if (movieType != null)
            {
                // 僅類型搜尋
                movies = _movieRepository.SearchMovies(null, movieType, null, null, pageNumber, pageSize);
            }
            else if (startDate.HasValue && endDate.HasValue)
            {
                // 僅日期搜尋
                movies = _movieRepository.SearchMovies(null, null, startDate, endDate, pageNumber, pageSize);
            }
            else
            {
                // 無搜尋條件，顯示所有電影
                movies = _movieRepository.GetMovieDetailsList(pageNumber, pageSize);
            }

            return View(movies);
        }
        
        public IActionResult MovieDetails(int id)
        {
            var movie = _movieRepository.GetMovieDetails(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Movie model, IFormFile? imageFile)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
            {
                return BadRequest(new { message = "請重新登入" });
            }
            if (User.FindFirstValue(ClaimTypes.Role) == "Member")
            {
                return BadRequest(new { message = "權限不足，請通知管理員" });
            }
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest(new { message = "請上傳電影圖片" });
            }

            var allowedExtensions = new[] { "image/jpg", "image/jpeg", "image/png" };
            if (!allowedExtensions.Contains(imageFile.ContentType))
            {
                return BadRequest(new { message = "只允許上傳JPG、JPEG或PNG格式的圖片" });
            }
            // 確保上傳目錄存在
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), _movieImageUploadSettings.UploadPath);
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(savePath, fileName);
            // 驗證圖片內容
            try
            {
                using var stream = imageFile.OpenReadStream();
                // 嘗試讀取圖片但不真正做任何處理，僅檢查格式是否有效
                var imageInfo = await Image.IdentifyAsync(stream);
                // 重置流位置，以便後續處理
                stream.Position = 0;
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "上傳的檔案不是有效的圖片格式" });
            }
            // 壓縮並儲存圖片
            using (var image = await Image.LoadAsync(imageFile.OpenReadStream()))
            {
                // ✅ 調整尺寸（例如：寬 800px，高度等比縮放）
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(800, 0) // 高度設為 0 表示等比例
                }));

                // 壓縮品質（JPEG 範例，品質 75%）
                if (imageFile.ContentType == "image/png")
                {
                    await image.SaveAsync(filePath, new PngEncoder
                    {
                        CompressionLevel = PngCompressionLevel.Level6
                    });
                }
                else
                {
                    await image.SaveAsync(filePath, new JpegEncoder
                    {
                        Quality = 75
                    });
                }
            }

            var movie = new Movie
            {
                Title = model.Title,
                Description = model.Description,
                Type = model.Type,
                ReleaseDate = model.ReleaseDate,
                Director = model.Director,
                Price = model.Price,
                ImageFileName = fileName,
                YoutubeTrailerUrl = model.YoutubeTrailerUrl,
                Stock = model.Stock
            };
            _movieRepository.Add(movie);

            return PartialView("_MovieEditCard", movie);
        }
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
            {
                return BadRequest(new { message = "請重新登入" });
            }
            if (User.FindFirstValue(ClaimTypes.Role) == "Member")
            {
                return BadRequest(new { message = "權限不足，請通知管理員" });
            }
            var movie = _movieRepository.GetById(id);
            if (movie == null)
            {
                return NotFound();
            }
            return PartialView("_EditMovieForm", movie);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Movie model, IFormFile? imageFile)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
            {
                return BadRequest(new { message = "請重新登入" });
            }
            if (User.FindFirstValue(ClaimTypes.Role) == "Member")
            {
                return BadRequest(new { message = "權限不足，請通知管理員" });
            }
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), _movieImageUploadSettings.UploadPath);
            var filePath = "";
            var oldMovie = _movieRepository.GetById(id);
            if (oldMovie == null)
            {
                return NotFound(new { message = "找不到該電影" });
            }
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            if (imageFile != null)
            {
                var allowedExtensions = new[] { "image/jpg", "image/jpeg", "image/png" };
                if (!allowedExtensions.Contains(imageFile.ContentType))
                {
                    return BadRequest(new { message = "只允許上傳JPG、JPEG或PNG格式的圖片" });
                }
                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                filePath = Path.Combine(savePath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }
                // 驗證圖片內容
                try
                {
                    using var stream = imageFile.OpenReadStream();
                    // 嘗試讀取圖片但不真正做任何處理，僅檢查格式是否有效
                    var imageInfo = await Image.IdentifyAsync(stream);
                    // 重置流位置，以便後續處理
                    stream.Position = 0;
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = "上傳的檔案不是有效的圖片格式" });
                }
                using (var image = await Image.LoadAsync(imageFile.OpenReadStream()))
                {
                    // ✅ 調整尺寸（例如：寬 800px，高度等比縮放）
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(800, 0) // 高度設為 0 表示等比例
                    }));

                    // 壓縮品質（JPEG 範例，品質 75%）
                    if (imageFile.ContentType == "image/png")
                    {
                        await image.SaveAsync(filePath, new PngEncoder
                        {
                            CompressionLevel = PngCompressionLevel.Level6
                        });
                    }
                    else
                    {
                        await image.SaveAsync(filePath, new JpegEncoder
                        {
                            Quality = 75
                        });
                    }
                    // 刪除舊圖片
                    if (oldMovie != null && !string.IsNullOrEmpty(oldMovie.ImageFileName))
                    {
                        var oldFilePath = Path.Combine(savePath, oldMovie.ImageFileName);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                }
            }
            oldMovie.Title = model.Title;
            oldMovie.Description = model.Description;
            oldMovie.Type = model.Type;
            oldMovie.ReleaseDate = model.ReleaseDate;
            oldMovie.Director = model.Director;
            oldMovie.Price = model.Price;
            oldMovie.ImageFileName = string.IsNullOrEmpty(filePath) ? oldMovie.ImageFileName : Path.GetFileName(filePath);
            oldMovie.YoutubeTrailerUrl = model.YoutubeTrailerUrl;
            oldMovie.Stock = model.Stock;

            // 更新資料庫中的電影資料
            _movieRepository.Update(oldMovie);
            // 返回更新後的卡片
            return PartialView("_MovieEditCard", oldMovie);
        }
        [HttpGet]
        public IActionResult Details(int id)
        {
            var movie = _movieRepository.GetById(id);
            if (movie == null)
            {
                return NotFound();
            }
            return Ok(new { Success = true, Data = movie });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
            {
                return BadRequest(new { message = "請重新登入" });
            }
            if (User.FindFirstValue(ClaimTypes.Role) == "Member")
            {
                return BadRequest(new { message = "權限不足，請通知管理員" });
            }
            var movie = _movieRepository.GetById(id);
            if (movie == null)
            {
                return NotFound(new { message = "找不到該電影" });
            }
            // 刪除圖片檔案
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), _movieImageUploadSettings.UploadPath);
            var filePath = Path.Combine(savePath, movie.ImageFileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            _movieRepository.Delete(id);
            return Ok(new { message = "電影刪除成功" });
        }

        [HttpGet]
        public IActionResult Search(string title, int pageNumber = 1, int pageSize = 9)
        {
            if (title == null || title.Trim() == "")
            {
                return BadRequest(new { Message = "請輸入搜尋關鍵字" });
            }
            var movies = _movieRepository.SearchByTitle(title, pageNumber, pageSize);
            return PartialView("_EditMovieList", movies);
        }

        [HttpGet]
        public IActionResult SearchByType(string type, int pageNumber = 1, int pageSize = 9)
        {
            if (string.IsNullOrEmpty(type) || !Enum.TryParse<MovieType>(type, true, out _))
            {
                return BadRequest(new { Message = "請輸入篩選類型" });
            }
            var movies = _movieRepository.FilterByType(type, pageNumber, pageSize);
            return PartialView("_EditMovieList", movies);
        }

        [HttpGet]
        public IActionResult SearchByDate(DateTime startDate, DateTime endDate, int pageNumber = 1, int pageSize = 9)
        {
            if (startDate == default || endDate == default)
            {
                return BadRequest(new { Message = "請輸入有效的日期範圍" });
            }
            var movies = _movieRepository.FilterByDateRange(startDate, endDate, pageNumber, pageSize);
            return PartialView("_EditMovieList", movies);
        }
    }
}