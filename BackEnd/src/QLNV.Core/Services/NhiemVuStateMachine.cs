using System.Globalization;
using System.Linq;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;

namespace QLNV.Core.Services;

/// <summary>
/// §2.4 — cai dat MAY TRANG THAI T2..T14 (T1 "Tao &amp; giao" khong o day: no tao ban ghi moi).
///
/// HOP DONG (xem <see cref="INhiemVuStateMachine"/>):
///  1. THUAN TUY — khong DbContext, khong I/O, khong <c>DateTime.Now</c>. Moc thoi gian lay
///     tu <c>ctx.HomNay</c> nen kiem thu xUnit chay duoc ma khong can CSDL.
///  2. Sua TRUC TIEP doi tuong <c>nv</c> truyen vao roi tra chinh no trong <c>Result.DuLieu</c>.
///  3. Khong nem exception cho loi nghiep vu — moi loi tra qua <see cref="Result{T}"/> voi
///     thong bao TIENG VIET CO DAU.
///  4. §6.4 kiem 2 LOP: moi hanh dong TU KIEM QUYEN qua <see cref="IQuyenService"/> truoc khi
///     doi trang thai, khong tin co do FE gui len.
///  5. Moi hanh dong sinh 1 ban ghi <see cref="XuLyNhiemVu"/> vao <c>ctx.KetXuat.LichSuXuLyMoi</c>
///     (ngoai le duy nhat: <see cref="SuaNhiemVu"/> — xem ghi chu tai cho).
/// </summary>
public sealed class NhiemVuStateMachine : INhiemVuStateMachine
{
    private readonly IQuyenService _quyen;

    /// <summary>Khoi tao voi <see cref="QuyenService"/> mac dinh.</summary>
    public NhiemVuStateMachine() : this(new QuyenService())
    {
    }

    /// <summary>Khoi tao voi mot cai dat quyen khac (tien cho kiem thu doi khang).</summary>
    public NhiemVuStateMachine(IQuyenService quyen)
    {
        _quyen = quyen ?? throw new ArgumentNullException(nameof(quyen));
    }

    // =====================================================================
    // KHUNG CHUNG
    // =====================================================================

