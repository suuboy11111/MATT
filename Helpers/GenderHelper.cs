using MaiAmTinhThuong.Models.Enums;

namespace MaiAmTinhThuong.Helpers
{
    public static class GenderHelper
    {
        public static string GetDisplayName(Gender gender)
        {
            return gender switch
            {
                Gender.Nam => "Nam",
                Gender.Nu => "Nữ",
                Gender.Khac => "Khác",
                _ => "Không xác định"
            };
        }

        public static Gender? ParseFromString(string genderString)
        {
            if (string.IsNullOrWhiteSpace(genderString))
                return null;

            return genderString.ToLower() switch
            {
                "nam" or "1" => Gender.Nam,
                "nu" or "nữ" or "2" => Gender.Nu,
                "khac" or "khác" or "3" => Gender.Khac,
                _ => null
            };
        }

        public static string ToString(Gender? gender)
        {
            if (gender == null)
                return string.Empty;

            return gender.Value switch
            {
                Gender.Nam => "Nam",
                Gender.Nu => "Nu",
                Gender.Khac => "Khac",
                _ => string.Empty
            };
        }
    }
}



