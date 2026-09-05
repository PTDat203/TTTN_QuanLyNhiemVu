using QLNV.Core.Common;
using QLNV.Core.Dtos;

namespace QLNV.Core.Abstractions;

/// <summary>
/// §9.8 — do hieu qua AI, lay so lieu cho man M13 va cho bao cao thuc tap.
/// Tat ca tinh tu bang <c>AI_GOIY_LOG</c> (§4.8) + du lieu nhiem vu.
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// §9.8 — Precision@1, Precision@3, ty le chap nhan, MRR, Gini tai, phan bo thu hang / diem.
    ///
    /// Quy uoc da kiem chung, PHAI GIU:
    ///  - mau so cua ca bon chi so la TONG SO LAN GOI Y (moi lan mo popup), khong phai
    ///    so lan co nguoi duoc chon; lan bo qua goi y dong gop 0 vao MRR;
    ///  - lam tron 2 chu so khi hien thi, nhung so voi nguong muc tieu bang GIA TRI THO.
    /// </summary>
    Task<ThongKeAiDto> ThongKeAiAsync(CancellationToken ct);

    /// <summary>
    /// §9.8 — he so Gini cua phan bo <c>so_nv_dang_mo</c> giua cac nguoi thuc hien.
    /// Ket qua thuoc [0, 1]; cang thap tai cang deu. Lam tron 3 chu so (2 chu so qua tho
    /// de phan biet cac chien luoc baseline).
    /// </summary>
    Task<double> GiniTaiAsync(CancellationToken ct);

    /// <summary>
    /// §9.8 — bang so sanh BAT BUOC co trong bao cao: mo hinh day du so voi
    /// (a) chon ngau nhien, (b) chon nguoi ranh nhat, (c) chon nguoi chuyen mon cao nhat.
    /// </summary>
    Task<IReadOnlyList<SoSanhBaselineDto>> SoSanhBaselineAsync(CancellationToken ct);

    /// <summary>§3.4 M13 — nhat ky goi y AI, phan trang.</summary>
    Task<PagedResult<AiGoiYLogDto>> NhatKyAsync(int trang, int kichThuoc, CancellationToken ct);
}
