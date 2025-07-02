namespace ITI_Gaduation.Models
{
    public class UserInfo
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public UserRole Role { get; set; }
        public bool IsVerified { get; set; }
    }
}
