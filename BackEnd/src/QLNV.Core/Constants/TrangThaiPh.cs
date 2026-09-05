namespace QLNV.Core.Constants;

/// <summary>
/// §2.2 Truc B — <c>trangthaiDvXuly</c> (trang thai kiem tra ket qua).
/// <c>null</c> = chua gui bao cao (duoc phep xu ly).
/// CHU Y: KHONG co ma 14 — nhanh "Trinh cap tren" bi luoc bo (§1.4).
/// </summary>
public static class TrangThaiPh
{
    /// <summary>10 — Cho xac nhan (da gui bao cao, cho nguoi giao kiem tra). §2.2</summary>
    public const int ChoXacNhan = 10;

    /// <summary>11 — Da xac nhan (ket qua DAT). §2.2 / §2.4 T9</summary>
    public const int DaXacNhan = 11;

    /// <summary>12 — Tu choi (ket qua CHUA DAT, yeu cau bo sung). §2.2 / §2.4 T10</summary>
    public const int TuChoi = 12;

    /// <summary>Toan bo ma hop le cua truc B (khong tinh <c>null</c>). §2.2</summary>
    public static IReadOnlyList<int> ToanBo() => new[] { ChoXacNhan, DaXacNhan, TuChoi };

    /// <summary>
    /// {null, 12} — hai gia tri cho phep nguoi thuc hien cap nhat tien do / gui bao cao
    /// (§6.2 dong 11 va 12).
    /// </summary>
    public static bool ChoPhepXuLy(int? ma) => ma is null or TuChoi;

    /// <summary>Nhan tieng Viet mac dinh cho thong bao loi phia server (§10.5).</summary>
    public static string Nhan(int? ma) => ma switch
    {
        null => "Chưa gửi báo cáo",
        ChoXacNhan => "Chờ xác nhận",
        DaXacNhan => "Đã xác nhận",
        TuChoi => "Từ chối",
        _ => "Không xác định"
    };
}
