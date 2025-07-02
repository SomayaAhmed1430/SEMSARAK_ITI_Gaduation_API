namespace ITI_Gaduation.Models
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public UserInfo User { get; set; }
    }
}
