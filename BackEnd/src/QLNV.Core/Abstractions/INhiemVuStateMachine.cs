using QLNV.Core.Common;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;

namespace QLNV.Core.Abstractions;

/// <summary>
/// §2.4 — MAY TRANG THAI cua nhiem vu, cai dat cac chuyen T2..T14.
/// (T1 "Tao va giao nhiem vu" khong o day: no tao ban ghi moi chu khong chuyen trang thai
/// cua ban ghi da co; xem <c>TaoNhiemVuRequest</c> va §5.3 C1.)
///
/// HOP DONG BAT BUOC CUA MOI CAI DAT:
///  1. THUAN TUY — khong truy CSDL, khong <c>DateTime.Now</c>, khong I/O. Moc thoi gian
///     lay tu <see cref="NguCanh.HomNay"/>.
///  2. SUA TRUC TIEP doi tuong <paramref name="nv"/> truyen vao roi tra chinh no trong
///     <c>Result.DuLieu</c> (giong ban flow.js da kiem chung). Nho vay tang tren chi can
///     <c>SaveChanges</c>.
///  3. KHONG nem exception cho loi nghiep vu — moi loi tra ve qua <see cref="Result{T}"/>
///     voi thong bao TIENG VIET CO DAU.
///  4. TU KIEM QUYEN §6.2 truoc khi doi trang thai (§6.4: khong tin FE).
///  5. Ghi ban ghi lich su vao <see cref="NguCanh.KetXuat"/> chu khong tu luu.
/// </summary>
public interface INhiemVuStateMachine
{
    /// <summary>
    /// §2.4 T2 / §5.4 D1 — <b>Tiep nhan</b>. THIET KE MOI (§10.3: he goc khong co buoc nay).
    /// <c>(3, null)</c> -&gt; <c>(2, null)</c> va ghi <c>ngaytiepnhan</c>.
    /// Quyen: §6.2 dong 9 — toi la CHUTRI VA <c>trangthai = 3</c>.
    /// </summary>
    Result<DmNhiemVuChiTiet> TiepNhan(DmNhiemVuChiTiet nv, Guid userId, TiepNhanRequest? payload, NguCanh ctx);

    /// <summary>
    /// §2.4 T3 / §5.4 D2 — <b>Tu choi nhiem vu</b>.
    /// <c>(3|2, null)</c> -&gt; <c>(6, 10)</c>.
    /// Ly do BAT BUOC (§10.7 — yeu cau MOI, he goc khong bat buoc).
    /// Quyen: §6.2 dong 10 — CHUTRI VA <c>trangthai ∈ {2,3}</c> VA <c>solangiahan = 0</c>.
    /// Rang buoc bo sung da chot: chi tu <c>trangthaiDvXuly = null</c> (bam §2.4 T3), de tranh
    /// vong lap tu choi vo han qua nhanh T5.
    /// </summary>
    Result<DmNhiemVuChiTiet> TuChoi(DmNhiemVuChiTiet nv, Guid userId, TuChoiRequest payload, NguCanh ctx);

    /// <summary>
    /// §2.4 T4/T5 — <b>Nguoi giao xu ly de nghi tu choi</b>. MO RONG (§5 khong co endpoint rieng).
    /// CHAP_NHAN: <c>(6, 10)</c> -&gt; <c>(97, null)</c> (DIEM CUOI §2.6).
    /// BAC_BO:    <c>(6, 10)</c> -&gt; <c>(2, 12)</c> (nguoi thuc hien lam lai).
    /// </summary>
    Result<DmNhiemVuChiTiet> XuLyTuChoi(DmNhiemVuChiTiet nv, Guid userId, XuLyTuChoiRequest payload, NguCanh ctx);

