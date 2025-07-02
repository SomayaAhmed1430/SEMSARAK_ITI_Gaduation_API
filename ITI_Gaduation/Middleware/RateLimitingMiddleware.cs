using System.Collections.Concurrent;
using System.Text.Json;

namespace ITI_Gaduation.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, RateLimitInfo> _clients = new();
        private readonly int _maxRequests = 100; // Max requests per minute
        private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientId(context);
            var rateLimitInfo = _clients.GetOrAdd(clientId, _ => new RateLimitInfo());

            lock (rateLimitInfo)
            {
                var now = DateTime.UtcNow;

                // Reset if time window has passed
                if (now - rateLimitInfo.WindowStart > _timeWindow)
                {
                    rateLimitInfo.RequestCount = 0;
                    rateLimitInfo.WindowStart = now;
                }

                rateLimitInfo.RequestCount++;

                if (rateLimitInfo.RequestCount > _maxRequests)
                {
                    _logger.LogWarning("Rate limit exceeded for client {ClientId}", clientId);
                    context.Response.StatusCode = 429; // Too Many Requests
                    context.Response.ContentType = "application/json";

                    var response = JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "تم تجاوز الحد المسموح من الطلبات. يرجى المحاولة لاحقاً"
                    });

                    //await context.Response.WriteAsync(response);
                    //return;
                }
            }

            await _next(context);
        }

        private string GetClientId(HttpContext context)
        {
            // Try to get user ID from JWT token first
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
                return $"user_{userId}";

            // Fallback to IP address
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private class RateLimitInfo
        {
            public int RequestCount { get; set; }
            public DateTime WindowStart { get; set; } = DateTime.UtcNow;
        }
    }
}

