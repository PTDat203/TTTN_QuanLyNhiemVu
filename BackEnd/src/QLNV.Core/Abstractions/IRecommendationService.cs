using QLNV.Core.Common;
using QLNV.Core.Dtos;

namespace QLNV.Core.Abstractions;

/// <summary>
/// §9 — dong co goi y nguoi thuc hien. Diem nhan cua de tai.
///
/// Ban chat: he thong cham diem da tieu chi co trong so (MCDM / rule-based), KHONG phai
/// mang no-ron (§9.9 ghi chu "Trung thuc ve thuat ngu khi bao ve").
/// Bat buoc GIAI THICH DUOC: moi ung vien tra kem diem thanh phan va ly do tieng Viet.
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// §5.8 H1 / §9.6 — cham diem va tra top-N ung vien.
    ///
    /// Thu tu bat buoc: loc cung §9.3 -&gt; cham S1..S5 §9.4 -&gt; xac dinh che do §9.5 -&gt;
    /// sinh nhan va ly do §9.6 -&gt; sap xep (nguoi QUA_TAI luon xep cuoi) -&gt; cat theo <c>soLuong</c>.
    ///
    /// KHONG BAO GIO tra danh sach rong khi con ung vien qua duoc bo loc: neu moi nguoi
    /// deu 0 diem thi xep theo S4 (ai ranh nhat) va ghi ro ly do vao <c>canhBao</c> (§9.5).
    /// Ham nay CO ghi mot ban ghi <c>AI_GOIY_LOG</c> (§4.8) de sau con do ty le chap nhan.
    /// </summary>
    Task<GoiYResponse> GoiYAsync(GoiYRequest req, CancellationToken ct);

    /// <summary>
    /// §5.8 H2 — ghi nhan nguoi giao thuc te da chon ai.
    /// Cap nhat <c>userid_da_chon</c>, <c>thu_hang_da_chon</c> va <c>co_trong_goi_y</c>.
    ///
    /// RANG BUOC DA KIEM CHUNG, PHAI GIU: <c>co_trong_goi_y</c> CHOT CUNG o top-5,
    /// khong phu thuoc <c>soLuong</c> cua request. Neu khong, nguoi giao bam
    /// "Xem them 5 nguoi" (soLuong = 10) se lam Precision@3 / ty le chap nhan / MRR
    /// khong con so sanh duoc giua cac lan goi (§9.8).
    /// </summary>
    /// <param name="goiYId">Id lan goi y (<c>AI_GOIY_LOG.id</c>).</param>
    /// <param name="userIdDaChon">Nguoi duoc chon. <c>null</c> = bo qua goi y, chon thu cong.</param>
    Task GhiKetQuaAsync(Guid goiYId, Guid? userIdDaChon, CancellationToken ct);

    /// <summary>§5.8 H4 — doc bo trong so dang hieu luc.</summary>
    Task<CauHinhAiDto> DocCauHinhAsync(CancellationToken ct);

    /// <summary>
    /// §5.8 H4 — cap nhat bo trong so. Tra loi nghiep vu (khong nem) khi
    /// <c>w1 + ... + w5 != 1,00</c> hoac cac nguong khong hop le.
    /// </summary>
    Task<Result<CauHinhAiDto>> LuuCauHinhAsync(CauHinhAiDto cauHinh, CancellationToken ct);

    /// <summary>§5.8 H4 — kiem tra bo cau hinh ma khong luu (dung cho nut "Kiem tra" tren M13).</summary>
    KiemTraCauHinhAiDto KiemTraCauHinh(CauHinhAiDto cauHinh);
}
