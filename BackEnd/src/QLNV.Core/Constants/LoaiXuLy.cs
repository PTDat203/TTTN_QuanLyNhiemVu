namespace QLNV.Core.Constants;

/// <summary>
/// §4.4 — cot <c>loai</c> cua bang XULY_NHIEMVU.
/// Dac ta §4.4 quy dinh 4 ma: TIENDO | BAOCAO | TUCHOI | THUHOI_BC.
/// Mo hinh chung bo sung TIEPNHAN va NGHIEMTHU (§2.4 T2, T9/T10).
/// Cac ma con lai la MO RONG co chu dich, de moi hanh dong cua §2.4 deu de lai vet
/// trong lich su (§5.4 D7) — danh dau bang <see cref="LaMoRong"/>.
/// </summary>
public static class LoaiXuLy
{
    /// <summary>§2.4 T2 — Tiep nhan nhiem vu (thiet ke moi, §10.3).</summary>
    public const string TiepNhan = "TIEPNHAN";

    /// <summary>§2.4 T6 / §5.4 D3 — Cap nhat tien do (khong doi trang thai).</summary>
    public const string TienDo = "TIENDO";

    /// <summary>§2.4 T7 / §5.4 D4 — Gui bao cao ket qua.</summary>
    public const string BaoCao = "BAOCAO";

    /// <summary>§2.4 T3 / §5.4 D2 — Tu choi nhiem vu.</summary>
    public const string TuChoi = "TUCHOI";

    /// <summary>§2.4 T8 / §5.4 D6 — Thu hoi bao cao.</summary>
    public const string ThuHoiBaoCao = "THUHOI_BC";

    /// <summary>§2.4 T9/T10 / §5.5 E1 — Kiem tra ket qua (nghiem thu).</summary>
    public const string NghiemThu = "NGHIEMTHU";

    /// <summary>§2.4 T4/T5 — Nguoi giao xu ly de nghi tu choi. MO RONG.</summary>
    public const string XuLyTuChoi = "XL_TUCHOI";

    /// <summary>§2.4 T11 / §5.6 F1 — De xuat gia han. MO RONG.</summary>
    public const string GiaHan = "GIAHAN";

    /// <summary>§2.4 T12/T13 / §5.6 F2 — Duyet hoac tu choi gia han. MO RONG.</summary>
    public const string DuyetGiaHan = "DUYET_GIAHAN";

    /// <summary>§2.4 T14 / §5.3 C6 — Thu hoi nhiem vu. MO RONG.</summary>
    public const string ThuHoiNhiemVu = "THUHOI_NV";

    /// <summary>§1.3 — Nhac viec (khong doi trang thai). MO RONG.</summary>
    public const string NhacViec = "NHACVIEC";

    public static IReadOnlyList<string> ToanBo() => new[]
    {
        TiepNhan, TienDo, BaoCao, TuChoi, ThuHoiBaoCao, NghiemThu,
        XuLyTuChoi, GiaHan, DuyetGiaHan, ThuHoiNhiemVu, NhacViec
    };

    public static bool HopLe(string? ma) => ma is not null && ToanBo().Contains(ma);

    /// <summary>True neu ma nay khong nam trong 4 ma goc cua §4.4 (tuc la phan MO RONG).</summary>
    public static bool LaMoRong(string? ma) =>
        ma is XuLyTuChoi or GiaHan or DuyetGiaHan or ThuHoiNhiemVu or NhacViec;

    public static string Nhan(string? ma) => ma switch
    {
        TiepNhan => "Tiếp nhận nhiệm vụ",
        TienDo => "Cập nhật tiến độ",
        BaoCao => "Gửi báo cáo kết quả",
        TuChoi => "Từ chối nhiệm vụ",
        ThuHoiBaoCao => "Thu hồi báo cáo",
        NghiemThu => "Kiểm tra kết quả",
        XuLyTuChoi => "Xử lý đề nghị từ chối",
        GiaHan => "Đề xuất gia hạn",
        DuyetGiaHan => "Duyệt gia hạn",
        ThuHoiNhiemVu => "Thu hồi nhiệm vụ",
        NhacViec => "Nhắc việc",
        _ => "Không xác định"
    };
}
