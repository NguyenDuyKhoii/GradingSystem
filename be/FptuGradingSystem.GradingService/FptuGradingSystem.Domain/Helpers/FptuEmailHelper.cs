using System;
using System.Linq;

namespace FptuGradingSystem.Domain.Helpers
{
    public static class FptuEmailHelper
    {
        public static string RemoveVietnameseSign(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            string[] vietnameseSigns = new string[]
            {
                "aàảãáạăằẳẵắặâầẩẫấậ", "AÀẢÃÁẠĂẰẲẴẮẶÂẦẨẪẤẬ",
                "dđ", "DĐ",
                "eèẻẽéẹêềểễếệ", "EÈẺẼÉẸÊỀỂỄẾỆ",
                "iìỉĩíị", "IÌỈĨÍỊ",
                "oòỏõóọôồổỗốộơờởỡớợ", "OÒỎÕÓỌÔỒỔỖỐỘƠỜỞỠỚỢ",
                "uùủũúụưừửữứự", "UÙỦŨÚỤƯỪỬỮỨỰ",
                "yỳỷỹýỵ", "YỲỶỸÝỴ"
            };

            for (int i = 0; i < vietnameseSigns.Length; i++)
            {
                for (int j = 1; j < vietnameseSigns[i].Length; j++)
                {
                    text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[i][0]);
                }
            }
            return text;
        }

        public static string GenerateFptuEmail(string studentName, string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
                return "student@fpt.edu.vn";

            var cleanId = studentId.ToLower().Trim();
            if (string.IsNullOrWhiteSpace(studentName))
                return $"{cleanId}@fpt.edu.vn";

            var unaccentedName = RemoveVietnameseSign(studentName);
            var parts = unaccentedName.Trim().Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return $"{cleanId}@fpt.edu.vn";

            var firstName = parts[^1].ToLower();
            var initials = "";
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    initials += parts[i][0].ToString().ToLower();
                }
            }

            return $"{firstName}{initials}{cleanId}@fpt.edu.vn";
        }
    }
}
