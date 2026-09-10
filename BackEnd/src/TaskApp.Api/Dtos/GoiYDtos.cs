namespace TaskApp.Api.Dtos;

/// <summary>
/// Yêu cầu gợi ý người thực hiện.
///
/// <para>
/// Gọi được theo hai cách: đưa <c>TaskId</c> của nhiệm vụ đã lưu, hoặc đưa thẳng
/// <c>Title</c>/<c>Description</c> khi người giao mới đang gõ và chưa bấm lưu —
/// để họ xem gợi ý ngay trên màn tạo nhiệm vụ.
/// </para>
/// </summary>
public sealed class GoiYRequest
{
    public long? TaskId { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }

    /// <summary>Số ứng viên muốn nhận. Mặc định 5.</summary>
    public int? SoLuong { get; set; }
}

/// <summary>Điểm của một đặc trưng, kèm phần đóng góp vào điểm tổng.</summary>
public sealed class ThanhPhanDiem
{
    /// <summary>Điểm thô của đặc trưng, trong khoảng [0, 1].</summary>
    public double Diem { get; set; }

    /// <summary>Trọng số của đặc trưng.</summary>
    public double TrongSo { get; set; }

    /// <summary>Phần đóng góp vào điểm tổng = Diem × TrongSo.</summary>
    public double DongGop { get; set; }
}

/// <summary>Bốn thành phần cấu thành điểm phù hợp.</summary>
public sealed class ChiTietDiem
{
    /// <summary>Độ khớp giữa nội dung nhiệm vụ và kỹ năng đã khai (TF-IDF + cosine).</summary>
    public ThanhPhanDiem KyNang { get; set; } = new();

    /// <summary>Kinh nghiệm — tổng số nhiệm vụ đã hoàn thành, thang log.</summary>
    public ThanhPhanDiem KinhNghiem { get; set; } = new();

    /// <summary>Tỷ lệ hoàn thành đúng hạn, có làm mượt Laplace.</summary>
    public ThanhPhanDiem DungHan { get; set; } = new();

    /// <summary>Mức độ rảnh — càng ít việc đang gánh thì điểm càng cao.</summary>
    public ThanhPhanDiem KhoiLuong { get; set; } = new();
}

/// <summary>Số liệu thô đứng sau điểm số, để người giao tự kiểm chứng.</summary>
public sealed class SoLieuUngVien
{
    public int SoNhiemVuHoanThanh { get; set; }
    public int SoNhiemVuDungHan { get; set; }
    public int SoNhiemVuDangLam { get; set; }

    /// <summary>Tổng trọng số ưu tiên của các việc đang mở.</summary>
    public double TaiHienTai { get; set; }

    /// <summary>Các kỹ năng khớp với nội dung nhiệm vụ, kèm mức thành thạo.</summary>
    public IReadOnlyList<string> KyNangKhop { get; set; } = Array.Empty<string>();
}

/// <summary>Một ứng viên được gợi ý.</summary>
public sealed class UngVienDto
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;

    /// <summary>Điểm phù hợp tổng, trong khoảng [0, 1]. Làm tròn 4 chữ số.</summary>
    public double Diem { get; set; }

    /// <summary>Thứ hạng trong danh sách, bắt đầu từ 1.</summary>
    public int ThuHang { get; set; }

    public ChiTietDiem ChiTietDiem { get; set; } = new();
    public SoLieuUngVien SoLieu { get; set; } = new();

    /// <summary>Lý do gợi ý bằng tiếng Việt, tối đa 4 dòng.</summary>
    public IReadOnlyList<string> LyDo { get; set; } = Array.Empty<string>();
}

/// <summary>Kết quả gợi ý.</summary>
public sealed class GoiYResponse
{
    /// <summary>Phiên bản bộ trọng số đã dùng, để đối chiếu khi hiệu chỉnh mô hình.</summary>
    public string PhienBanTrongSo { get; set; } = string.Empty;

    /// <summary>Nội dung nhiệm vụ đã dùng để chấm điểm.</summary>
    public string NoiDungDaDung { get; set; } = string.Empty;

    /// <summary>Số ứng viên đã xét sau khi lọc.</summary>
    public int SoUngVienDaXet { get; set; }

    /// <summary>Cảnh báo về chất lượng gợi ý, ví dụ khi không ai khai kỹ năng khớp.</summary>
    public IReadOnlyList<string> CanhBao { get; set; } = Array.Empty<string>();

    public IReadOnlyList<UngVienDto> UngVien { get; set; } = Array.Empty<UngVienDto>();

    /// <summary>Thời gian tính toán, mili giây.</summary>
    public long ThoiGianMs { get; set; }
}
