namespace TaskApp.Api.Common;

/// <summary>
/// TF-IDF và độ tương đồng cosine, cài thuần C# không dùng thư viện ngoài.
///
/// <para>
/// <b>TF</b> (term frequency): từ xuất hiện càng nhiều trong một văn bản thì càng quan trọng
/// với văn bản đó.
/// <b>IDF</b> (inverse document frequency): từ xuất hiện ở càng nhiều văn bản thì càng ít
/// giá trị phân biệt. "Oracle" chỉ vài người có nên rất đáng giá; "quản lý" ai cũng có nên
/// gần như vô dụng.
/// </para>
/// <para>
/// Nhân hai thành phần này lại thì một từ chỉ được điểm cao khi vừa nổi bật trong hồ sơ
/// của một người, vừa hiếm trên toàn tập ứng viên. Đó chính là thứ ta cần khi tìm người
/// hợp nhất với một nhiệm vụ.
/// </para>
/// </summary>
public static class TfIdf
{
    /// <summary>Một văn bản đã được vector hoá.</summary>
    public sealed class VanBanVector
    {
        /// <summary>Trọng số TF-IDF theo từng từ.</summary>
        public Dictionary<string, double> TrongSo { get; init; } = new();

        /// <summary>Độ dài Euclid của vector, tính sẵn để khỏi lặp lại khi so cosine.</summary>
        public double DoDai { get; init; }
    }

    /// <summary>
    /// Dựng chỉ mục IDF từ tập văn bản (mỗi văn bản là danh sách từ đã tách).
    /// </summary>
    /// <remarks>
    /// Dùng công thức làm mượt <c>ln((1 + N) / (1 + df)) + 1</c>:
    /// cộng 1 vào tử và mẫu để không chia cho 0 khi một từ không có trong văn bản nào,
    /// cộng 1 ở ngoài để từ xuất hiện ở mọi văn bản vẫn còn trọng số dương thay vì bằng 0.
    /// </remarks>
    public static Dictionary<string, double> DungIdf(IReadOnlyCollection<List<string>> tapVanBan)
    {
        var soVanBanChua = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var vanBan in tapVanBan)
        {
            // Đếm theo VĂN BẢN chứ không theo lần xuất hiện, nên phải lọc trùng trước.
            foreach (var tu in vanBan.Distinct(StringComparer.Ordinal))
            {
                soVanBanChua[tu] = soVanBanChua.GetValueOrDefault(tu) + 1;
            }
        }

        var tongSo = tapVanBan.Count;
        var idf = new Dictionary<string, double>(StringComparer.Ordinal);

        foreach (var (tu, df) in soVanBanChua)
        {
            idf[tu] = Math.Log((1.0 + tongSo) / (1.0 + df)) + 1.0;
        }

        return idf;
    }

    /// <summary>Vector hoá một văn bản theo chỉ mục IDF đã dựng.</summary>
    public static VanBanVector VectorHoa(List<string> cacTu, Dictionary<string, double> idf)
    {
        if (cacTu.Count == 0)
        {
            return new VanBanVector();
        }

        // TF chuẩn hoá theo độ dài văn bản, để văn bản dài không tự nhiên được điểm cao hơn.
        var tanSuat = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var tu in cacTu)
        {
            tanSuat[tu] = tanSuat.GetValueOrDefault(tu) + 1.0;
        }

        var trongSo = new Dictionary<string, double>(StringComparer.Ordinal);
        double tongBinhPhuong = 0;

        foreach (var (tu, soLan) in tanSuat)
        {
            var tf = soLan / cacTu.Count;

            // Từ chưa từng gặp khi dựng chỉ mục: cho IDF cao nhất có thể, vì nó hiếm.
            var giaTriIdf = idf.TryGetValue(tu, out var v) ? v : 1.0;

            var w = tf * giaTriIdf;
            trongSo[tu] = w;
            tongBinhPhuong += w * w;
        }

        return new VanBanVector
        {
            TrongSo = trongSo,
            DoDai = Math.Sqrt(tongBinhPhuong)
        };
    }

    /// <summary>
    /// Độ tương đồng cosine giữa hai vector, kết quả trong khoảng [0, 1].
    /// Bằng 1 khi hai văn bản dùng đúng cùng bộ từ với cùng tỉ lệ; bằng 0 khi không chung từ nào.
    /// </summary>
    public static double Cosine(VanBanVector a, VanBanVector b)
    {
        if (a.DoDai == 0 || b.DoDai == 0) return 0;

        // Duyệt vector ngắn hơn: tích vô hướng chỉ cộng ở những từ có mặt ở CẢ HAI bên,
        // nên duyệt bên ít từ hơn cho cùng kết quả mà nhanh hơn.
        var (it, nhieu) = a.TrongSo.Count <= b.TrongSo.Count ? (a, b) : (b, a);

        double tichVoHuong = 0;
        foreach (var (tu, w) in it.TrongSo)
        {
            if (nhieu.TrongSo.TryGetValue(tu, out var w2))
            {
                tichVoHuong += w * w2;
            }
        }

        var ketQua = tichVoHuong / (a.DoDai * b.DoDai);

        // Chặn sai số dấu phẩy động để kết quả không vượt khỏi [0, 1].
        return Math.Clamp(ketQua, 0.0, 1.0);
    }
}
