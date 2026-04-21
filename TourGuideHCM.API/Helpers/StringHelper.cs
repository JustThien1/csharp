using System.Globalization;
using System.Text;

namespace TourGuideHCM.API.Helpers;

/// <summary>
/// Tiện ích so sánh chuỗi cho việc phát hiện trùng lặp POI.
/// Hỗ trợ: normalize tiếng Việt (bỏ dấu), Levenshtein distance, similarity ratio.
/// </summary>
public static class StringHelper
{
    /// <summary>
    /// Chuẩn hoá chuỗi: lowercase, bỏ dấu tiếng Việt, bỏ khoảng trắng thừa, bỏ ký tự đặc biệt.
    /// Ví dụ: "Nhà THỜ Đức-Bà!" → "nha tho duc ba"
    /// </summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // 1. Bỏ dấu
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        // Xử lý đ/Đ riêng (NormalizationForm không tách được)
        var result = sb.ToString()
            .Replace('đ', 'd')
            .Replace('Đ', 'd')
            .ToLowerInvariant();

        // 2. Giữ lại chữ + số + khoảng trắng, bỏ ký tự đặc biệt
        var clean = new StringBuilder(result.Length);
        foreach (var ch in result)
        {
            if (char.IsLetterOrDigit(ch) || ch == ' ')
                clean.Append(ch);
            else if (char.IsPunctuation(ch) || ch == '-' || ch == '_')
                clean.Append(' ');   // thay thành space để tránh dính chữ
        }

        // 3. Gom khoảng trắng thừa
        return System.Text.RegularExpressions.Regex
            .Replace(clean.ToString(), @"\s+", " ")
            .Trim();
    }

    /// <summary>
    /// Khoảng cách Levenshtein — số thao tác chèn/xoá/thay để biến s1 → s2.
    /// Dùng optimized 2-row DP (O(m*n) time, O(min(m,n)) space).
    /// </summary>
    public static int LevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;

        var n = s1.Length;
        var m = s2.Length;

        // Đảm bảo s2 ngắn hơn → tiết kiệm bộ nhớ
        if (m > n) { (s1, s2) = (s2, s1); (n, m) = (m, n); }

        var prev = new int[m + 1];
        var curr = new int[m + 1];

        for (int j = 0; j <= m; j++) prev[j] = j;

        for (int i = 1; i <= n; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= m; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                curr[j] = Math.Min(
                    Math.Min(curr[j - 1] + 1, prev[j] + 1),
                    prev[j - 1] + cost
                );
            }
            (prev, curr) = (curr, prev);
        }

        return prev[m];
    }

    /// <summary>
    /// Tính độ giống giữa 2 chuỗi theo tỉ lệ 0..1 (1 = giống hoàn toàn).
    /// Tự normalize cả 2 trước khi so sánh.
    /// </summary>
    public static double Similarity(string? s1, string? s2)
    {
        var n1 = Normalize(s1);
        var n2 = Normalize(s2);

        if (string.IsNullOrEmpty(n1) && string.IsNullOrEmpty(n2)) return 1.0;
        if (string.IsNullOrEmpty(n1) || string.IsNullOrEmpty(n2)) return 0.0;

        var maxLen = Math.Max(n1.Length, n2.Length);
        var distance = LevenshteinDistance(n1, n2);
        return 1.0 - (double)distance / maxLen;
    }
}
