namespace TaskApp.Api.Services.GoiY;

// ===========================================================================
// Dữ liệu đầu vào của bộ xếp hạng — đã nạp sẵn từ database, không còn phụ thuộc EF.
// Tách như vậy để phần đánh giá chạy lại được đúng bộ xếp hạng với dữ liệu "nhìn từ quá khứ".
// ===========================================================================

/// <summary>Một nhiệm vụ đã hoàn thành, nhìn từ một mốc thời gian.</summary>
public sealed class ViecLichSu
{
    public long TaskId { get; init; }
    public string TieuDe { get; init; } = string.Empty;

    /// <summary>Tiêu đề + mô tả, là đoạn văn đem đi so với nhiệm vụ đang cần giao.</summary>
    public string VanBan { get; init; } = string.Empty;

    public long AssigneeId { get; init; }
    public long? DepartmentId { get; init; }
    public long? TeamId { get; init; }
    public bool DungHan { get; init; }
    public int? ChatLuong { get; init; }
    public int? MucHoanThanh { get; init; }

    /// <summary>
    /// Kết quả đánh giá quy về 0..1: trung bình chất lượng và mức hoàn thành, thang 1..5 đổi
    /// thành 0..1. Rỗng khi người duyệt không chấm điểm.
    /// </summary>
    public double? HieuSuat
    {
        get
        {
            var cacDiem = new[] { ChatLuong, MucHoanThanh }.Where(x => x.HasValue).Select(x => x!.Value).ToList();
            return cacDiem.Count == 0 ? null : (cacDiem.Average() - 1.0) / 4.0;
        }
    }
}

/// <summary>Một ứng viên, gom sẵn mọi thứ cần để chấm điểm.</summary>
public sealed class HoSoUngVien
{
    public long UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string VaiTro { get; init; } = string.Empty;
    public string? ChucDanh { get; init; }
    public long? DepartmentId { get; init; }
    public string? TenPhong { get; init; }
    public long? TeamId { get; init; }
    public string? TenNhom { get; init; }

    /// <summary>Đoạn văn hồ sơ đưa vào mô hình, xem <see cref="Ai.VanBanHoSo.NguoiThucHien"/>.</summary>
    public string VanBanHoSo { get; init; } = string.Empty;

    /// <summary>Mã kỹ năng chuẩn → mức thành thạo 1..5.</summary>
    public IReadOnlyDictionary<long, int> MucKyNang { get; init; } = new Dictionary<long, int>();

    public IReadOnlyList<ViecLichSu> ViecDaXong { get; init; } = Array.Empty<ViecLichSu>();

    public int SoDangLam { get; init; }

    /// <summary>Tổng trọng số ưu tiên của các việc đang mở (HIGH 3, MEDIUM 2, LOW 1).</summary>
    public double TaiHienTai { get; init; }
}

/// <summary>Một phòng ban hoặc một nhóm, dưới dạng đoạn văn để so với nhiệm vụ.</summary>
public sealed class HoSoDonVi
{
    public long Id { get; init; }

    /// <summary>Với nhóm: phòng chứa nhóm. Với phòng: rỗng.</summary>
    public long? DepartmentId { get; init; }

    public string Ten { get; init; } = string.Empty;
    public string VanBan { get; init; } = string.Empty;
}

/// <summary>Một kỹ năng trong danh mục chuẩn.</summary>
public sealed class KyNangDanhMuc
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Ten { get; init; } = string.Empty;
    public string VanBan { get; init; } = string.Empty;
}

/// <summary>Một kỹ năng mà nhiệm vụ đòi hỏi.</summary>
public sealed class KyNangCan
{
    public long SkillId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Ten { get; init; } = string.Empty;

    /// <summary>Mức tối thiểu. Rỗng khi không nói rõ — lúc chấm dùng mức mặc định.</summary>
    public int? Muc { get; init; }

    /// <summary>AI hoặc MANUAL.</summary>
    public string Nguon { get; init; } = string.Empty;

    /// <summary>Độ khớp giữa nhiệm vụ và kỹ năng, chỉ có khi do AI trích.</summary>
    public double? DoKhop { get; init; }
}

/// <summary>Toàn bộ đầu vào của một lần xếp hạng.</summary>
public sealed class DauVaoXepHang
{
    public string NoiDung { get; init; } = string.Empty;
    public IReadOnlyList<HoSoUngVien> UngVien { get; init; } = Array.Empty<HoSoUngVien>();

    /// <summary>Các phòng có người nhận việc được — phòng chỉ có Giám đốc không nằm ở đây.</summary>
    public IReadOnlyList<HoSoDonVi> PhongBan { get; init; } = Array.Empty<HoSoDonVi>();

    public IReadOnlyList<HoSoDonVi> Nhom { get; init; } = Array.Empty<HoSoDonVi>();
    public IReadOnlyList<KyNangDanhMuc> DanhMucKyNang { get; init; } = Array.Empty<KyNangDanhMuc>();

