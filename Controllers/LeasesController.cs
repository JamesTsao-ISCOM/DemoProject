using Microsoft.AspNetCore.Mvc;
using Project01_movie_lease_system.Repositories;
using Project01_movie_lease_system.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Project01_movie_lease_system.Controllers;

public class LeasesController : Controller
{
    private readonly LeaseRepository _leaseRepository;
    private readonly MovieRepository _movieRepository;
    public LeasesController(LeaseRepository leaseRepository, MovieRepository movieRepository)
    {
        _leaseRepository = leaseRepository;
        _movieRepository = movieRepository;
    }
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Home");
    }
    public IActionResult Create(int movieId)
    {
        if (movieId == 0 || movieId < 0 || movieId.ToString() == null)
        {
            return RedirectToAction("Index", "Movies");
        }
        var movie = _movieRepository.GetById(movieId);
        if (movie == null)
        {
            return NotFound();
        }
        return View(movie);
    }

    public IActionResult MyLeases(int status = -1, int pageNumber = 1, int pageSize = 10)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var leases = _leaseRepository.GetByMemberId(userId, pageNumber, pageSize, status);
        return View(leases);
    }

    public IActionResult Cancel(int id)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var lease = _leaseRepository.GetById(id);
        if (lease == null)
        {
            return NotFound();
        }

        if (lease.Status != 0) // 只有待處理的租借可以取消
        {
            return BadRequest("只有待處理的租借可以取消");
        }

        lease.Status = 2; // 設定狀態為 "已取消"
        _leaseRepository.Update(lease);

        // 更新電影庫存
        var movie = _movieRepository.GetById(lease.MovieId);
        if (movie != null)
        {
            movie.Stock += 1;
            _movieRepository.Update(movie);
        }

        return Json(new { success = true, message = "租借已取消" });
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Lease lease)
    {
        try
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
            {
                return Json(new { success = false, message = "請先登入" });
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            Console.WriteLine($"User ID: {userId}, Movie ID: {lease.MovieId}, Lease Date: {lease.LeaseDate}, Return Date: {lease.ReturnDate}, Payment Method: {lease.payment_method}, Status: {lease.Status}");

            lease.MemberId = userId;
            lease.Status = 0; // 設定狀態為 "待處理"

            var movie = _movieRepository.GetById(lease.MovieId);
            if (movie == null)
            {
                return Json(new { success = false, message = "找不到指定電影" });
            }

            // 驗證租借日期
            if (lease.LeaseDate < DateTime.Now.Date)
            {
                return Json(new { success = false, message = "租借日期不能是過去的日期" });
            }

            // 檢查庫存
            if (movie.Stock <= 0)
            {
                return Json(new { success = false, message = "該電影目前缺貨" });
            }
            _leaseRepository.Add(lease);
            // 更新庫存
            movie.Stock -= 1;
            _movieRepository.Update(movie);
            return Json(new { success = true, message = "租借成功" });

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating lease: {ex.Message}");
            return Json(new { success = false, message = "系統錯誤，請稍後再試" });
        }
    }

    [HttpPut]
    public IActionResult Update(int id, Lease lease)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null && User.FindFirstValue(ClaimTypes.Role) != "Admin")
        {
            return Json(new { success = false, message = "請先登入" });
        }
        var existingLease = _leaseRepository.GetById(id);
        if (existingLease == null)
        {
            return NotFound();
        }

        existingLease.LeaseDate = lease.LeaseDate;
        existingLease.ReturnDate = lease.ReturnDate;
        existingLease.Status = lease.Status;
        _leaseRepository.Update(existingLease);
        return Json(new { success = true, message = "租借資訊已更新" });
    }

    [HttpGet]
    public IActionResult GetStatistics()
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null && User.FindFirstValue(ClaimTypes.Role) != "Admin")
        {
            return Json(new { success = false, message = "請先登入" });
        }

        var stats = _leaseRepository.GetStatistics();
        return Json(new { success = true, data = stats });
    }
    [HttpGet]
    public IActionResult Search(int leaseId = 0, string memberName = "", int status = -1, DateTime? leaseDate = null, int pageNumber = 1, int pageSize = 10)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null && User.FindFirstValue(ClaimTypes.Role) != "Admin")
        {
            return Json(new { success = false, message = "請先登入" });
        }
        if (pageNumber < 1 || pageSize < 1)
        {
            return Json(new { success = false, message = "頁碼和每頁大小必須大於零" });
        }
        var result = _leaseRepository.Search(leaseId, memberName, status, leaseDate, pageNumber, pageSize);
        return Json(new { success = true, data = result });
    }
    [HttpPut]
    public IActionResult UpdateStatus(int id, int status)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null && User.FindFirstValue(ClaimTypes.Role) != "Admin")
        {
            return Json(new { success = false, message = "請先登入" });
        }
        var existingLease = _leaseRepository.GetById(id);
        if (existingLease == null)
        {
            return NotFound();
        }
        if(status == 2 || status == 3)
        {
            var movie = _movieRepository.GetById(existingLease.MovieId);
            if (movie != null)
            {
                movie.Stock += 1;
                _movieRepository.Update(movie);
            }
        }
        existingLease.Status = status;
        _leaseRepository.Update(existingLease);
        return Json(new { success = true, message = "租借狀態已更新" });
    }
    [HttpPut]
    public IActionResult CancelLease(int id)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
        {
            return Json(new { success = false, message = "請先登入" });
        }

        var lease = _leaseRepository.GetById(id);
        if (lease == null)
        {
            return NotFound();
        }

        if (lease.Status != 0) // 只有待處理的租借可以取消
        {
            return BadRequest("只有待處理的租借可以取消");
        }

        lease.Status = 2; // 設定狀態為 "已取消"
        _leaseRepository.Update(lease);

        // 更新電影庫存
        var movie = _movieRepository.GetById(lease.MovieId);
        if (movie != null)
        {
            movie.Stock += 1;
            _movieRepository.Update(movie);
        }
        return Json(new { success = true, message = "租借已取消" });
    }
}
