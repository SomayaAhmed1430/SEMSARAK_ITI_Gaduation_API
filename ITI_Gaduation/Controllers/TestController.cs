using ITI_Gaduation.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITI_Gaduation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("public")]
        public IActionResult PublicTest()
        {
            return Ok(ApiResponse<string>.SuccessResult("Public endpoint working!"));
        }

        [HttpGet("protected")]
        [Authorize]
        public IActionResult ProtectedTest()
        {
            var userInfo = new
            {
                Id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
                Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            };

            return Ok(ApiResponse<object>.SuccessResult(userInfo, "Protected endpoint accessed successfully!"));
        }

        [HttpGet("owner-only")]
        [Authorize(Policy = "PropertyOwnerOnly")]
        public IActionResult OwnerOnlyTest()
        {
            return Ok(ApiResponse<string>.SuccessResult("Property owner endpoint working!"));
        }

        [HttpGet("tenant-only")]
        [Authorize(Policy = "TenantOnly")]
        public IActionResult TenantOnlyTest()
        {
            return Ok(ApiResponse<string>.SuccessResult("Tenant endpoint working!"));
        }

        [HttpGet("admin-only")]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult AdminOnlyTest()
        {
            return Ok(ApiResponse<string>.SuccessResult("Admin endpoint working!"));
        }

        [HttpGet("verified-only")]
        [Authorize(Policy = "VerifiedUsersOnly")]
        public IActionResult VerifiedOnlyTest()
        {
            return Ok(ApiResponse<string>.SuccessResult("Verified users endpoint working!"));
        }
    }
}
