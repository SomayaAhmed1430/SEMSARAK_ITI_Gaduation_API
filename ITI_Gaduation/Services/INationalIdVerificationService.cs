using ITI_Gaduation.Models;

namespace ITI_Gaduation.Services
{
    public interface INationalIdVerificationService
    {
        Task<NationalIdVerificationResponse> VerifyNationalIdAsync(NationalIdVerificationRequest request);
    }
}
