using System.ComponentModel.DataAnnotations;

namespace ITI_Gaduation.Models
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [StringLength(14, MinimumLength = 14)]
        public string NationalId { get; set; }

        [Required]
        public UserRole Role { get; set; }
    }
}
