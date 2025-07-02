namespace ITI_Gaduation.Helpers
{
    public static class NationalIdHelper
    {
        public static bool IsValidFormat(string nationalId)
        {
            if (string.IsNullOrEmpty(nationalId) || nationalId.Length != 14)
                return false;

            if (!nationalId.All(char.IsDigit))
                return false;

            var centuryDigit = int.Parse(nationalId[0].ToString());
            if (centuryDigit != 2 && centuryDigit != 3)
                return false;

            return IsValidBirthDate(nationalId);
        }

        public static bool IsValidBirthDate(string nationalId)
        {
            try
            {
                var centuryDigit = int.Parse(nationalId[0].ToString());
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

        public static DateTime? ExtractBirthDate(string nationalId)
        {
            if (!IsValidFormat(nationalId))
                return null;

            try
            {
                var centuryDigit = int.Parse(nationalId[0].ToString());
                var year = int.Parse(nationalId.Substring(1, 2));
                var month = int.Parse(nationalId.Substring(3, 2));
                var day = int.Parse(nationalId.Substring(5, 2));

                var fullYear = centuryDigit == 2 ? 1900 + year : 2000 + year;
                return new DateTime(fullYear, month, day);
            }
            catch
            {
                return null;
            }
        }

        public static string GetGender(string nationalId)
        {
            if (!IsValidFormat(nationalId))
                return "غير محدد";

            var genderDigit = int.Parse(nationalId[12].ToString());
            return genderDigit % 2 == 0 ? "أنثى" : "ذكر";
        }

        public static string GetGovernorate(string nationalId)
        {
            if (!IsValidFormat(nationalId))
                return "غير محدد";

            var governorateCode = int.Parse(nationalId.Substring(7, 2));
            return governorateCode switch
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
}
