using Microsoft.AspNetCore.Mvc;
using Project01_movie_lease_system.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Project01_movie_lease_system.Repositories;
using DocumentFormat.OpenXml.Drawing;


namespace Project01_movie_lease_system.Controllers
{
    public class AccountController : Controller
    {
        private readonly MemberRepository _memberRepository;
        public AccountController(MemberRepository memberRepository)
        {
            _memberRepository = memberRepository;
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 登出後導回首頁或登入頁
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public IActionResult Profile()
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var member = _memberRepository.GetById(userId);
            if (member == null)
            {
                return RedirectToAction("Login", "Account");
            }
            // 返回會員資料到Profile視圖
            return View(member);
        }
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register(Member member)
        {
            Console.WriteLine("收到註冊請求");
            if (ModelState.IsValid)
            {
                Console.WriteLine("ModelState 通過驗證");
                if (_memberRepository.GetByEmail(member.Email).Any())
                {
                    TempData["ErrorMessage"] = "電子郵件已被註冊";
                    return View(member);
                }
                member.PasswordHash = BCrypt.Net.BCrypt.HashPassword(member.PasswordHash);
                member.CreatedAt = DateTime.Now;
                _memberRepository.Add(member);
                return RedirectToAction("Login", "Account");
            }
            else
            {
                Console.WriteLine("ModelState 驗證失敗");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    TempData["ErrorMessage"] = $"錯誤: {error.ErrorMessage}";
                }
            }
            return View(member);
        }
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _memberRepository.GetByEmail(email)
                .FirstOrDefault(m => m.Email == email);
            if (user == null)
            {
                ViewBag.Error = "帳號不存在";
                return View();
            }
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Claim (key: value)
                    new Claim(ClaimTypes.Role, "Member") // 角色
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme); // 使用 Cookie 方式儲存

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity)); //封裝cookie 

                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = "帳號或密碼錯誤";
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]  //確保請求來自你的畫面
        public IActionResult UpdateProfile([FromBody] Member update_member)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var member = _memberRepository.GetById(userId);
            if (member == null)
            {
                return NotFound(new { message = "會員不存在" });
            }
            // 更新會員資料
            member.Name = update_member.Name;
            member.Email = update_member.Email;
            member.PhoneNumber = update_member.PhoneNumber;
            member.UpdatedAt = DateTime.Now; // 更新時間
            _memberRepository.Update(member);
            // 返回更新成功的結果
            return Ok(new { message = "會員資料更新成功" });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword([FromBody] UpdateMemberPassword updateMemberPassword)
        {
            Console.WriteLine($"收到更改密碼請求:{updateMemberPassword.OldPassword}, {updateMemberPassword.NewPassword}, {updateMemberPassword.ConfirmNewPassword}");
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var member = _memberRepository.GetById(userId);
            if (member == null)
            {
                return NotFound(new { message = "會員不存在" });
            }
            if( !BCrypt.Net.BCrypt.Verify(updateMemberPassword.OldPassword, member.PasswordHash))
            {
                return BadRequest(new { message = "舊密碼不正確" });
            }
            if (updateMemberPassword.NewPassword != updateMemberPassword.ConfirmNewPassword)
            {
                return BadRequest(new { message = "新密碼和確認新密碼不匹配" });
            }
            // 更新密碼
            member.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateMemberPassword.NewPassword);
            member.UpdatedAt = DateTime.Now; // 更新時間
            _memberRepository.Update(member);
            // 驗證舊密碼
            return Ok(new { message = "密碼更新成功" });
        }
    }
}