    /// <summary>
    /// §2.4 T6 / §5.4 D3 — <b>Cap nhat tien do</b>. KHONG doi trang thai (ca hai truc).
    /// Validate <c>mucdoht</c> la so nguyen 0-100 (§10.2 — yeu cau MOI).
    /// Quyen: §6.2 dong 11 — CHUTRI VA <c>trangthai ∈ {2,3,7}</c> VA <c>trangthaiDvXuly ∈ {null,12}</c>.
    /// </summary>
    Result<DmNhiemVuChiTiet> CapNhatTienDo(DmNhiemVuChiTiet nv, Guid userId, TienDoRequest payload, NguCanh ctx);

    /// <summary>
    /// §2.4 T7 / §5.4 D4 — <b>Gui bao cao ket qua</b>.
    /// <c>(2|3|7, null|12)</c> -&gt; <c>(gia tri da chon, 10)</c>.
    /// PHAI validate lai <c>payload.TrangThai</c> bang <c>HanUtil.TrangThaiHopLeKhiBaoCao</c>
    /// (§5.4 D4, §6.4) va bat buoc noi dung khi trang thai chon thuoc {1, 5} (§1.2 buoc 5).
    /// </summary>
    Result<DmNhiemVuChiTiet> GuiBaoCao(DmNhiemVuChiTiet nv, Guid userId, BaoCaoRequest payload, NguCanh ctx);

    /// <summary>
    /// §2.4 T8 / §5.4 D6 — <b>Thu hoi bao cao</b>.
    /// <c>(1|5, 10)</c> -&gt; <c>(2 neu con han / 7 neu qua han, null)</c>.
    /// Quyen: §6.2 dong 13.
    /// </summary>
    Result<DmNhiemVuChiTiet> ThuHoiBaoCao(DmNhiemVuChiTiet nv, Guid userId, ThuHoiBaoCaoRequest? payload, NguCanh ctx);

    /// <summary>
    /// §2.4 T9/T10 / §5.5 E1 — <b>Kiem tra ket qua (nghiem thu)</b>.
    /// DAT:      <c>(1|5, 10)</c> -&gt; <c>(giu nguyen, 11)</c> + ghi <c>ngayhoanthanhthucte</c>. DIEM CUOI.
    /// CHUA_DAT: <c>trangthaiDvXuly := 12</c>; neu truc A thuoc {1,5} thi ve 2 (con han) / 7 (qua han).
    ///
    /// RANG BUOC DA KIEM CHUNG, PHAI GIU: chi cho ket qua DAT khi truc A thuoc {1, 5}.
    /// O <c>(2|3|7, 10)</c> nguoi thuc hien vua bao cao mot trang thai CHUA hoan thanh; neu
    /// cho DAT se sinh cap <c>(2|3|7, 11)</c> — khong phai diem cuoi §2.6 ma moi hanh dong
    /// tiep theo deu tat (nhiem vu chet).
    /// </summary>
    Result<DmNhiemVuChiTiet> NghiemThu(DmNhiemVuChiTiet nv, Guid userId, NghiemThuRequest payload, NguCanh ctx);

    /// <summary>
    /// §2.4 T11 / §5.6 F1 — <b>Xin gia han</b>.
    /// <c>trangthai := 13</c>, <c>trangthaixulygiahan := 10</c>, va them mot
    /// <see cref="GiaHanNhiemVu"/> vao <c>ctx.KetXuat.GiaHanMoi</c> (co ghi <c>TrangThaiCu</c>).
    /// Quyen: §6.2 dong 15 — CHUTRI VA <c>trangthai ∈ {2,3,7}</c> VA <c>trangthaixulygiahan ≠ 10</c>
    /// VA <c>solangiahan &lt; 2</c>.
    /// Han de xuat phai MUON HON <c>hanxulyth</c> hien tai (§3.3 M10).
    /// </summary>
    Result<DmNhiemVuChiTiet> XinGiaHan(DmNhiemVuChiTiet nv, Guid userId, GiaHanRequest payload, NguCanh ctx);

