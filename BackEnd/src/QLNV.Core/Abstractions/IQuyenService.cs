using QLNV.Core.Dtos;
using QLNV.Core.Entities;

namespace QLNV.Core.Abstractions;

/// <summary>
/// §6.2 — 15 co quyen tren MOT nhiem vu, tinh truc tiep tu trang thai + bang phan cong.
/// §6.4: KHONG dung cac co do backend goc tinh san (<c>isxuly</c>, <c>istuchoi</c>...) vi
/// quy tac tinh cua chung khong ton tai trong ma nguon FE goc =&gt; khong kiem chung duoc.
///
/// 14 co dau tien bam dung 14 dong hanh dong cua §6.2 (dong 6-19).
/// Co thu 15 (<see cref="XuLyTuChoi"/>) la MO RONG: §6.2 khong co dong nao cho hanh dong
/// T4/T5 cua §2.4, nhung thieu no thi cap (6, 10) khong co loi ra.
/// </summary>
public sealed record QuyenNhiemVu(
    bool SuaNhiemVu,
    bool ThuHoiNhiemVu,
    bool ThuHoiPhanCong,
    bool TiepNhan,
    bool TuChoi,
    bool CapNhatTienDo,
    bool GuiBaoCao,
    bool ThuHoiBaoCao,
    bool KiemTraKetQua,
    bool XinGiaHan,
    bool DuyetGiaHan,
    bool NhacViec,
    bool XemChiTiet,
    bool TaiTep,
    bool XuLyTuChoi)
{
    /// <summary>Khong co quyen nao — dung khi nguoi dung khong lien quan toi nhiem vu.</summary>
    public static readonly QuyenNhiemVu KhongCo = new(
        false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false);

    /// <summary>Chuyen sang DTO de tra ve cho FE (§6.4 tang Frontend).</summary>
    public QuyenNhiemVuDto SangDto() => new()
    {
        SuaNhiemVu = SuaNhiemVu,
        ThuHoiNhiemVu = ThuHoiNhiemVu,
        ThuHoiPhanCong = ThuHoiPhanCong,
        TiepNhan = TiepNhan,
        TuChoi = TuChoi,
        CapNhatTienDo = CapNhatTienDo,
        GuiBaoCao = GuiBaoCao,
        ThuHoiBaoCao = ThuHoiBaoCao,
        KiemTraKetQua = KiemTraKetQua,
        XinGiaHan = XinGiaHan,
        DuyetGiaHan = DuyetGiaHan,
        NhacViec = NhacViec,
        XemChiTiet = XemChiTiet,
        TaiTep = TaiTep,
        XuLyTuChoi = XuLyTuChoi
    };
}

/// <summary>
/// Tinh quyen §6.2. Cai dat PHAI thuan tuy: chi doc <paramref name="nv"/> va
/// <see cref="NguCanh"/>, khong truy CSDL, khong doc dong ho he thong.
/// </summary>
public interface IQuyenService
{
    /// <summary>
    /// Tinh 15 co quyen cua <paramref name="userId"/> tren <paramref name="nv"/>.
    /// </summary>
    /// <param name="nv">Nhiem vu can xet.</param>
    /// <param name="userId">Nguoi dang dang nhap. Phai trung <c>ctx.NguoiThaoTac.Id</c>.</param>
    /// <param name="ctx">Ngu canh da nap day du (phan cong, cau hinh, hom nay).</param>
    QuyenNhiemVu Tinh(DmNhiemVuChiTiet nv, Guid userId, NguCanh ctx);

    /// <summary>
    /// §6.1 — vai tro cua nguoi dung tren nhiem vu (tien cho tang tren khi can giai trinh
    /// vi sao mot quyen bi tat).
    /// </summary>
    VaiTroNhiemVu TinhVaiTro(DmNhiemVuChiTiet nv, Guid userId, NguCanh ctx);
}
