using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ThaiBeer.Core.Common;

public static class SlugHelper
{
    public static string TaoSlug(string? tieuDe)
    {
        if (string.IsNullOrWhiteSpace(tieuDe))
            return string.Empty;

        // 1. Chuyển chữ hoa thành chữ thường
        var slug = tieuDe.ToLowerInvariant().Trim();

        // 2. Thay thế ký tự có dấu tiếng Việt
        slug = LoaiBoDauTiengViet(slug);

        // 3. Thay thế khoảng trắng và ký tự đặc biệt thành dấu gạch ngang
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", " ").Trim();
        slug = Regex.Replace(slug, @"\s", "-");

        // 4. Rút gọn các dấu gạch nối liên tiếp
        slug = Regex.Replace(slug, @"-+", "-");

        return slug.Trim('-');
    }

    private static string LoaiBoDauTiengViet(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

        // Thay ký tự đ, Đ
        return result.Replace("đ", "d").Replace("Đ", "d");
    }
}