    /// <summary>
    /// §2.4 T12/T13 / §5.6 F2 — <b>Duyet / tu choi gia han</b>.
    /// Lam viec tren <see cref="NguCanh.GiaHanChoDuyet"/> (BAT BUOC nap truoc khi goi).
    /// DUYET:   <c>trangthaixulygiahan := 11</c>, <c>solangiahan += 1</c>, <c>hanxulyth := hanxulydexuat</c>.
    /// TU_CHOI: <c>trangthaixulygiahan := 12</c>, han giu nguyen.
    /// Sau do khoi phuc truc A tu <c>GiaHanNhiemVu.TrangThaiCu</c> co ap quy tac han §2.5:
    ///   - truoc do la 3 (chua tiep nhan) =&gt; giu 3 neu con han, 7 neu qua han
    ///     (KHONG duoc nhay sang 2, vi se bo qua buoc Tiep nhan T2 va thieu <c>ngaytiepnhan</c>);
    ///   - con lai =&gt; 2 neu con han, 7 neu qua han.
    ///
    /// RANG BUOC DA KIEM CHUNG, PHAI GIU: chan khi <c>trangthai = 97</c>. §2.6 coi 97 la DIEM CUOI,
    /// khong duoc "hoi sinh" nhiem vu da thu hoi bang cach duyet gia han.
    /// </summary>
    Result<DmNhiemVuChiTiet> DuyetGiaHan(DmNhiemVuChiTiet nv, Guid userId, DuyetGiaHanRequest payload, NguCanh ctx);

    /// <summary>
    /// §2.4 T14 / §5.3 C6 — <b>Thu hoi nhiem vu</b>.
    /// -&gt; <c>(97, null)</c>, DIEM CUOI §2.6.
    /// Quyen: §6.2 dong 7 — <c>userIdGiaoViec = toi</c> VA <c>trangthai ∉ {1,5,97}</c>.
    ///
    /// RANG BUOC DA KIEM CHUNG, PHAI GIU: neu con de xuat gia han dang treo (<c>trangThai = 10</c>)
    /// thi PHAI dong lai (-&gt; 12, dua vao <c>ctx.KetXuat.GiaHanCapNhat</c>); neu khong, nguoi giao
    /// van bam duoc "Duyet gia han" va ham DuyetGiaHan se gan lai truc A =&gt; hoi sinh diem cuoi.
    /// </summary>
    Result<DmNhiemVuChiTiet> ThuHoiNhiemVu(DmNhiemVuChiTiet nv, Guid userId, ThuHoiNhiemVuRequest? payload, NguCanh ctx);

    /// <summary>
    /// §1.3 — <b>Nhac viec</b>. CHI ghi lich su, KHONG doi trang thai, khong dinh kem tep,
    /// khong dem so lan (§10.8). Noi dung BAT BUOC, &lt;= 2000 ky tu.
    /// Quyen: §6.2 dong 17.
    /// </summary>
    Result<DmNhiemVuChiTiet> NhacViec(DmNhiemVuChiTiet nv, Guid userId, NhacViecRequest payload, NguCanh ctx);

    /// <summary>
    /// §5.3 C5 / §6.2 dong 6 — <b>Sua nhiem vu da giao</b> (chi khi <c>trangthai = 3</c>).
    /// Khong phai mot chuyen trang thai cua §2.4 nhung dung chung khung kiem quyen.
    /// </summary>
    Result<DmNhiemVuChiTiet> SuaNhiemVu(DmNhiemVuChiTiet nv, Guid userId, SuaNhiemVuRequest payload, NguCanh ctx);

    /// <summary>
    /// §2.5 / §5.9 I5 — quy tac tu dong theo han cho MOT nhiem vu:
    /// <c>trangthai 2 -&gt; 7</c> va <c>3 -&gt; 7</c> khi <c>hanxulyth &lt; hom nay</c>.
    /// Tra <c>true</c> neu co thay doi. Job nen goi ham nay cho tung ban ghi.
    /// </summary>
    bool ApQuyTacQuaHan(DmNhiemVuChiTiet nv, DateOnly homNay);
}
