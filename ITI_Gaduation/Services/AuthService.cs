using ITI_Gaduation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using ITI_Gaduation.Data;
using Microsoft.EntityFrameworkCore;

namespace ITI_Gaduation.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly INationalIdVerificationService _verificationService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            INationalIdVerificationService verificationService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _verificationService = verificationService;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // التحقق من وجود المستخدم مسبقاً
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    throw new InvalidOperationException("البريد الإلكتروني مستخدم مسبقاً");
                }

                if (await _context.Users.AnyAsync(u => u.NationalId == request.NationalId))
                {
                    throw new InvalidOperationException("رقم البطاقة مستخدم مسبقاً");
                }

                // التحقق من صحة البطاقة الشخصية (فقط للمالكين والمستأجرين)
                if (request.Role != UserRole.Admin)
                {
                    var verificationResult = await _verificationService.VerifyNationalIdAsync(
                        new NationalIdVerificationRequest
                        {
                            NationalId = request.NationalId,
                            FullName = request.FullName
                        });

                    if (!verificationResult.IsValid)
                    {
                        throw new InvalidOperationException($"البطاقة الشخصية غير صحيحة: {verificationResult.Message}");
                    }

                    if (!verificationResult.NameMatches)
                    {
                        throw new InvalidOperationException("الاسم المدخل لا يطابق الاسم في البطاقة الشخصية");
                    }
                }

                // إنشاء المستخدم
                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    FullName = request.FullName,
                    NationalId = request.NationalId,
                    Role = request.Role,
                    IsVerified = request.Role == UserRole.Admin, // الآدمن يكون مؤكد تلقائياً
                    CreatedAt = DateTime.UtcNow,
                    VerifiedAt = request.Role == UserRole.Admin ? DateTime.UtcNow : null
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // إنشاء التوكن
                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                // حفظ الـ Refresh Token
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    IsActive = true
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                return new AuthResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FullName = user.FullName,
                        Role = user.Role,
                        IsVerified = user.IsVerified
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                throw;
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    throw new UnauthorizedAccessException("البريد الإلكتروني أو كلمة المرور غير صحيحة");
                }

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                // إلغاء الـ Refresh Tokens القديمة
                var oldTokens = _context.RefreshTokens.Where(rt => rt.UserId == user.Id && rt.IsActive);
                foreach (var oldToken in oldTokens)
                {
                    oldToken.IsActive = false;
                }

                // إضافة الـ Refresh Token الجديد
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    IsActive = true
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                return new AuthResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FullName = user.FullName,
                        Role = user.Role,
                        IsVerified = user.IsVerified
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                throw;
            }
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var tokenEntity = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.IsActive);

            if (tokenEntity == null || tokenEntity.ExpiryDate <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Refresh token غير صالح أو منتهي الصلاحية");
            }

            // إنشاء توكن جديد
            var newToken = GenerateJwtToken(tokenEntity.User);
            var newRefreshToken = GenerateRefreshToken();

            // إلغاء الـ Refresh Token القديم
            tokenEntity.IsActive = false;

            // إضافة الـ Refresh Token الجديد
            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = tokenEntity.UserId,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsActive = true
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Token = newToken,
                RefreshToken = newRefreshToken,
                User = new UserInfo
                {
                    Id = tokenEntity.User.Id,
                    Email = tokenEntity.User.Email,
                    FullName = tokenEntity.User.FullName,
                    Role = tokenEntity.User.Role,
                    IsVerified = tokenEntity.User.IsVerified
                }
            };
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var tokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.IsActive);

            if (tokenEntity == null)
                return false;

            tokenEntity.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> VerifyUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.IsVerified = true;
            user.VerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("IsVerified", user.IsVerified.ToString()),
                    new Claim("NationalId", user.NationalId)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
