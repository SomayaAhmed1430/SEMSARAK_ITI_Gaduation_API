namespace ITI_Gaduation.Models
{
    public class NationalIdVerificationResponse
    {
        public bool IsValid { get; set; }
        public bool NameMatches { get; set; }
        public string Message { get; set; }
        public VerificationData Data { get; set; }
    }
}
