using System.ComponentModel.DataAnnotations;

namespace ITI_Gaduation.Models
{
    

    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber {  get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string NationalId { get; set; }
        public UserRole Role { get; set; }
        public bool IsVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }
}
