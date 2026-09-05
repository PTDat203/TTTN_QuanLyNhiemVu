using System.Linq;
using QLNV.Core.Abstractions;
using QLNV.Core.Constants;
using QLNV.Core.Entities;

namespace QLNV.Core.Services;

/// <summary>
/// §6.2 — cai dat bang vai tro x hanh dong.
///
/// THUAN TUY: chi doc <c>nv</c> va <see cref="NguCanh"/>, KHONG truy CSDL, KHONG doc dong ho
/// he thong. Nho vay kiem thu xUnit chay duoc voi doi tuong dung san.
///
/// §6.4 — kiem 2 LOP:
///  - LOP 1: <c>SYS_USER.vaitro</c> co duoc phep goi hanh dong nay khong (cac o ❌ cua §6.2).
///           Dung <see cref="VaiTro.LaBenGiao"/> / <see cref="VaiTro.LaBenLam"/>.
///  - LOP 2: dieu kien DU LIEU (so huu ban ghi, co ten trong bang phan cong) + dieu kien
///           TRANG THAI (hai truc A/B + truc C + <c>solangiahan</c>).
///
/// Tai khoan bi khoa (<c>SYS_USER.trangthai = 0</c>) hoac <c>userId</c> khong trung
/// <c>ctx.NguoiThaoTac.Id</c> =&gt; TAT TOAN BO quyen (tra <see cref="QuyenNhiemVu.KhongCo"/>).
///
/// §6.4 — KHONG dung cac co do backend goc tinh san (<c>isxuly</c>, <c>istuchoi</c>):
/// quy tac tinh cua chung khong ton tai trong ma nguon FE goc nen khong kiem chung duoc.
/// </summary>
public sealed class QuyenService : IQuyenService
{
    /// <inheritdoc />
    public VaiTroNhiemVu TinhVaiTro(DmNhiemVuChiTiet nv, Guid userId, NguCanh ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return VaiTroNhiemVu.Tinh(nv, userId, ctx);
    }