    /// <summary>
    /// §6.4 — kiem tra tien dieu kien chung roi kiem quyen §6.2.
    /// Tra <c>null</c> khi HOP LE; tra <c>Result</c> loi khi khong duoc phep.
    /// </summary>
    private Result<DmNhiemVuChiTiet>? KiemTruoc(
        DmNhiemVuChiTiet? nv,
        Guid userId,
        NguCanh? ctx,
        Func<QuyenNhiemVu, bool> chonCo,
        string tenHanhDong)
    {
        if (nv is null)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai("Không tìm thấy nhiệm vụ.", MaLoiChung.KhongTimThay);
        }
        if (ctx is null)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Thiếu ngữ cảnh xử lý nhiệm vụ.", MaLoiChung.DuLieuKhongHopLe);
        }
        if (userId == Guid.Empty)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Chưa xác định người thực hiện thao tác.", MaLoiChung.ChuaDangNhap);
        }

        var quyen = _quyen.Tinh(nv, userId, ctx);
        if (!chonCo(quyen))
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                $"Bạn không có quyền {tenHanhDong} ở trạng thái hiện tại " +
                $"({TrangThaiNv.Nhan(nv.TrangThai)} / {TrangThaiPh.Nhan(nv.TrangThaiDvXuly)}).",
                MaLoiChung.KhongCoQuyen);
        }
        return null;
    }

    /// <summary>
    /// §4.2 / §4.4 — kiem tra mot o van ban: bat buoc (neu can) va do dai toi da.
    /// Tra <c>null</c> khi hop le, nguoc lai tra thong bao loi tieng Viet.
    /// </summary>
    private static string? KiemNoiDung(string? giaTri, bool batBuoc, string tenTruong, NguCanh ctx)
    {
        var s = (giaTri ?? string.Empty).Trim();
        if (batBuoc && s.Length == 0)
        {
            return $"Vui lòng nhập {tenTruong}.";
        }
        int toiDa = ctx.CauHinh.DoDaiNoiDungToiDa;
        if (s.Length > toiDa)
        {
            return $"{HoaChuDau(tenTruong)} không được vượt quá {toiDa} ký tự.";
        }
        return null;
    }

    /// <summary>Viet hoa chu cai dau — chi dung de dung cau thong bao loi.</summary>
    private static string HoaChuDau(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], CultureInfo.InvariantCulture) + s[1..];

    /// <summary>Cat khoang trang hai dau; <c>null</c> thanh chuoi rong.</summary>
    private static string Sach(string? s) => (s ?? string.Empty).Trim();

    /// <summary>Cat khoang trang; chuoi rong thanh <c>null</c> (de khong ghi rac xuong CSDL).</summary>
    private static string? SachHoacNull(string? s)
    {
        var v = Sach(s);
        return v.Length == 0 ? null : v;
    }

    /// <summary>Chuan hoa ma ket qua do FE gui len: cat khoang trang + viet hoa.</summary>
    private static string ChuanHoaMa(string? s) => Sach(s).ToUpperInvariant();

    /// <summary>
    /// §4.4 — sinh 1 ban ghi lich su xu ly va day vao <c>ctx.KetXuat.LichSuXuLyMoi</c>.
    /// May trang thai KHONG tu luu; tang tren doc ra roi ghi CSDL trong cung giao dich.
    /// </summary>
    private static XuLyNhiemVu GhiXuLy(
        NguCanh ctx,
        DmNhiemVuChiTiet nv,
        Guid userId,
        string loai,
        string? noiDung,
        int? mucDoHt,
        int? trangThai,
        int? trangThaiXuLy,
        int? trangThaiDvXuly)
    {
        var banGhi = new XuLyNhiemVu
        {
            IdCtnv = nv.Id,
            Loai = loai,
            NoiDung = noiDung,
            MucDoHt = mucDoHt,
            TrangThai = trangThai,
            TrangThaiXuLy = trangThaiXuLy,
            TrangThaiDvXuly = trangThaiDvXuly,
            UserIdXuLy = userId,
            NgayXuLy = NgayUtil.SangMoc(ctx.HomNay)
        };
        ctx.KetXuat.LichSuXuLyMoi.Add(banGhi);
        return banGhi;
    }

    /// <summary>Danh dau nhiem vu vua bi sua (§4.2 <c>updatedate</c>).</summary>
    private static void DanhDauSua(DmNhiemVuChiTiet nv, NguCanh ctx) =>
        nv.UpdateDate = NgayUtil.SangMoc(ctx.HomNay);

    /// <summary>
    /// §10.2 — kiem tra <c>mucdoht</c> phai la so nguyen 0-100.
    /// He goc KHONG validate (o <c>input type="text"</c>); day la yeu cau MOI, BAT BUOC.
    /// (Kieu <c>int</c> cua DTO da chan sang chuoi/NaN o tang binding; o day chan mien gia tri.)
    /// </summary>
    private static string? KiemMucDoHt(int giaTri) =>
        giaTri < GioiHan.MucDoHtMin || giaTri > GioiHan.MucDoHtMax
            ? $"Mức độ hoàn thành phải là số nguyên từ {GioiHan.MucDoHtMin} đến {GioiHan.MucDoHtMax}."
            : null;

    // =====================================================================
    // §2.4 T2 — TIEP NHAN  (§1.2 buoc 3, §5.4 D1)
    // =====================================================================

    /// <inheritdoc />
    public Result<DmNhiemVuChiTiet> TiepNhan(
        DmNhiemVuChiTiet nv, Guid userId, TiepNhanRequest? payload, NguCanh ctx)
    {
        // §6.2 dong 9 — CHUTRI VA trangthai = 3.
        var chan = KiemTruoc(nv, userId, ctx, q => q.TiepNhan, "tiếp nhận nhiệm vụ");
        if (chan is not null) return chan;

        var loi = KiemNoiDung(payload?.NoiDung, false, "nội dung tiếp nhận", ctx);
        if (loi is not null) return Result<DmNhiemVuChiTiet>.ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);

        // §2.4 T2: (3, null) -> (2, null) + ngaytiepnhan. THIET KE MOI (§10.3).
        nv.TrangThai = TrangThaiNv.DangTrienKhai;
        nv.NgayTiepNhan = NgayUtil.SangMoc(ctx.HomNay);
        DanhDauSua(nv, ctx);

        GhiXuLy(ctx, nv, userId, LoaiXuLy.TiepNhan,
            noiDung: SachHoacNull(payload?.NoiDung) ?? "Đã tiếp nhận nhiệm vụ.",
            mucDoHt: nv.MucDoHt,
            trangThai: TrangThaiNv.DangTrienKhai,
            trangThaiXuLy: null,
            trangThaiDvXuly: nv.TrangThaiDvXuly);

        return Result<DmNhiemVuChiTiet>.Ok(nv);
    }

    // =====================================================================
    // §2.4 T3 — TU CHOI NHIEM VU  (§1.2 buoc 3, §5.4 D2)
    // =====================================================================

    /// <inheritdoc />
    public Result<DmNhiemVuChiTiet> TuChoi(
        DmNhiemVuChiTiet nv, Guid userId, TuChoiRequest payload, NguCanh ctx)
    {
        // §6.2 dong 10 — CHUTRI VA trangthai ∈ {2,3} VA solangiahan = 0 VA trangthaiDvXuly = null.
        var chan = KiemTruoc(nv, userId, ctx, q => q.TuChoi, "từ chối nhiệm vụ");
        if (chan is not null) return chan;

        if (payload is null)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Thiếu dữ liệu từ chối nhiệm vụ.", MaLoiChung.DuLieuKhongHopLe);
        }

        // §10.7 — he goc KHONG bat buoc ly do (o "Noi dung xu ly" chi required khi trang thai
        // la '1' hoac '5'). BAT BUOC nhap ly do la RANG BUOC MOI cua app nho.
        var loi = KiemNoiDung(payload.LyDo, true, "lý do từ chối", ctx);
        if (loi is not null) return Result<DmNhiemVuChiTiet>.ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);

        // §2.4 T3: (3|2, null) -> (6, 10).
        nv.TrangThai = TrangThaiNv.TuChoi;
        nv.TrangThaiDvXuly = TrangThaiPh.ChoXacNhan;
        DanhDauSua(nv, ctx);

        GhiXuLy(ctx, nv, userId, LoaiXuLy.TuChoi,
            noiDung: Sach(payload.LyDo),
            mucDoHt: nv.MucDoHt,
            trangThai: TrangThaiNv.TuChoi,
            // Bam he goc: CATE_FORM.REFUSE ep trangthai = 6 VA trangthaiXuly = 10.
            trangThaiXuLy: TrangThaiPh.ChoXacNhan,
            trangThaiDvXuly: TrangThaiPh.ChoXacNhan);

        return Result<DmNhiemVuChiTiet>.Ok(nv);
    }

    // =====================================================================
    // §2.4 T4/T5 — NGUOI GIAO XU LY DE NGHI TU CHOI
    // =====================================================================

    /// <inheritdoc />
    public Result<DmNhiemVuChiTiet> XuLyTuChoi(
        DmNhiemVuChiTiet nv, Guid userId, XuLyTuChoiRequest payload, NguCanh ctx)
    {
        // MO RONG: §6.2 khong co dong nao cho hanh dong nay. Co thu 15 cua QuyenNhiemVu
        // yeu cau dung cap (6, 10) + la nguoi giao/nguoi tao/quan tri.
        var chan = KiemTruoc(nv, userId, ctx, q => q.XuLyTuChoi, "xử lý đề nghị từ chối");
        if (chan is not null) return chan;

        if (payload is null)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Thiếu dữ liệu xử lý đề nghị từ chối.", MaLoiChung.DuLieuKhongHopLe);
        }

        var ketQua = ChuanHoaMa(payload.KetQua);
        if (!KetQuaXuLyTuChoi.HopLe(ketQua))
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Kết quả xử lý từ chối phải là \"CHAP_NHAN\" hoặc \"BAC_BO\".",
                MaLoiChung.DuLieuKhongHopLe);
        }

        var loi = KiemNoiDung(payload.PhanHoi, false, "ý kiến phản hồi", ctx);
        if (loi is not null) return Result<DmNhiemVuChiTiet>.ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);

        if (ketQua == KetQuaXuLyTuChoi.ChapNhan)
        {
            // §2.4 T4: (6, 10) -> (97, null) — DIEM CUOI §2.6.
            nv.TrangThai = TrangThaiNv.DaThuHoi;
            nv.TrangThaiDvXuly = null;
        }
        else
        {
            // §2.4 T5: (6, 10) -> (2, 12) — nguoi thuc hien lam lai.
            // CHU Y: truc B phai la 12, KHONG phai null. Neu de null thi cap (2, null) lai
            // mo khoa quyen "Tu choi" (§6.2 dong 10) => vong lap tu choi vo han.
            nv.TrangThai = TrangThaiNv.DangTrienKhai;
            nv.TrangThaiDvXuly = TrangThaiPh.TuChoi;
        }

        nv.PhanHoi = SachHoacNull(payload.PhanHoi);
        DanhDauSua(nv, ctx);

        GhiXuLy(ctx, nv, userId, LoaiXuLy.XuLyTuChoi,
            noiDung: SachHoacNull(payload.PhanHoi)
                     ?? (ketQua == KetQuaXuLyTuChoi.ChapNhan
                         ? "Chấp nhận đề nghị từ chối."
                         : "Bác bỏ đề nghị từ chối."),
            mucDoHt: nv.MucDoHt,
            trangThai: nv.TrangThai,
            trangThaiXuLy: null,
            trangThaiDvXuly: nv.TrangThaiDvXuly);

        return Result<DmNhiemVuChiTiet>.Ok(nv);
    }

    // =====================================================================
    // §2.4 T6 — CAP NHAT TIEN DO  (§1.2 buoc 4, §5.4 D3)
    // =====================================================================

    /// <inheritdoc />
    public Result<DmNhiemVuChiTiet> CapNhatTienDo(
        DmNhiemVuChiTiet nv, Guid userId, TienDoRequest payload, NguCanh ctx)
    {
        // §6.2 dong 11 — CHUTRI VA trangthai ∈ {2,3,7} VA trangthaiDvXuly ∈ {null,12}.
        var chan = KiemTruoc(nv, userId, ctx, q => q.CapNhatTienDo, "cập nhật tiến độ");
        if (chan is not null) return chan;

        if (payload is null)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Thiếu dữ liệu cập nhật tiến độ.", MaLoiChung.DuLieuKhongHopLe);
        }

        // §10.2 — yeu cau MOI, BAT BUOC: chan gia tri am va > 100.
        var loiMucDo = KiemMucDoHt(payload.MucDoHt);
        if (loiMucDo is not null)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(loiMucDo, MaLoiChung.DuLieuKhongHopLe);
        }

        var loi = KiemNoiDung(payload.NoiDung, false, "nội dung công việc đã làm", ctx);
        if (loi is not null) return Result<DmNhiemVuChiTiet>.ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);

        // §2.4 T6: KHONG doi trang thai o CA HAI truc, chi ghi mucdoht + lich su.
        nv.MucDoHt = payload.MucDoHt;
        DanhDauSua(nv, ctx);

        GhiXuLy(ctx, nv, userId, LoaiXuLy.TienDo,
            noiDung: SachHoacNull(payload.NoiDung),
            mucDoHt: payload.MucDoHt,
            trangThai: nv.TrangThai,
            trangThaiXuLy: null,
            trangThaiDvXuly: nv.TrangThaiDvXuly);

        return Result<DmNhiemVuChiTiet>.Ok(nv);
    }

    // =====================================================================
    // §2.4 T7 — GUI BAO CAO KET QUA  (§1.2 buoc 5, §5.4 D4)
    // =====================================================================

    /// <inheritdoc />
    public Result<DmNhiemVuChiTiet> GuiBaoCao(
        DmNhiemVuChiTiet nv, Guid userId, BaoCaoRequest payload, NguCanh ctx)
    {
        // §6.2 dong 12 — dieu kien nhu dong 11.
        var chan = KiemTruoc(nv, userId, ctx, q => q.GuiBaoCao, "gửi báo cáo kết quả");
        if (chan is not null) return chan;

        if (payload is null)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Thiếu dữ liệu báo cáo kết quả.", MaLoiChung.DuLieuKhongHopLe);
        }

        // §5.4 D4 + §6.4 — server VALIDATE LAI trang thai co nam trong danh sach da loc theo han
        // hay khong. KHONG duoc tin gia tri FE gui len.
        var hopLe = HanUtil.TrangThaiHopLeKhiBaoCao(nv.HanXuLyTh, ctx.HomNay);
        int trangThaiChon = payload.TrangThai;
        if (!hopLe.Contains(trangThaiChon))
        {
            var danhSach = string.Join(", ", hopLe.Select(m => TrangThaiNv.Nhan(m)));
            return Result<DmNhiemVuChiTiet>.ThatBai(
                $"Kết quả xử lý không hợp lệ theo hạn của nhiệm vụ. Chỉ được chọn: {danhSach}.",
                MaLoiChung.SaiTrangThai);
        }

        // §1.2 buoc 5 — noi dung xu ly BAT BUOC khi chon trang thai 1 hoac 5.
        bool batBuocNoiDung = TrangThaiNv.DaHoanThanh.Contains(trangThaiChon);
        var loi = KiemNoiDung(payload.NoiDung, batBuocNoiDung, "nội dung báo cáo", ctx);
        if (loi is not null) return Result<DmNhiemVuChiTiet>.ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);

        // §1.2 buoc 4 + §10.2 — mucdoht TUY CHON o buoc bao cao, nhung neu co gui len thi phai
        // la so nguyen 0-100. KHONG duoc nuot gia tri rac am tham.
        if (payload.MucDoHt.HasValue)
        {
            var loiMucDo = KiemMucDoHt(payload.MucDoHt.Value);
            if (loiMucDo is not null)
            {
                return Result<DmNhiemVuChiTiet>.ThatBai(loiMucDo, MaLoiChung.DuLieuKhongHopLe);
            }
            nv.MucDoHt = payload.MucDoHt.Value;
        }

        // §2.4 T7: (2|3|7, null|12) -> (gia tri da chon, 10).
        nv.TrangThai = trangThaiChon;
        nv.TrangThaiDvXuly = TrangThaiPh.ChoXacNhan;
        DanhDauSua(nv, ctx);

        GhiXuLy(ctx, nv, userId, LoaiXuLy.BaoCao,
            noiDung: SachHoacNull(payload.NoiDung),
            mucDoHt: nv.MucDoHt,
            trangThai: trangThaiChon,
            trangThaiXuLy: TrangThaiPh.ChoXacNhan,   // §4.4 — = 10 khi gui bao cao
            trangThaiDvXuly: TrangThaiPh.ChoXacNhan);

        return Result<DmNhiemVuChiTiet>.Ok(nv);
    }

    // =====================================================================
    // §2.4 T8 — THU HOI BAO CAO  (§1.2 buoc 5, §5.4 D6)
    // =====================================================================

    /// <inheritdoc />
    public Result<DmNhiemVuChiTiet> ThuHoiBaoCao(
        DmNhiemVuChiTiet nv, Guid userId, ThuHoiBaoCaoRequest? payload, NguCanh ctx)
    {
        // §6.2 dong 13 — CHUTRI VA trangthai ∈ {1,5} VA trangthaiDvXuly = 10.
        var chan = KiemTruoc(nv, userId, ctx, q => q.ThuHoiBaoCao, "thu hồi báo cáo");
        if (chan is not null) return chan;

        var loi = KiemNoiDung(payload?.LyDo, false, "lý do thu hồi", ctx);
        if (loi is not null) return Result<DmNhiemVuChiTiet>.ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);

        // §2.4 T8 + §2.5: con han (hoac khong co han) -> 2 ; qua han -> 7. Truc B ve null.
        nv.TrangThai = HanUtil.TrangThaiTheoHan(nv.HanXuLyTh, ctx.HomNay);
        nv.TrangThaiDvXuly = null;
        DanhDauSua(nv, ctx);

        GhiXuLy(ctx, nv, userId, LoaiXuLy.ThuHoiBaoCao,
            noiDung: SachHoacNull(payload?.LyDo) ?? "Thu hồi báo cáo để chỉnh sửa.",
            mucDoHt: nv.MucDoHt,
            trangThai: nv.TrangThai,
            trangThaiXuLy: null,
            trangThaiDvXuly: null);

        return Result<DmNhiemVuChiTiet>.Ok(nv);
    }

    // =====================================================================
    // §2.4 T9/T10 — KIEM TRA KET QUA (NGHIEM THU)  (§1.2 buoc 6, §5.5 E1)
    // =====================================================================

    /// <inheritdoc />
    public Result<DmNhiemVuChiTiet> NghiemThu(
        DmNhiemVuChiTiet nv, Guid userId, NghiemThuRequest payload, NguCanh ctx)
    {
        // §6.2 dong 14 — trangthaiDvXuly = 10 VA (nguoi giao / nguoi tao / quan tri),
        // da loai truc A = 6 va 97 trong QuyenService.
        var chan = KiemTruoc(nv, userId, ctx, q => q.KiemTraKetQua, "kiểm tra kết quả");
        if (chan is not null) return chan;

        if (payload is null)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Thiếu dữ liệu nghiệm thu.", MaLoiChung.DuLieuKhongHopLe);
        }

        var ketQua = ChuanHoaMa(payload.KetQua);
        if (!KetQuaNghiemThu.HopLe(ketQua))
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Kết quả nghiệm thu phải là \"DAT\" hoặc \"CHUA_DAT\".",
                MaLoiChung.DuLieuKhongHopLe);
        }

        // §1.2 buoc 6 + §10.7 — noi dung phan hoi BAT BUOC (he goc khong bat buoc: yeu cau MOI).
        var loi = KiemNoiDung(payload.PhanHoi, true, "nội dung phản hồi", ctx);
        if (loi is not null) return Result<DmNhiemVuChiTiet>.ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);

        if (ketQua == KetQuaNghiemThu.Dat)
        {
            // RANG BUOC DA KIEM CHUNG, PHAI GIU: §2.4 T9 chi dinh nghia DAT tu (1|5, 10).
            // O (2|3|7, 10) nguoi thuc hien vua bao cao mot trang thai CHUA hoan thanh; neu cho
            // DAT se sinh cap (2|3|7, 11) — khong phai diem cuoi §2.6 ma moi hanh dong tiep theo
            // deu tat (nhiem vu chet). Bat buoc chon CHUA_DAT o cac cap do.
            if (!TrangThaiNv.DaHoanThanh.Contains(nv.TrangThai))
            {
                return Result<DmNhiemVuChiTiet>.ThatBai(
                    "Chỉ nghiệm thu ĐẠT khi người thực hiện báo cáo Hoàn thành (1) hoặc " +
                    "Hoàn thành - Sau hạn (5). Hãy chọn \"Chưa đạt\" để yêu cầu bổ sung.",
                    MaLoiChung.SaiTrangThai);
            }

            // §5.5 E1 — he so chat luong chi ghi khi DAT, mien 1..6.
            if (payload.HsChatLuong.HasValue)
            {
                int hs = payload.HsChatLuong.Value;
                if (hs < GioiHan.HsChatLuongMin || hs > GioiHan.HsChatLuongMax)
                {
                    return Result<DmNhiemVuChiTiet>.ThatBai(
                        $"Hệ số chất lượng phải nằm trong khoảng {GioiHan.HsChatLuongMin} - " +
                        $"{GioiHan.HsChatLuongMax}.",
                        MaLoiChung.DuLieuKhongHopLe);
                }
                nv.HsChatLuong = hs;
            }

            // §2.4 T9: (1|5, 10) -> (GIU NGUYEN truc A, 11) => DIEM CUOI §2.6.
            nv.TrangThaiDvXuly = TrangThaiPh.DaXacNhan;
            nv.NgayHoanThanhThucTe = NgayUtil.SangMoc(ctx.HomNay);
        }
        else
        {
            // §2.4 T10: truc B := 12; truc A chi doi khi dang ∈ {1,5}
            // (con han -> 2, qua han -> 7). Neu dang la 2/3/7 thi GIU NGUYEN.
            nv.TrangThaiDvXuly = TrangThaiPh.TuChoi;
            if (TrangThaiNv.DaHoanThanh.Contains(nv.TrangThai))
            {
                nv.TrangThai = HanUtil.TrangThaiTheoHan(nv.HanXuLyTh, ctx.HomNay);
            }
        }

        nv.PhanHoi = SachHoacNull(payload.PhanHoi);
        DanhDauSua(nv, ctx);

        GhiXuLy(ctx, nv, userId, LoaiXuLy.NghiemThu,
            noiDung: Sach(payload.PhanHoi),
            mucDoHt: nv.MucDoHt,
            trangThai: nv.TrangThai,
            trangThaiXuLy: null,
            trangThaiDvXuly: nv.TrangThaiDvXuly);

        return Result<DmNhiemVuChiTiet>.Ok(nv);
    }

    // =====================================================================
    // §2.4 T11 — XIN GIA HAN  (§1.3, §5.6 F1)
    // =====================================================================

    /// <inheritdoc />
    public Result<DmNhiemVuChiTiet> XinGiaHan(
        DmNhiemVuChiTiet nv, Guid userId, GiaHanRequest payload, NguCanh ctx)
    {
        // §6.2 dong 15 — CHUTRI VA trangthai ∈ {2,3,7} VA trangthaixulygiahan ≠ 10
        // VA solangiahan < 2.
        var chan = KiemTruoc(nv, userId, ctx, q => q.XinGiaHan, "xin gia hạn");
        if (chan is not null) return chan;

        if (payload is null)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Thiếu dữ liệu đề xuất gia hạn.", MaLoiChung.DuLieuKhongHopLe);
        }

        if (payload.HanXuLyDeXuat == default)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Vui lòng chọn thời gian đề xuất gia hạn.", MaLoiChung.DuLieuKhongHopLe);
        }

        // §3.3 M10 — `min` cua o ngay = han hien tai: han de xuat phai MUON HON han hien tai.
        var hanCu = nv.HanXuLyTh;
        if (hanCu.HasValue && payload.HanXuLyDeXuat <= hanCu.Value)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Thời gian đề xuất gia hạn phải muộn hơn thời hạn hiện tại " +
                $"({NgayUtil.DinhDang(hanCu)}).",
                MaLoiChung.ViPhamRangBuoc);
        }

        var loi = KiemNoiDung(payload.NoiDung, false, "lý do gia hạn", ctx);
        if (loi is not null) return Result<DmNhiemVuChiTiet>.ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);

        var deXuat = new GiaHanNhiemVu
        {
            IdCtnv = nv.Id,
            NoiDung = SachHoacNull(payload.NoiDung),
            HanXuLyDeXuat = payload.HanXuLyDeXuat,
            HanXuLyThCu = hanCu,
            // MO RONG ngoai §4.5: nho lai truc A truoc khi chuyen 13, de khoi phuc CHINH XAC
            // o T12/T13 (dac biet khi truoc do la 3 — chua tiep nhan).
            TrangThaiCu = nv.TrangThai,
            TrangThai = TrangThaiGiaHan.ChoDuyet,
            PhanHoi = null,
            UserIdDeXuat = userId,
            UserIdDuyet = null,
            CreateDate = NgayUtil.SangMoc(ctx.HomNay),
            NgayDuyet = null
        };
        ctx.KetXuat.GiaHanMoi.Add(deXuat);

        // §2.4 T11: trangthai := 13, trangthaixulygiahan := 10. Truc B GIU NGUYEN.
        nv.TrangThai = TrangThaiNv.GiaHan;
        nv.TrangThaiXuLyGiaHan = TrangThaiGiaHan.ChoDuyet;
        DanhDauSua(nv, ctx);

        GhiXuLy(ctx, nv, userId, LoaiXuLy.GiaHan,
            noiDung: deXuat.NoiDung
                     ?? $"Đề xuất gia hạn đến {NgayUtil.DinhDang(payload.HanXuLyDeXuat)}.",
            mucDoHt: nv.MucDoHt,
            trangThai: TrangThaiNv.GiaHan,
            trangThaiXuLy: null,
            trangThaiDvXuly: nv.TrangThaiDvXuly);

        return Result<DmNhiemVuChiTiet>.Ok(nv);
    }

    // =====================================================================
    // §2.4 T12/T13 — DUYET / TU CHOI GIA HAN  (§1.3, §5.6 F2)
    // =====================================================================

    /// <inheritdoc />
    public Result<DmNhiemVuChiTiet> DuyetGiaHan(
        DmNhiemVuChiTiet nv, Guid userId, DuyetGiaHanRequest payload, NguCanh ctx)
    {
        // §6.2 dong 16 — trangthaixulygiahan = 10 VA nguoi giao (hoac quan tri),
        // VA trangthai ≠ 97 (chan hoi sinh DIEM CUOI §2.6) — da cai trong QuyenService.
        var chan = KiemTruoc(nv, userId, ctx, q => q.DuyetGiaHan, "duyệt gia hạn");
        if (chan is not null) return chan;

        if (payload is null)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Thiếu dữ liệu duyệt gia hạn.", MaLoiChung.DuLieuKhongHopLe);
        }

        var ketQua = ChuanHoaMa(payload.KetQua);
        if (!KetQuaDuyetGiaHan.HopLe(ketQua))
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Kết quả duyệt gia hạn phải là \"DUYET\" hoặc \"TU_CHOI\".",
                MaLoiChung.DuLieuKhongHopLe);
        }

        var loi = KiemNoiDung(payload.PhanHoi, false, "ý kiến của người duyệt", ctx);
        if (loi is not null) return Result<DmNhiemVuChiTiet>.ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);

        // De xuat dang treo PHAI duoc tang tren nap san vao ngu canh (may trang thai khong truy CSDL).
        var deXuat = ctx.GiaHanChoDuyet;
        bool hopLe = deXuat is not null
                     && deXuat.IdCtnv == nv.Id
                     && deXuat.TrangThai == TrangThaiGiaHan.ChoDuyet
                     && (!payload.IdGiaHan.HasValue || deXuat.Id == payload.IdGiaHan.Value);
        if (deXuat is null || !hopLe)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Không tìm thấy yêu cầu gia hạn đang chờ duyệt của nhiệm vụ này.",
                MaLoiChung.KhongTimThay);
        }

        deXuat.PhanHoi = SachHoacNull(payload.PhanHoi);
        deXuat.UserIdDuyet = userId;
        deXuat.NgayDuyet = NgayUtil.SangMoc(ctx.HomNay);

        if (ketQua == KetQuaDuyetGiaHan.Duyet)
        {
            // §2.4 T12 / §5.6 F2 (dac ta MOI: he goc khong gan lai hanxulyth o FE).
            deXuat.TrangThai = TrangThaiGiaHan.DaDuyet;
            nv.TrangThaiXuLyGiaHan = TrangThaiGiaHan.DaDuyet;
            nv.SoLanGiaHan += 1;                      // §2.3 — chi +1 khi DUYET
            nv.HanXuLyTh = deXuat.HanXuLyDeXuat;
        }
        else
        {
            // §2.4 T13 — han GIU NGUYEN.
            deXuat.TrangThai = TrangThaiGiaHan.TuChoi;
            nv.TrangThaiXuLyGiaHan = TrangThaiGiaHan.TuChoi;
        }
        ctx.KetXuat.GiaHanCapNhat.Add(deXuat);

        // Khoi phuc truc A tu 13 ve gia tri truoc khi xin gia han, co ap quy tac han §2.5.
        nv.TrangThai = TrangThaiSauGiaHan(deXuat.TrangThaiCu, nv.HanXuLyTh, ctx.HomNay);
        DanhDauSua(nv, ctx);

        GhiXuLy(ctx, nv, userId, LoaiXuLy.DuyetGiaHan,
            noiDung: deXuat.PhanHoi
                     ?? (ketQua == KetQuaDuyetGiaHan.Duyet
                         ? $"Đồng ý gia hạn đến {NgayUtil.DinhDang(deXuat.HanXuLyDeXuat)}."
                         : "Không đồng ý gia hạn."),
            mucDoHt: nv.MucDoHt,
            trangThai: nv.TrangThai,
            trangThaiXuLy: null,
            trangThaiDvXuly: nv.TrangThaiDvXuly);

        return Result<DmNhiemVuChiTiet>.Ok(nv);
    }

    /// <summary>
    /// §2.5 — tinh lai truc A sau khi ket thuc quy trinh gia han (T12 hoac T13).
    ///
    /// NGOAI LE CO Y (ghi ro trong tai lieu ban giao): §5.6 F2 viet "trangthai ve 2 neu han moi
    /// con hieu luc", nhung neu ap cho ca truong hop TRUOC DO la 3 (chua tiep nhan) thi nhiem vu
    /// se nhay sang "Dang trien khai" MA KHONG qua buoc Tiep nhan (§2.4 T2) va thieu
    /// <c>ngaytiepnhan</c>. Vi vay: chua tiep nhan thi giu 3 neu con han, chuyen 7 neu qua han.
    /// </summary>
    /// <param name="trangThaiCu">Truc A truoc khi chuyen sang 13; <c>null</c> khi khong co thong tin.</param>
    /// <param name="hanXuLyTh">Han hien hanh SAU khi da cap nhat (neu duyet).</param>
    /// <param name="homNay">Moc so han.</param>
    public static int TrangThaiSauGiaHan(int? trangThaiCu, DateOnly? hanXuLyTh, DateOnly homNay)
    {
        int cu = trangThaiCu ?? TrangThaiNv.DangTrienKhai;
        if (cu == TrangThaiNv.GiaHan) cu = TrangThaiNv.DangTrienKhai;   // du phong: 13 -> 2

        bool quaHan = NgayUtil.QuaHan(hanXuLyTh, homNay);

        if (cu == TrangThaiNv.ChuaTrienKhai)
        {
            // §2.5 quy tac 3 -> 7 khi qua han; con lai giu 3 (chua qua buoc Tiep nhan).
            return quaHan ? TrangThaiNv.DangTrienKhaiQuaHan : TrangThaiNv.ChuaTrienKhai;
        }

        // Con lai (2 hoac 7): 7 -> 2 khi han moi con hieu luc, 2 -> 7 khi da qua han.
        return quaHan ? TrangThaiNv.DangTrienKhaiQuaHan : TrangThaiNv.DangTrienKhai;
    }

    // =====================================================================
    // §2.4 T14 — THU HOI NHIEM VU  (§1.3, §5.3 C6)
    // =====================================================================

    /// <inheritdoc />
    public Result<DmNhiemVuChiTiet> ThuHoiNhiemVu(
        DmNhiemVuChiTiet nv, Guid userId, ThuHoiNhiemVuRequest? payload, NguCanh ctx)
    {
        // §6.2 dong 7 — userIdGiaoViec = toi VA trangthai ∉ {1,5,97}.
        var chan = KiemTruoc(nv, userId, ctx, q => q.ThuHoiNhiemVu, "thu hồi nhiệm vụ");
        if (chan is not null) return chan;

        var loi = KiemNoiDung(payload?.LyDo, false, "lý do thu hồi", ctx);
        if (loi is not null) return Result<DmNhiemVuChiTiet>.ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);

        // §2.4 T14: -> (97, null), DIEM CUOI §2.6.
        nv.TrangThai = TrangThaiNv.DaThuHoi;
        nv.TrangThaiDvXuly = null;

        // RANG BUOC DA KIEM CHUNG, PHAI GIU: 97 la DIEM CUOI, khong co duong quay lai (§1.3).
        // Neu con de xuat gia han dang treo (trangThai = 10) thi PHAI dong lai; neu khong,
        // nguoi giao van bam duoc "Duyet gia han" va T12 se gan lai truc A => hoi sinh diem cuoi.
        var deXuat = ctx.GiaHanChoDuyet;
        if (deXuat is not null
            && deXuat.IdCtnv == nv.Id
            && deXuat.TrangThai == TrangThaiGiaHan.ChoDuyet)
        {
            deXuat.TrangThai = TrangThaiGiaHan.TuChoi;
            deXuat.PhanHoi = "Nhiệm vụ đã bị thu hồi, đề xuất gia hạn không còn hiệu lực.";
            deXuat.UserIdDuyet = userId;
            deXuat.NgayDuyet = NgayUtil.SangMoc(ctx.HomNay);
            ctx.KetXuat.GiaHanCapNhat.Add(deXuat);
        }
        // Dong truc C ngay ca khi tang tren khong nap duoc ban ghi de xuat (phong thu).
        if (nv.TrangThaiXuLyGiaHan == TrangThaiGiaHan.ChoDuyet)
        {
            nv.TrangThaiXuLyGiaHan = TrangThaiGiaHan.TuChoi;
        }

        DanhDauSua(nv, ctx);

        GhiXuLy(ctx, nv, userId, LoaiXuLy.ThuHoiNhiemVu,
            noiDung: SachHoacNull(payload?.LyDo) ?? "Người giao thu hồi nhiệm vụ.",
            mucDoHt: nv.MucDoHt,
            trangThai: TrangThaiNv.DaThuHoi,
            trangThaiXuLy: null,
            trangThaiDvXuly: null);

        return Result<DmNhiemVuChiTiet>.Ok(nv);
    }

    // =====================================================================
    // §1.3 — NHAC VIEC
    // =====================================================================

    /// <inheritdoc />
    public Result<DmNhiemVuChiTiet> NhacViec(
        DmNhiemVuChiTiet nv, Guid userId, NhacViecRequest payload, NguCanh ctx)
    {
        // §6.2 dong 17 — userIdGiaoViec = toi VA trangthai ∉ {1,5,97}.
        var chan = KiemTruoc(nv, userId, ctx, q => q.NhacViec, "nhắc việc");
        if (chan is not null) return chan;

        if (payload is null)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Thiếu nội dung nhắc việc.", MaLoiChung.DuLieuKhongHopLe);
        }

        var loi = KiemNoiDung(payload.NoiDung, true, "nội dung nhắc việc", ctx);
        if (loi is not null) return Result<DmNhiemVuChiTiet>.ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);

        // §1.3 + §10.8 — he goc KHONG doi trang thai, khong dinh kem tep, khong dem so lan nhac.
        // Cung KHONG cham `updatedate` vi ban than nhiem vu khong doi.
        GhiXuLy(ctx, nv, userId, LoaiXuLy.NhacViec,
            noiDung: Sach(payload.NoiDung),
            mucDoHt: nv.MucDoHt,
            trangThai: nv.TrangThai,
            trangThaiXuLy: null,
            trangThaiDvXuly: nv.TrangThaiDvXuly);

        return Result<DmNhiemVuChiTiet>.Ok(nv);
    }

    // =====================================================================
    // §5.3 C5 / §6.2 dong 6 — SUA NHIEM VU DA GIAO
    // =====================================================================

    /// <inheritdoc />
    public Result<DmNhiemVuChiTiet> SuaNhiemVu(
        DmNhiemVuChiTiet nv, Guid userId, SuaNhiemVuRequest payload, NguCanh ctx)
    {
        // §6.2 dong 6 — userIdGiaoViec = toi VA trangthai = 3.
        var chan = KiemTruoc(nv, userId, ctx, q => q.SuaNhiemVu, "sửa nhiệm vụ");
        if (chan is not null) return chan;

        if (payload is null)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Thiếu dữ liệu sửa nhiệm vụ.", MaLoiChung.DuLieuKhongHopLe);
        }

        // §4.2 — noi dung BAT BUOC, <= 2000 ky tu.
        var loi = KiemNoiDung(payload.NoiDung, true, "nội dung nhiệm vụ", ctx);
        if (loi is not null) return Result<DmNhiemVuChiTiet>.ThatBai(loi, MaLoiChung.DuLieuKhongHopLe);

        // §7.4 muc 4 — do khan chi nhan TRONGTAM / THUONGXUYEN / DOTXUAT.
        var doKhan = Sach(payload.DoKhan);
        if (doKhan.Length == 0) doKhan = DoKhan.ThuongXuyen;
        if (!DoKhan.HopLe(doKhan))
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Độ khẩn không hợp lệ. Chỉ nhận: TRONGTAM, THUONGXUYEN, DOTXUAT.",
                MaLoiChung.DuLieuKhongHopLe);
        }

        // §4.2 — `songayhxlth` (so ngay xu ly) neu co phai la so duong.
        if (payload.SoNgayHxlTh.HasValue && payload.SoNgayHxlTh.Value <= 0)
        {
            return Result<DmNhiemVuChiTiet>.ThatBai(
                "Số ngày xử lý phải lớn hơn 0.", MaLoiChung.DuLieuKhongHopLe);
        }

        nv.NoiDung = Sach(payload.NoiDung);
        nv.LinhVuc = SachHoacNull(payload.LinhVuc);
        nv.DoKhan = doKhan;
        nv.SoNgayHxlTh = payload.SoNgayHxlTh;
        // Uu tien han cu the; neu chi co so ngay thi tinh tu ngay giao (§4.2).
        nv.HanXuLyTh = payload.HanXuLyTh
                       ?? NgayUtil.TinhHanTuSoNgay(NgayUtil.SangNgay(nv.NgayGiao), payload.SoNgayHxlTh);
        nv.HanXuLyPh = payload.HanXuLyPh;
        DanhDauSua(nv, ctx);

        // GHI CHU: KHONG sinh ban ghi XULY_NHIEMVU o day. §4.4 chi dinh nghia 11 ma `loai`
        // (§ hang so LoaiXuLy) va KHONG co ma nao cho "sua nhiem vu"; tu bia them ma se pha
        // rang buoc §7.4. Neu sau nay can vet sua, hay bo sung ma moi vao §4.4 truoc.
        return Result<DmNhiemVuChiTiet>.Ok(nv);
    }

    // =====================================================================
    // §2.5 / §5.9 I5 — JOB QUA HAN
    // =====================================================================

    /// <inheritdoc />
    public bool ApQuyTacQuaHan(DmNhiemVuChiTiet nv, DateOnly homNay)
    {
        ArgumentNullException.ThrowIfNull(nv);

        // §2.5 — chi ap cho truc A = 2 (Dang trien khai) va 3 (Chua trien khai).
        if (nv.TrangThai != TrangThaiNv.DangTrienKhai && nv.TrangThai != TrangThaiNv.ChuaTrienKhai)
        {
            return false;
        }
        // hanxulyth < hom nay. Khong co han => KHONG bao gio qua han.
        if (!NgayUtil.QuaHan(nv.HanXuLyTh, homNay)) return false;

        nv.TrangThai = TrangThaiNv.DangTrienKhaiQuaHan;
        nv.UpdateDate = NgayUtil.SangMoc(homNay);
        return true;
    }
}
