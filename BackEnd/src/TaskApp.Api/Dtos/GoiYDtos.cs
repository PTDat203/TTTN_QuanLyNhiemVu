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

/// <summary>Điểm của một thành phần, kèm phần đóng góp vào điểm tổng.</summary>
public sealed class ThanhPhanDiem
{
    /// <summary>Điểm của thành phần, trong khoảng [0, 1].</summary>
    public double Diem { get; set; }

    /// <summary>Trọng số của thành phần.</summary>
    public double TrongSo { get; set; }

    /// <summary>Phần đóng góp vào điểm tổng = Diem × TrongSo.</summary>
    public double DongGop { get; set; }
}

/// <summary>Bảy thành phần cấu thành điểm phù hợp.</summary>
public sealed class ChiTietDiem
{
    /// <summary>Độ khớp ngữ nghĩa giữa nội dung nhiệm vụ và hồ sơ người.</summary>
    public ThanhPhanDiem NguNghia { get; set; } = new();

    /// <summary>Có đủ mức ở những kỹ năng nhiệm vụ đòi hỏi hay không.</summary>
    public ThanhPhanDiem MucKyNang { get; set; } = new();

    /// <summary>Điểm đánh giá chất lượng các việc đã hoàn thành.</summary>
    public ThanhPhanDiem HieuSuat { get; set; } = new();

    /// <summary>Đã làm những việc giống việc này chưa, và làm tốt tới đâu.</summary>
    public ThanhPhanDiem ViecTuongTu { get; set; } = new();

    /// <summary>Tỷ lệ hoàn thành đúng hạn.</summary>
    public ThanhPhanDiem DungHan { get; set; } = new();

    /// <summary>Mức rảnh — càng ít việc đang gánh càng cao.</summary>
    public ThanhPhanDiem KhoiLuong { get; set; } = new();

    /// <summary>Số năm đã làm ở công ty, thang log.</summary>
    public ThanhPhanDiem ThamNien { get; set; } = new();
}

/// <summary>Một việc đã làm, gần với nhiệm vụ đang cần giao.</summary>
public sealed class ViecTuongTuDto
{
    public long TaskId { get; set; }
    public string TieuDe { get; set; } = string.Empty;

    /// <summary>Độ gần với nhiệm vụ đang cần giao, 0..1.</summary>
    public double DoGan { get; set; }

    /// <summary>Điểm chất lượng người duyệt đã chấm, 1..5.</summary>
    public int? ChatLuong { get; set; }
}

/// <summary>Số liệu thô đứng sau điểm số, để người giao tự kiểm chứng.</summary>
public sealed class SoLieuUngVien
{
    public int SoNhiemVuHoanThanh { get; set; }
    public int SoNhiemVuDungHan { get; set; }
    public int SoNhiemVuDangLam { get; set; }

    /// <summary>Tổng trọng số ưu tiên của các việc đang mở.</summary>
    public double TaiHienTai { get; set; }

    /// <summary>Số năm đã làm ở công ty.</summary>
    public double SoNamLamViec { get; set; }

    /// <summary>Chất lượng trung bình thang 1..5. Rỗng khi chưa có đánh giá nào.</summary>
    public double? ChatLuongTrungBinh { get; set; }

    /// <summary>Chưa hoàn thành việc nào — hiệu suất và đúng hạn đang là giá trị mặc định.</summary>
    public bool ChuaCoLichSu { get; set; }

    /// <summary>Kỹ năng nhiệm vụ cần mà người này có, kèm mức. Ví dụ "C# 4/5 (cần 3)".</summary>
    public IReadOnlyList<string> KyNangKhop { get; set; } = Array.Empty<string>();

    /// <summary>Kỹ năng nhiệm vụ cần mà người này chưa có.</summary>
    public IReadOnlyList<string> KyNangThieu { get; set; } = Array.Empty<string>();

    public IReadOnlyList<ViecTuongTuDto> ViecTuongTu { get; set; } = Array.Empty<ViecTuongTuDto>();
}

