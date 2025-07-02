using ITI_Gaduation.Models;
using ITI_Gaduation.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ITI_Gaduation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterAsync(request);
                return Ok(new { success = true, data = result, message = "تم التسجيل بنجاح" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error");
                return StatusCode(500, new { success = false, message = "حدث خطأ أثناء التسجيل" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.LoginAsync(request);
                return Ok(new { success = true, data = result, message = "تم تسجيل الدخول بنجاح" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                return StatusCode(500, new { success = false, message = "حدث خطأ أثناء تسجيل الدخول" });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);
                return Ok(new { success = true, data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token error");
                return StatusCode(500, new { success = false, message = "حدث خطأ أثناء تحديث التوكن" });
            }
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RevokeTokenAsync(request.RefreshToken);
                if (result)
                {
                    return Ok(new { success = true, message = "تم إلغاء التوكن بنجاح" });
                }
                return BadRequest(new { success = false, message = "فشل في إلغاء التوكن" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Revoke token error");
                return StatusCode(500, new { success = false, message = "حدث خطأ أثناء إلغاء التوكن" });
            }
        }

        [HttpPost("verify/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> VerifyUser(int userId)
        {
            try
            {
                var result = await _authService.VerifyUserAsync(userId);
                if (result)
                {
                    return Ok(new { success = true, message = "تم تأكيد المستخدم بنجاح" });
                }
                return NotFound(new { success = false, message = "المستخدم غير موجود" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User verification error");
                return StatusCode(500, new { success = false, message = "حدث خطأ أثناء تأكيد المستخدم" });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public IActionResult GetProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                var fullName = User.FindFirst(ClaimTypes.Name)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                var isVerified = bool.Parse(User.FindFirst("IsVerified")?.Value ?? "false");

                var userInfo = new UserInfo
                {
                    Id = userId,
                    Email = email,
                    FullName = fullName,
                    Role = Enum.Parse<UserRole>(role),
                    IsVerified = isVerified
                };

                return Ok(new { success = true, data = userInfo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get profile error");
                return StatusCode(500, new { success = false, message = "حدث خطأ أثناء جلب بيانات المستخدم" });
            }
        }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }
}
