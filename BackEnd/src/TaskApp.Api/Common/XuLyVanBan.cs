using System.Globalization;
using System.Text;

namespace TaskApp.Api.Common;

/// <summary>
/// Tách từ và chuẩn hoá văn bản tiếng Việt, phục vụ so khớp nội dung nhiệm vụ với kỹ năng.
///
/// <para>
/// <b>Bỏ dấu khi so khớp.</b> Người dùng gõ "Báo cáo", "báo cáo", "bao cao" đều phải khớp
/// với kỹ năng "Báo cáo". Nếu so nguyên dấu thì chỉ cần thiếu một dấu là trượt hoàn toàn.
/// Dấu chỉ bỏ ở khâu so khớp; dữ liệu gốc trong CSDL vẫn giữ nguyên tiếng Việt có dấu.
/// </para>
/// <para>
/// <b>Tách theo từ đơn, không tách từ ghép.</b> Tiếng Việt có từ ghép ("cơ sở dữ liệu" là
/// bốn tiếng nhưng một từ). Tách đúng cần từ điển hoặc mô hình, quá nặng cho phạm vi đề tài.
/// Tách theo tiếng vẫn hoạt động tốt vì kỹ năng phần lớn là thuật ngữ kỹ thuật ngắn
/// ("Oracle", "Angular") hoặc cụm hai ba tiếng đủ đặc trưng.
/// </para>
/// </summary>
public static class XuLyVanBan
{
    /// <summary>
    /// Từ dừng: xuất hiện ở hầu hết văn bản nên không giúp phân biệt nhiệm vụ này với
    /// nhiệm vụ khác. Giữ lại chỉ làm loãng vector và kéo mọi ứng viên về gần nhau.
    /// </summary>
    private static readonly HashSet<string> TuDung = new(StringComparer.Ordinal)
    {
        // tiếng Việt (đã bỏ dấu)
        "va", "hoac", "cua", "cho", "voi", "tu", "den", "trong", "ngoai", "tren", "duoi",
        "la", "co", "khong", "duoc", "bi", "se", "da", "dang", "cac", "nhung", "mot", "moi",
        "nay", "do", "kia", "ay", "khi", "neu", "thi", "ma", "nen", "vi", "boi", "de",
        "theo", "ve", "ra", "vao", "len", "xuong", "qua", "lai", "cung", "cu", "van",
        "rat", "hon", "nhat", "sau", "truoc", "giua", "day", "dau", "gi", "nao", "sao",
        "toi", "ban", "ho", "chung", "minh", "ai", "nguoi", "viec", "phai", "can", "hay",
        // tiếng Anh
        "the", "a", "an", "and", "or", "of", "to", "in", "on", "at", "for", "with",
        "by", "from", "is", "are", "was", "were", "be", "been", "this", "that", "it",
        "as", "but", "not", "have", "has", "had", "do", "does", "did", "will", "would"
    };

    /// <summary>
    /// Bỏ dấu tiếng Việt và chuyển về chữ thường.
    /// Ví dụ: "Kiểm thử Phần mềm" -&gt; "kiem thu phan mem".
    /// </summary>
    public static string BoDau(string? vanBan)
    {
        if (string.IsNullOrWhiteSpace(vanBan)) return string.Empty;

        // Chuẩn hoá về dạng D: tách ký tự gốc khỏi dấu thanh, rồi loại các dấu đó đi.
        var tachDau = vanBan.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(tachDau.Length);

        foreach (var c in tachDau)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        // Đ và đ không phải chữ D có dấu phụ nên FormD không tách được, phải thay tay.
        return sb.ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('Đ', 'D')
            .Replace('đ', 'd')
            .ToLowerInvariant();
    }

    /// <summary>
    /// Tách một chuỗi thành danh sách từ đã chuẩn hoá: bỏ dấu, tách theo ký tự không phải
    /// chữ/số, loại từ dừng và các từ chỉ có một ký tự.
    /// </summary>
    public static List<string> TachTu(string? vanBan)
    {
        var ketQua = new List<string>();
        var chuanHoa = BoDau(vanBan);
        if (chuanHoa.Length == 0) return ketQua;

        var sb = new StringBuilder();
        foreach (var c in chuanHoa)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else if (sb.Length > 0)
            {
                ThemNeuHopLe(ketQua, sb.ToString());
                sb.Clear();
            }
        }
        if (sb.Length > 0) ThemNeuHopLe(ketQua, sb.ToString());

        return ketQua;
    }

    private static void ThemNeuHopLe(List<string> ds, string tu)
    {
        // Từ một ký tự gần như không mang thông tin phân biệt.
        if (tu.Length <= 1) return;
        if (TuDung.Contains(tu)) return;
        ds.Add(tu);
    }
}
