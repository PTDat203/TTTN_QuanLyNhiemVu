using QLNV.Core.Dtos;

namespace QLNV.Core.Abstractions;

/// <summary>
/// §5.10 J1-J2 / §3.1 M02 — thong ke cho bang dieu khien.
/// §1.4: KHONG dung job nen thong ke va truc <c>TRANGTHAITHONGKE</c> cua he goc;
/// tinh truc tiep tu trang thai nghiep vu + han.
/// </summary>
public interface IThongKeService
{
    /// <summary>
    /// §5.10 J1 — 4 the dem + bieu do tron theo trang thai + bieu do cot theo don vi
    /// + danh sach "Viec cua toi sap den han".
    /// Pham vi du lieu tuan thu §6.2 dong 20: quan tri xem toan he thong, nguoi giao chi
    /// xem don vi minh va don vi con.
    /// </summary>
    Task<DashboardTongQuanDto> TongQuanAsync(Guid userId, CancellationToken ct);

    /// <summary>§5.10 J2 — so nhiem vu va ty le hoan thanh theo don vi.</summary>
    Task<IReadOnlyList<DashboardDonViDto>> TheoDonViAsync(Guid userId, CancellationToken ct);

    /// <summary>§3.1 M02 — viec cua toi sap den han (mac dinh &lt;= 3 ngay), ke ca da qua han.</summary>
    Task<IReadOnlyList<NhiemVuSapDenHanDto>> SapDenHanAsync(Guid userId, int soNgay, CancellationToken ct);

    /// <summary>§5.9 I2 — nang luc suy dien tu lich su theo tung linh vuc (chi de xem).</summary>
    Task<NangLucNguoiDungDto> NangLucAsync(Guid userId, CancellationToken ct);

    /// <summary>§5.9 I3 — chi so hieu qua theo linh vuc. <paramref name="linhVuc"/> null = tong hop.</summary>
    Task<HieuSuatNguoiDungDto> HieuSuatAsync(Guid userId, string? linhVuc, CancellationToken ct);

    /// <summary>§5.9 I4 — chay job tong hop <c>USER_HIEUSUAT</c> (cron 01:00).</summary>
    Task<KetQuaJobDto> ChayJobCapNhatHieuSuatAsync(CancellationToken ct);

    /// <summary>
    /// §5.9 I5 / §2.5 — chay job hang ngay chuyen <c>trangthai 2 -&gt; 7</c> va <c>3 -&gt; 7</c>
    /// khi <c>hanxulyth &lt; hom nay</c>.
    /// </summary>
    Task<KetQuaJobDto> ChayJobCapNhatQuaHanAsync(DateOnly homNay, CancellationToken ct);
}