    /// <inheritdoc />
    public QuyenNhiemVu Tinh(DmNhiemVuChiTiet nv, Guid userId, NguCanh ctx)
    {
        ArgumentNullException.ThrowIfNull(nv);
        ArgumentNullException.ThrowIfNull(ctx);

        var nguoiDung = ctx.NguoiThaoTac;

        // §4.7 + §6.4 — tai khoan khong khop ngu canh hoac da bi khoa: khong co quyen nao.
        if (userId == Guid.Empty || nguoiDung.Id != userId || !nguoiDung.DangHoatDong)
        {
            return QuyenNhiemVu.KhongCo;
        }

        var v = VaiTroNhiemVu.Tinh(nv, userId, ctx);

        // ---- LOP 1: truc `vaitro` (§6.2 cac cot QUAN_TRI / NGUOI_GIAO / NGUOI_THUC_HIEN) ----
        // §6.2 dong 6,7,8,14,16,17: cot NGUOI_THUC_HIEN = ❌.
        bool laBenGiao = VaiTro.LaBenGiao(nguoiDung.VaiTro);
        // §6.2 dong 9,10,11,12,13,15: chi cot NGUOI_THUC_HIEN co dau, hai cot kia = ❌.
        bool laBenLam = VaiTro.LaBenLam(nguoiDung.VaiTro);

        // §6.2 danh dau ✅ cho QUAN_TRI o cac dong 6,7,8,14,16,17,18,19.
        // Quyet dinh da chot (bam flow.js): quan tri BO QUA dieu kien SO HUU du lieu nhung
        // VAN phai thoa dieu kien TRANG THAI — hanh dong sai trang thai la vo nghia.
        bool quanTri = ctx.CauHinh.QuanTriToanQuyen && v.LaQuanTri;

        // ---- LOP 2: du lieu + trang thai ----
        int tt = nv.TrangThai;                       // truc A — §2.1
        int? dv = nv.TrangThaiDvXuly;                // truc B — §2.2
        int? gh = nv.TrangThaiXuLyGiaHan;            // truc C — §2.3
        int soLanGiaHan = nv.SoLanGiaHan;

        // Chu so huu ben giao: `userIdGiaoViec = toi`, hoac quan tri (khi bat co toan quyen).
        bool nguoiGiaoCuaNv = v.LaNguoiGiao || quanTri;

        // §6.2 dong 14 (bam `coTheXacNhan` cua he goc): nguoi TAO cung duoc coi la nguoi giao
        // KHI nhiem vu chua chi dinh can bo giao viec.
        bool nguoiGiaoHoacTao =
            v.LaNguoiGiao || (nv.UserIdGiaoViec == Guid.Empty && v.LaNguoiTao) || quanTri;

        // §6.2 dong 6 — Sua nhiem vu da giao: `userIdGiaoViec = toi` VA `trangthai = 3`.
        bool suaNhiemVu = laBenGiao && nguoiGiaoCuaNv && tt == TrangThaiNv.ChuaTrienKhai;

        // §6.2 dong 7 — Thu hoi nhiem vu (-> 97): `userIdGiaoViec = toi` VA `trangthai ∉ {1,5,97}`.
        bool thuHoiNhiemVu = laBenGiao && nguoiGiaoCuaNv && !TrangThaiNv.KetThuc.Contains(tt);

        // §6.2 dong 8 — Thu hoi phan cong (rut 1 nguoi): `userIdGiaoViec = toi` VA `trangthai ∉ {1,5}`.
        bool thuHoiPhanCong = laBenGiao && nguoiGiaoCuaNv && !TrangThaiNv.DaHoanThanh.Contains(tt);

        // §6.2 dong 9 — Tiep nhan: toi la CHUTRI VA `trangthai = 3`. (QUAN_TRI/NGUOI_GIAO = ❌)
        bool tiepNhan = laBenLam && v.LaChuTri && tt == TrangThaiNv.ChuaTrienKhai;

        // §6.2 dong 10 — Tu choi: CHUTRI VA `trangthai ∈ {2,3}` VA `solangiahan = 0`.
        // XUNG DOT DAC TA da chot: §6.2 dong 10 khong neu truc B, nhung §2.4 T3 ghi ro chi tu
        // (3, null) hoac (2, null). Chon theo §2.4 (may trang thai la nguon chuan cho chuyen
        // trang thai) => them ve `trangthaiDvXuly = null`, chan vong lap tu choi vo han
        // (tu choi -> (6,10) -> bac bo -> (2,12) -> tu choi lai -> ...).
        bool tuChoi = laBenLam && v.LaChuTri
                      && (tt == TrangThaiNv.DangTrienKhai || tt == TrangThaiNv.ChuaTrienKhai)
                      && dv is null
                      && soLanGiaHan == 0;

        // §6.2 dong 11 — Cap nhat tien do: CHUTRI VA `trangthai ∈ {2,3,7}` VA `trangthaiDvXuly ∈ {null,12}`.
        bool dangMoVaChoPhepXuLy = TrangThaiNv.DangMo.Contains(tt) && TrangThaiPh.ChoPhepXuLy(dv);
        bool capNhatTienDo = laBenLam && v.LaChuTri && dangMoVaChoPhepXuLy;

        // §6.2 dong 12 — Gui bao cao ket qua: dieu kien nhu dong 11.
        bool guiBaoCao = laBenLam && v.LaChuTri && dangMoVaChoPhepXuLy;

        // §6.2 dong 13 — Thu hoi bao cao: CHUTRI VA `trangthai ∈ {1,5}` VA `trangthaiDvXuly = 10`.
        bool thuHoiBaoCao = laBenLam && v.LaChuTri
                            && TrangThaiNv.DaHoanThanh.Contains(tt)
                            && dv == TrangThaiPh.ChoXacNhan;

        // §6.2 dong 14 — Kiem tra ket qua: `trangthaiDvXuly = 10` VA (`userIdGiaoViec = toi`
        // HOAC toi la nguoi tao khi chua chi dinh can bo).
        // RANG BUOC DA KIEM CHUNG, PHAI GIU: loai truc A = 6 va 97.
        //  - Cap (6, 10) thuoc §2.4 T4/T5 (Xu ly tu choi), khong phai T9/T10; neu de chung se
        //    sinh cac cap chet (6, 11) / (6, 12) ngoai may trang thai §2.4.
        //  - Ma 97 la DIEM CUOI §2.6, khong duoc nghiem thu.
        bool kiemTraKetQua = laBenGiao
                             && dv == TrangThaiPh.ChoXacNhan
                             && tt != TrangThaiNv.TuChoi
                             && tt != TrangThaiNv.DaThuHoi
                             && nguoiGiaoHoacTao;

        // §6.2 dong 15 — Xin gia han: CHUTRI VA `trangthai ∈ {2,3,7}` VA `trangthaixulygiahan ≠ 10`
        // VA `solangiahan < 2`.
        bool xinGiaHan = laBenLam && v.LaChuTri
                         && TrangThaiNv.DangMo.Contains(tt)
                         && gh != TrangThaiGiaHan.ChoDuyet
                         && soLanGiaHan < ctx.CauHinh.SoLanGiaHanToiDa;

        // §6.2 dong 16 — Duyet / tu choi gia han: `trangthaixulygiahan = 10` VA `userIdGiaoViec = toi`.
        // RANG BUOC DA KIEM CHUNG, PHAI GIU: them ve `trangthai ≠ 97`. §2.6 coi 97 la DIEM CUOI;
        // khong duoc "hoi sinh" nhiem vu da thu hoi bang cach duyet gia han con treo.
        bool duyetGiaHan = laBenGiao
                           && gh == TrangThaiGiaHan.ChoDuyet
                           && tt != TrangThaiNv.DaThuHoi
                           && nguoiGiaoCuaNv;

        // §6.2 dong 17 — Nhac viec: `userIdGiaoViec = toi` VA `trangthai ∉ {1,5,97}`.
        bool nhacViec = laBenGiao && nguoiGiaoCuaNv && !TrangThaiNv.KetThuc.Contains(tt);

        // §6.2 dong 18 — Xem chi tiet + lich su: la nguoi giao, hoac co ten trong NHIEMVU_PHANCONG
        // (ca CHUTRI lan PHOIHOP), hoac la nguoi tao ban ghi, hoac quan tri.
        // KHONG phu thuoc LOP 1: ca ba vai tro deu duoc xem (§6.2 cot nao cung ✅ hoac 🔸).
        bool xemChiTiet = v.CoLienQuan;

        // §6.2 dong 19 — Tai tep dinh kem: dieu kien nhu dong 18.
        bool taiTep = xemChiTiet;

        // MO RONG (§2.4 T4/T5) — §6.2 KHONG co dong nao cho "Xu ly de nghi tu choi".
        // Quy dinh cua app nho: nguoi giao (hoac nguoi tao khi chua chi dinh can bo, hoac quan tri)
        // xu ly khi nhiem vu dang o dung cap (6, 10). Thieu co nay thi (6, 10) khong co loi ra.
        bool xuLyTuChoi = laBenGiao
                          && tt == TrangThaiNv.TuChoi
                          && dv == TrangThaiPh.ChoXacNhan
                          && nguoiGiaoHoacTao;

        return new QuyenNhiemVu(
            SuaNhiemVu: suaNhiemVu,
            ThuHoiNhiemVu: thuHoiNhiemVu,
            ThuHoiPhanCong: thuHoiPhanCong,
            TiepNhan: tiepNhan,
            TuChoi: tuChoi,
            CapNhatTienDo: capNhatTienDo,
            GuiBaoCao: guiBaoCao,
            ThuHoiBaoCao: thuHoiBaoCao,
            KiemTraKetQua: kiemTraKetQua,
            XinGiaHan: xinGiaHan,
            DuyetGiaHan: duyetGiaHan,
            NhacViec: nhacViec,
            XemChiTiet: xemChiTiet,
            TaiTep: taiTep,
            XuLyTuChoi: xuLyTuChoi);
    }

