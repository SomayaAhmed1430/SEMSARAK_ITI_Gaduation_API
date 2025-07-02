using FluentValidation;
using ITI_Gaduation.Models; 

namespace ITI_Gaduation.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
                .EmailAddress().WithMessage("تنسيق البريد الإلكتروني غير صحيح");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("كلمة المرور مطلوبة")
                .MinimumLength(8).WithMessage("كلمة المرور يجب أن تكون 8 أحرف على الأقل")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
                .WithMessage("كلمة المرور يجب أن تحتوي على حرف كبير وحرف صغير ورقم ورمز خاص");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("الاسم الكامل مطلوب")
                .MinimumLength(2).WithMessage("الاسم يجب أن يكون حرفين على الأقل")
                .MaximumLength(100).WithMessage("الاسم يجب أن يكون أقل من 100 حرف")
                .Matches(@"^[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\uFB50-\uFDFF\uFE70-\uFEFF\s]+$")
                .WithMessage("الاسم يجب أن يحتوي على أحرف عربية فقط");

            RuleFor(x => x.NationalId)
                .NotEmpty().WithMessage("رقم البطاقة الشخصية مطلوب")
                .Length(14).WithMessage("رقم البطاقة يجب أن يكون 14 رقم")
                .Matches(@"^\d{14}$").WithMessage("رقم البطاقة يجب أن يحتوي على أرقام فقط")
                .Must(BeValidNationalId).WithMessage("رقم البطاقة الشخصية غير صحيح");

            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("نوع المستخدم غير صحيح");
        }

        private bool BeValidNationalId(string nationalId)
        {
            if (string.IsNullOrEmpty(nationalId) || nationalId.Length != 14)
                return false;

            // التحقق من الرقم الأول (القرن)
            var centuryDigit = int.Parse(nationalId[0].ToString());
            if (centuryDigit != 2 && centuryDigit != 3)
                return false;

            // التحقق من تاريخ الميلاد
            try
            {
                var year = int.Parse(nationalId.Substring(1, 2));
                var month = int.Parse(nationalId.Substring(3, 2));
                var day = int.Parse(nationalId.Substring(5, 2));

                var fullYear = centuryDigit == 2 ? 1900 + year : 2000 + year;
                var birthDate = new DateTime(fullYear, month, day);

                var age = DateTime.Now.Year - birthDate.Year;
                if (DateTime.Now.DayOfYear < birthDate.DayOfYear)
                    age--;

                return age >= 18 && age <= 100;
            }
            catch
            {
                return false;
            }
        }
    }
}
