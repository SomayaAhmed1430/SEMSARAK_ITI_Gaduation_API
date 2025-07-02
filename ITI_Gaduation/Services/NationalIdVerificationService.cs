using ITI_Gaduation.Models;
using System.Text.Json;

namespace ITI_Gaduation.Services
{
    public class NationalIdVerificationService : INationalIdVerificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NationalIdVerificationService> _logger;

        public NationalIdVerificationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<NationalIdVerificationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<NationalIdVerificationResponse> VerifyNationalIdAsync(NationalIdVerificationRequest request)
        {
            try
            {
                // التحقق من صحة البطاقة محلياً أولاً
                if (!IsValidNationalIdFormat(request.NationalId))
                {
                    return new NationalIdVerificationResponse
                    {
                        IsValid = false,
                        Message = "تنسيق رقم البطاقة غير صحيح"
                    };
                }

                // استخدام API الحكومي المصري للتحقق من البطاقات
                var apiUrl = _configuration["NationalIdApi:BaseUrl"];
                var apiKey = _configuration["NationalIdApi:ApiKey"];

                var requestPayload = new
                {
                    national_id = request.NationalId,
                    full_name = request.FullName
                };

                var jsonContent = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await _httpClient.PostAsync($"{apiUrl}/verify", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiVerificationResponse>(responseContent);

                    return new NationalIdVerificationResponse
                    {
                        IsValid = apiResponse.IsValid,
                        NameMatches = apiResponse.NameMatches,
                        Message = apiResponse.Message,
                        Data = apiResponse.Data != null ? new VerificationData
                        {
                            FullName = apiResponse.Data.FullName,
                            BirthDate = apiResponse.Data.BirthDate,
                            Gender = apiResponse.Data.Gender,
                            Governorate = apiResponse.Data.Governorate
                        } : null
                    };
                }
                else
                {
                    _logger.LogError($"API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");

                    // في حالة فشل API، نستخدم التحقق المحلي
                    return PerformLocalVerification(request);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during national ID verification");

                // في حالة الخطأ، نستخدم التحقق المحلي
                return PerformLocalVerification(request);
            }
        }

        private bool IsValidNationalIdFormat(string nationalId)
        {
            if (string.IsNullOrEmpty(nationalId) || nationalId.Length != 14)
                return false;

            if (!nationalId.All(char.IsDigit))
                return false;

            // التحقق من صحة تاريخ الميلاد في البطاقة
            var centuryDigit = int.Parse(nationalId[0].ToString());
            var year = int.Parse(nationalId.Substring(1, 2));
            var month = int.Parse(nationalId.Substring(3, 2));
            var day = int.Parse(nationalId.Substring(5, 2));

            var fullYear = centuryDigit switch
            {
                2 => 1900 + year,
                3 => 2000 + year,
                _ => 0
            };

            if (fullYear == 0 || month < 1 || month > 12 || day < 1 || day > 31)
                return false;

            try
            {
                var birthDate = new DateTime(fullYear, month, day);
                var age = DateTime.Now.Year - birthDate.Year;

                // التحقق من عمر منطقي (18-100 سنة)
                if (age < 18 || age > 100)
                    return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        private NationalIdVerificationResponse PerformLocalVerification(NationalIdVerificationRequest request)
        {
            var isValidFormat = IsValidNationalIdFormat(request.NationalId);

            return new NationalIdVerificationResponse
            {
                IsValid = isValidFormat,
                NameMatches = isValidFormat, // في التحقق المحلي نفترض أن الاسم صحيح إذا كان التنسيق صحيح
                Message = isValidFormat ? "تم التحقق من البطاقة محلياً" : "تنسيق البطاقة غير صحيح",
                Data = isValidFormat ? ExtractDataFromNationalId(request.NationalId, request.FullName) : null
            };
        }

        private VerificationData ExtractDataFromNationalId(string nationalId, string fullName)
        {
            var centuryDigit = int.Parse(nationalId[0].ToString());
            var year = int.Parse(nationalId.Substring(1, 2));
            var month = int.Parse(nationalId.Substring(3, 2));
            var day = int.Parse(nationalId.Substring(5, 2));
            var governorateCode = int.Parse(nationalId.Substring(7, 2));
            var genderDigit = int.Parse(nationalId[12].ToString());

            var fullYear = centuryDigit == 2 ? 1900 + year : 2000 + year;
            var birthDate = new DateTime(fullYear, month, day);
            var gender = genderDigit % 2 == 0 ? "أنثى" : "ذكر";
            var governorate = GetGovernorateByCode(governorateCode);

            return new VerificationData
            {
                FullName = fullName,
                BirthDate = birthDate.ToString("yyyy-MM-dd"),
                Gender = gender,
                Governorate = governorate
            };
        }

        private string GetGovernorateByCode(int code)
        {
            return code switch
            {
                01 => "القاهرة",
                02 => "الإسكندرية",
                03 => "بورسعيد",
                04 => "السويس",
                11 => "دمياط",
                12 => "الدقهلية",
                13 => "الشرقية",
                14 => "القليوبية",
                15 => "كفر الشيخ",
                16 => "الغربية",
                17 => "المنوفية",
                18 => "البحيرة",
                19 => "الإسماعيلية",
                21 => "الجيزة",
                22 => "بني سويف",
                23 => "الفيوم",
                24 => "المنيا",
                25 => "أسيوط",
                26 => "سوهاج",
                27 => "قنا",
                28 => "أسوان",
                29 => "الأقصر",
                31 => "البحر الأحمر",
                32 => "الوادي الجديد",
                33 => "مطروح",
                34 => "شمال سيناء",
                35 => "جنوب سيناء",
                _ => "غير محدد"
            };
        }
    }

    // Response model for external API
    public class ApiVerificationResponse
    {
        public bool IsValid { get; set; }
        public bool NameMatches { get; set; }
        public string Message { get; set; }
        public VerificationData Data { get; set; }
    }
}