    /// <summary>Mọi nhiệm vụ đã hoàn thành tính tới mốc thời gian — dùng cho cả phòng ban lẫn việc tương tự.</summary>
    public IReadOnlyList<ViecLichSu> LichSu { get; init; } = Array.Empty<ViecLichSu>();

    /// <summary>Kỹ năng yêu cầu đã lưu sẵn cho nhiệm vụ. Rỗng thì AI tự trích từ nội dung.</summary>
    public IReadOnlyList<KyNangCan>? KyNangCoSan { get; init; }
}

// ===========================================================================
// Kết quả
// ===========================================================================

public enum KetLuanPhongBan
{
    /// <summary>Một phòng khớp rõ vượt trội — chỉ xét người phòng đó.</summary>
    ChacChan,

    /// <summary>Hai phòng khớp sát nhau — xét người của cả hai.</summary>
    LuongLu,

    /// <summary>Không phòng nào khớp đủ — xét mọi người trong phạm vi.</summary>
    KhongRo
}

/// <summary>Điểm khớp của nhiệm vụ với một phòng hoặc một nhóm.</summary>
public sealed class DiemDonVi
{
    public HoSoDonVi DonVi { get; init; } = new();

    /// <summary>Điểm cuối, 0..1.</summary>
    public double Diem { get; init; }

    /// <summary>Phần so với đoạn mô tả phòng.</summary>
    public double DiemHoSo { get; init; }

    /// <summary>Phần so với các việc phòng đã làm. Rỗng khi phòng chưa có lịch sử.</summary>
    public double? DiemLichSu { get; init; }
}

public sealed class KetQuaSuyLuan
{
    public KetLuanPhongBan KetLuan { get; set; }
    public IReadOnlyList<DiemDonVi> CacPhong { get; set; } = Array.Empty<DiemDonVi>();
    public IReadOnlyList<long> PhongDaChon { get; set; } = Array.Empty<long>();
    public IReadOnlyList<DiemDonVi> CacNhom { get; set; } = Array.Empty<DiemDonVi>();

    /// <summary>Nhóm khớp rõ nhất trong phòng đứng đầu. Chỉ để tham khảo, không dùng để lọc.</summary>
    public DiemDonVi? Nhom { get; set; }

    public IReadOnlyList<KyNangCan> KyNang { get; set; } = Array.Empty<KyNangCan>();

    /// <summary>
    /// Độ khớp với TỪNG kỹ năng trong danh mục, 0..1 — kể cả những kỹ năng không được chọn. Chỉ có
    /// khi AI tự trích. Phần hiệu chỉnh cần đủ cả bảng này để dò ngưỡng.
    /// </summary>
    public IReadOnlyDictionary<long, double> DiemKyNang { get; set; } = new Dictionary<long, double>();
}

/// <summary>Sáu thành phần điểm, mỗi thành phần 0..1.</summary>
public sealed class DiemThanhPhan
{
    public double NguNghia { get; init; }
    public double MucKyNang { get; init; }
    public double HieuSuat { get; init; }
    public double ViecTuongTu { get; init; }
    public double DungHan { get; init; }
    public double KhoiLuong { get; init; }
}

public sealed class KetQuaUngVien
{
    public HoSoUngVien HoSo { get; init; } = new();
    public DiemThanhPhan Diem { get; init; } = new();

    /// <summary>Điểm tổng = tổng các phần đóng góp ĐÃ LÀM TRÒN, để số hiển thị luôn cộng khớp.</summary>
    public double Tong { get; init; }

    public int SoHoanThanh { get; init; }
    public int SoDungHan { get; init; }

    /// <summary>Chất lượng trung bình thang 1..5. Rỗng khi chưa có đánh giá nào.</summary>
    public double? ChatLuongTrungBinh { get; init; }

    /// <summary>Những việc đã làm gần với nhiệm vụ này nhất, kèm độ gần 0..1.</summary>
    public IReadOnlyList<(ViecLichSu Viec, double DoGan)> ViecGanNhat { get; init; }
        = Array.Empty<(ViecLichSu, double)>();

    /// <summary>Từng kỹ năng yêu cầu và mức người này đang có (rỗng = không có).</summary>
    public IReadOnlyList<(KyNangCan KyNang, int? MucCo)> DoiChieuKyNang { get; init; }
        = Array.Empty<(KyNangCan, int?)>();
}

public sealed class KetQuaXepHang
{
    public string PhuongPhap { get; set; } = string.Empty;

    /// <summary>Dịch vụ nhúng không phản hồi nên đã chạy lại toàn bộ bằng TF-IDF.</summary>
    public bool DaLuiVeTfIdf { get; set; }

    public KetQuaSuyLuan SuyLuan { get; set; } = new();

    /// <summary>Số người trong phạm vi giao việc, trước khi lọc theo phòng.</summary>
    public int SoTrongPhamVi { get; set; }

    /// <summary>AI đoán ra phòng nhưng phòng đó không có ai trong phạm vi — đã lùi về xét tất cả.</summary>
    public bool PhongNgoaiPhamVi { get; set; }

    public IReadOnlyList<KetQuaUngVien> UngVien { get; set; } = Array.Empty<KetQuaUngVien>();
}