/// <summary>Một ứng viên được gợi ý.</summary>
public sealed class UngVienDto
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ChucDanh { get; set; }
    public string? TenPhongBan { get; set; }
    public string? TenNhom { get; set; }

    /// <summary>Điểm phù hợp tổng, trong khoảng [0, 1]. Bằng đúng tổng các phần đóng góp.</summary>
    public double Diem { get; set; }

    /// <summary>Thứ hạng trong danh sách, bắt đầu từ 1.</summary>
    public int ThuHang { get; set; }

    public ChiTietDiem ChiTietDiem { get; set; } = new();
    public SoLieuUngVien SoLieu { get; set; } = new();

    /// <summary>Lý do gợi ý bằng tiếng Việt.</summary>
    public IReadOnlyList<string> LyDo { get; set; } = Array.Empty<string>();
}

/// <summary>Độ khớp của nhiệm vụ với một phòng hoặc một nhóm.</summary>
public sealed class DiemDonViDto
{
    public long Id { get; set; }
    public string Ten { get; set; } = string.Empty;

    /// <summary>Điểm cuối, 0..1.</summary>
    public double Diem { get; set; }

    /// <summary>Phần so với đoạn mô tả đơn vị.</summary>
    public double DiemHoSo { get; set; }

    /// <summary>Phần so với những việc đơn vị đã làm. Rỗng khi chưa có lịch sử.</summary>
    public double? DiemLichSu { get; set; }
}

/// <summary>AI đoán nhiệm vụ thuộc phòng nào, nhóm nào.</summary>
public sealed class SuyLuanPhongBanDto
{
    /// <summary>CHAC_CHAN, LUONG_LU hoặc KHONG_RO.</summary>
    public string KetLuan { get; set; } = string.Empty;

    /// <summary>Câu tiếng Việt diễn giải kết luận.</summary>
    public string MoTa { get; set; } = string.Empty;

    public IReadOnlyList<DiemDonViDto> CacPhong { get; set; } = Array.Empty<DiemDonViDto>();

    /// <summary>Phòng được dùng để lọc ứng viên. Rỗng khi không rõ phòng.</summary>
    public IReadOnlyList<long> PhongDaChon { get; set; } = Array.Empty<long>();

    /// <summary>Nhóm khớp nhất trong phòng đứng đầu. Chỉ để tham khảo, không dùng để lọc.</summary>
    public DiemDonViDto? Nhom { get; set; }
}

/// <summary>Một kỹ năng nhiệm vụ đòi hỏi.</summary>
public sealed class KyNangYeuCauDto
{
    public long SkillId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Ten { get; set; } = string.Empty;
    public int? MucYeuCau { get; set; }

    /// <summary>MANUAL = người giao nhập; AI = trích tự động từ nội dung nhiệm vụ.</summary>
    public string Nguon { get; set; } = string.Empty;

    /// <summary>Độ khớp giữa nhiệm vụ và kỹ năng, chỉ có khi do AI trích.</summary>
    public double? DoKhop { get; set; }
}

/// <summary>Kết quả gợi ý.</summary>
public sealed class GoiYResponse
{
    /// <summary>Phiên bản bộ tham số đã dùng, để đối chiếu khi hiệu chỉnh mô hình.</summary>
    public string PhienBanTrongSo { get; set; } = string.Empty;

    /// <summary>Phương pháp đo độ gần nghĩa thực tế đã dùng: nhúng ngữ nghĩa, hoặc TF-IDF khi dự phòng.</summary>
    public string PhuongPhap { get; set; } = string.Empty;

    /// <summary>Nội dung nhiệm vụ đã dùng để chấm điểm.</summary>
    public string NoiDungDaDung { get; set; } = string.Empty;

    /// <summary>Số người trong phạm vi giao việc của người gọi.</summary>
    public int SoUngVienTrongPhamVi { get; set; }

    /// <summary>Số người thực sự được chấm, sau khi lọc theo phòng.</summary>
    public int SoUngVienDaXet { get; set; }

    public SuyLuanPhongBanDto SuyLuanPhongBan { get; set; } = new();

    public IReadOnlyList<KyNangYeuCauDto> KyNangYeuCau { get; set; } = Array.Empty<KyNangYeuCauDto>();

    /// <summary>Cảnh báo về chất lượng gợi ý.</summary>
    public IReadOnlyList<string> CanhBao { get; set; } = Array.Empty<string>();

    public IReadOnlyList<UngVienDto> UngVien { get; set; } = Array.Empty<UngVienDto>();

    /// <summary>Thời gian tính toán, mili giây.</summary>
    public long ThoiGianMs { get; set; }
}