    /// <summary>
    /// Doc mot co quyen theo ma hanh dong (ten khoa camelCase cua <c>QuyenNhiemVuDto</c>).
    /// Tien cho FE sinh menu ngu canh tu <see cref="MaTranChuyen.HanhDong"/>.
    /// Ma khong biet =&gt; <c>false</c>.
    /// </summary>
    public static bool DocCo(QuyenNhiemVu quyen, string? maHanhDong)
    {
        ArgumentNullException.ThrowIfNull(quyen);
        return maHanhDong switch
        {
            "suaNhiemVu" => quyen.SuaNhiemVu,
            "thuHoiNhiemVu" => quyen.ThuHoiNhiemVu,
            "thuHoiPhanCong" => quyen.ThuHoiPhanCong,
            "tiepNhan" => quyen.TiepNhan,
            "tuChoi" => quyen.TuChoi,
            "capNhatTienDo" => quyen.CapNhatTienDo,
            "guiBaoCao" => quyen.GuiBaoCao,
            "thuHoiBaoCao" => quyen.ThuHoiBaoCao,
            "kiemTraKetQua" => quyen.KiemTraKetQua,
            "xinGiaHan" => quyen.XinGiaHan,
            "duyetGiaHan" => quyen.DuyetGiaHan,
            "nhacViec" => quyen.NhacViec,
            "xemChiTiet" => quyen.XemChiTiet,
            "taiTep" => quyen.TaiTep,
            "xuLyTuChoi" => quyen.XuLyTuChoi,
            _ => false
        };
    }

    /// <summary>
    /// Danh sach hanh dong dang duoc phep — tien cho FE ve menu ngu canh (§3.3 M07).
    /// </summary>
    public static IReadOnlyList<MoTaHanhDong> DsHanhDongChoPhep(QuyenNhiemVu quyen)
    {
        ArgumentNullException.ThrowIfNull(quyen);
        return MaTranChuyen.HanhDong.Where(h => DocCo(quyen, h.Ma)).ToList();
    }
}
