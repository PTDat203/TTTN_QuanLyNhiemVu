namespace QLNV.Core.Constants;

/// <summary>
/// §2.3 Truc phu C — <c>trangthaixulygiahan</c> (duyet gia han).
/// <c>null</c> = chua xin gia han.
/// </summary>
public static class TrangThaiGiaHan
{
    /// <summary>10 — Cho duyet gia han. §2.3 / §2.4 T11</summary>
    public const int ChoDuyet = 10;

    /// <summary>11 — Da duyet gia han (<c>solangiahan</c> +1). §2.3 / §2.4 T12</summary>
    public const int DaDuyet = 11;

    /// <summary>12 — Tu choi gia han (<c>solangiahan</c> giu nguyen). §2.3 / §2.4 T13</summary>
    public const int TuChoi = 12;

    /// <summary>Toan bo ma hop le cua truc C (khong tinh <c>null</c>). §2.3</summary>
    public static IReadOnlyList<int> ToanBo() => new[] { ChoDuyet, DaDuyet, TuChoi };

    /// <summary>Nhan tieng Viet mac dinh cho thong bao loi phia server (§10.5).</summary>
    public static string Nhan(int? ma) => ma switch
    {
        null => "Chưa xin gia hạn",
        ChoDuyet => "Chờ duyệt gia hạn",
        DaDuyet => "Đã duyệt gia hạn",
        TuChoi => "Từ chối gia hạn",
        _ => "Không xác định"
    };
}
