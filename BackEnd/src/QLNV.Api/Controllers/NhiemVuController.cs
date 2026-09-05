using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QLNV.Api.Common;
using QLNV.Api.Services;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;
using MaDoKhan = QLNV.Core.Constants.DoKhan;
using MaVaiTro = QLNV.Core.Constants.VaiTro;

namespace QLNV.Api.Controllers;

/// <summary>
/// §5.3 C1-C7 + §5.4 D1-D7 — tao/giao nhiem vu va luong thuc hien.
///
/// NGUYEN TAC: controller KHONG tu viet lai logic chuyen trang thai. Moi chuyen T2..T14
/// deu goi <see cref="INhiemVuStateMachine"/>; moi quyet dinh quyen deu goi
/// <see cref="IQuyenService"/> (qua <see cref="TroGiupNghiepVu.NapAsync"/>).
///
/// KIEM QUYEN 2 LOP (§6.4) tren MOI endpoint:
///   LOP 1 — vai tro: BatBuocBenGiao / BatBuocBenLam / BatBuocDangNhap.
///   LOP 2 — du lieu + trang thai: doc co tuong ung trong <see cref="QuyenNhiemVu"/>, sai thi 403.
/// </summary>
[Route("api/v1")]
public sealed class NhiemVuController : QlnvControllerBase
{
    private readonly TroGiupNghiepVu _troGiup;
    private readonly INhiemVuStateMachine _mayTrangThai;
    private readonly ILogger<NhiemVuController> _log;

    public NhiemVuController(
        IHienTai hienTai,
        TroGiupNghiepVu troGiup,
        INhiemVuStateMachine mayTrangThai,
        ILogger<NhiemVuController> log)
        : base(hienTai)
    {
        _troGiup = troGiup;
        _mayTrangThai = mayTrangThai;
        _log = log;
    }

    private DbContext Db => _troGiup.Db;

    // =================================================================
    // C1 — POST /api/v1/van-ban/{idvb}/nhiem-vu
    // =================================================================

    /// <summary>
    /// §5.3 C1 / §1.2 buoc 2 — tao va giao NHIEU nhiem vu trong 1 lan luu (M05).
    /// SERVER dat <c>trangthai = 3</c> va <c>trangthaiDvXuly = null</c> (§1.2 buoc 2.4, §10.4)
    /// — khong nhan gia tri trang thai tu FE.
    /// </summary>
    [HttpPost("van-ban/{idvb:guid}/nhiem-vu")]
    public async Task<IActionResult> TaoVaGiao(Guid idvb, [FromBody] TaoNhiemVuRequest req, CancellationToken ct)
    {
        // §6.4 LOP 1 — §6.2 dong 4: chi QUAN_TRI / NGUOI_GIAO.
        var chan = BatBuocBenGiao("tạo và giao nhiệm vụ");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        var nguoi = await _troGiup.TimNguoiDungAsync(UserId, ct);
        if (nguoi is null || !nguoi.DangHoatDong) return Loi403("Tài khoản của bạn không tồn tại hoặc đã bị khoá.");

        var vb = await Db.Set<DmVanBan>().FirstOrDefaultAsync(v => v.Id == idvb, ct);
        if (vb is null) return Loi404("Không tìm thấy văn bản chỉ đạo.");

        if (req.Items.Count == 0) return Loi400("Chưa có dòng nhiệm vụ nào để lưu.");

        // -----------------------------------------------------------
        // Vong 1 — kiem tra lai TOAN BO rang buoc o phia server (§6.4).
        // -----------------------------------------------------------
        var dongDaChuan = new List<DongNhiemVu>();
        for (var i = 0; i < req.Items.Count; i++)
        {
            var it = req.Items[i];
            var stt = i + 1;

            if (string.IsNullOrWhiteSpace(it.NoiDung))
            {
                return Loi400($"Dòng {stt}: nội dung nhiệm vụ là bắt buộc.");
            }

            var noiDung = it.NoiDung.Trim();
            if (noiDung.Length > GioiHan.DoDaiNoiDung)
            {
                return Loi400($"Dòng {stt}: nội dung nhiệm vụ không được vượt quá {GioiHan.DoDaiNoiDung} ký tự.");
            }

            if (!MaDoKhan.HopLe(it.DoKhan))
            {
                return Loi400($"Dòng {stt}: độ khẩn không hợp lệ (chỉ nhận TRONGTAM, THUONGXUYEN, DOTXUAT).");
            }

            // §4.3 — moi dong PHAI co it nhat 1 chu tri.
            var chuTri = it.ChuTri.Where(x => x != Guid.Empty).Distinct().ToList();
            if (chuTri.Count == 0)
            {
                return Loi400($"Dòng {stt}: phải có ít nhất 1 người/đơn vị chủ trì.");
            }

            // §4.3 — mot nguoi KHONG duoc vua CHUTRI vua PHOIHOP tren cung 1 nhiem vu.
            var phoiHop = it.PhoiHop.Where(x => x != Guid.Empty).Distinct().ToList();
            var trungVai = chuTri.Intersect(phoiHop).ToList();
            if (trungVai.Count > 0)
            {
                return Loi400($"Dòng {stt}: một người không được vừa là chủ trì vừa là phối hợp.");
            }

            // §4.2 — linh vuc BAT BUOC (dau vao cua AI goi y). Thieu thi lay theo van ban.
            var linhVuc = string.IsNullOrWhiteSpace(it.LinhVuc) ? vb.LinhVuc : it.LinhVuc.Trim();
            if (string.IsNullOrWhiteSpace(linhVuc))
            {
                return Loi400($"Dòng {stt}: lĩnh vực là bắt buộc (văn bản chỉ đạo cũng chưa có lĩnh vực).");
            }

            if (it.SoNgayHxlTh.HasValue && it.SoNgayHxlTh.Value < 0)
            {
                return Loi400($"Dòng {stt}: số ngày thực hiện không được là số âm.");
            }

            dongDaChuan.Add(new DongNhiemVu(stt, noiDung, linhVuc, it, chuTri, phoiHop));
        }

        // Kiem tra nguoi duoc giao co that va con hoat dong (§6.4: khong tin FE).
        var tatCaUser = dongDaChuan
            .SelectMany(d => d.ChuTri.Concat(d.PhoiHop))
            .Distinct()
            .ToList();

        var dsUser = await Db.Set<SysUser>().Where(u => tatCaUser.Contains(u.Id)).ToListAsync(ct);
        if (dsUser.Count != tatCaUser.Count)
        {
            return Loi400("Một số người được giao không tồn tại trong hệ thống.");
        }

        var biKhoa = dsUser.Where(u => !u.DangHoatDong).Select(u => u.FullName).ToList();
        if (biKhoa.Count > 0)
        {
            return Loi400("Không thể giao nhiệm vụ cho tài khoản đã bị khoá: " + string.Join(", ", biKhoa) + ".");
        }

        var mapUser = dsUser.ToDictionary(u => u.Id);

        // -----------------------------------------------------------
        // §1.2 buoc 4 — do trung noi dung, FAIL-OPEN.
        // -----------------------------------------------------------
        if (!req.BoQuaTrungNoiDung)
        {
            var dongDoTrung = dongDaChuan
                .Select(d => new KiemTraTrungItem { Tt = d.Stt, NoiDung = d.NoiDung, DsUserChuTri = d.ChuTri })
                .ToList();

            var trung = await DoTrungFailOpenAsync(dongDoTrung, ct);
            if (trung.Count > 0)
            {
                // Chua luu gi ca — tra ve de FE hoi lai nguoi dung (§1.2 buoc 4).
                return Ok(new TaoNhiemVuResponse { SoLuong = 0, TrungNoiDung = trung });
            }
        }

        // -----------------------------------------------------------
        // Vong 2 — tao ban ghi.
        // -----------------------------------------------------------
        var bayGio = NgayUtil.BayGio();
        var ngayGiao = NgayUtil.SangNgay(bayGio);
        var ids = new List<Guid>();

        foreach (var d in dongDaChuan)
        {
            var han = d.Item.HanXuLyTh ?? NgayUtil.TinhHanTuSoNgay(ngayGiao, d.Item.SoNgayHxlTh);

            var nv = new DmNhiemVuChiTiet
            {
                Id = Guid.NewGuid(),
                IdVb = vb.Id,
                NoiDung = d.NoiDung,
                LinhVuc = d.LinhVuc,
                DoKhan = d.Item.DoKhan,
                HanXuLyTh = han,
                SoNgayHxlTh = d.Item.SoNgayHxlTh,
                HanXuLyPh = d.Item.HanXuLyPh,
                NgayGiao = bayGio,
                NgayTiepNhan = null,
                NgayHoanThanhThucTe = null,

                // §1.2 buoc 2.4 / §10.4 — TRANG THAI KHOI TAO DO SERVER DAT.
                TrangThai = TrangThaiNv.ChuaTrienKhai,   // 3
                TrangThaiDvXuly = null,                  // truc B chua co bao cao
                TrangThaiXuLyGiaHan = null,              // truc C chua xin gia han
                SoLanGiaHan = 0,
                MucDoHt = null,

                UserIdGiaoViec = nguoi.Id,
                UserIdCreate = nguoi.Id,
                UnitCode = nguoi.UnitCode,
                CreateDate = bayGio,
                UpdateDate = bayGio,
                AiGoiYId = d.Item.AiGoiYId
            };

            Db.Set<DmNhiemVuChiTiet>().Add(nv);

            foreach (var uid in d.ChuTri)
            {
                Db.Set<NhiemVuPhanCong>().Add(TaoPhanCong(nv.Id, uid, VaiTroPhanCong.ChuTri, nguoi.Id, mapUser, bayGio));
            }

            foreach (var uid in d.PhoiHop)
            {
                Db.Set<NhiemVuPhanCong>().Add(TaoPhanCong(nv.Id, uid, VaiTroPhanCong.PhoiHop, nguoi.Id, mapUser, bayGio));
            }

            var ganFile = await _troGiup.GanFileAsync(d.Item.FileIds, nv.Id, LoaiBanGhiFile.NhiemVu, nguoi.Id, ct);
            var loiFile = NeuLoi(ganFile);
            if (loiFile is not null) return loiFile;

            // §4.2 ai_goiy_id / §5.8 H2 — noi nhat ky goi y voi nhiem vu vua tao.
            if (d.Item.AiGoiYId.HasValue)
            {
                var aiGoiYId = d.Item.AiGoiYId.Value;
                var log = await Db.Set<AiGoiYLog>().FirstOrDefaultAsync(x => x.Id == aiGoiYId, ct);
                if (log is not null && log.IdNvChiTiet is null) log.IdNvChiTiet = nv.Id;
            }

            ids.Add(nv.Id);
        }

        await Db.SaveChangesAsync(ct);

        return Ok(new TaoNhiemVuResponse { Ids = ids, SoLuong = ids.Count });
    }

    private static NhiemVuPhanCong TaoPhanCong(
        Guid idNv, Guid userId, string vaiTro, Guid nguoiTao,
        IReadOnlyDictionary<Guid, SysUser> mapUser, DateTime bayGio)
    {
        var unitCode = mapUser.TryGetValue(userId, out var u) ? u.UnitCode : string.Empty;
        return new NhiemVuPhanCong
        {
            Id = Guid.NewGuid(),
            IdNvChiTiet = idNv,
            UserId = userId,
            UnitCode = unitCode,
            VaiTro = vaiTro,
            UserIdCreate = nguoiTao,
            TrangThai = TrangThaiPhanCong.ConHieuLuc,
            CreateDate = bayGio
        };
    }

    /// <summary>Mot dong nhiem vu da chuan hoa va da qua kiem tra o vong 1.</summary>
    private sealed record DongNhiemVu(
        int Stt,
        string NoiDung,
        string LinhVuc,
        TaoNhiemVuItem Item,
        List<Guid> ChuTri,
        List<Guid> PhoiHop);

    // =================================================================
    // C2 — POST /api/v1/nhiem-vu/kiem-tra-trung
    // =================================================================

    /// <summary>
    /// §5.3 C2 / §1.2 buoc 4 — do trung noi dung truoc khi luu. <b>FAIL-OPEN</b>:
    /// loi ky thuat KHONG chan viec luu, chi ghi log va tra ve danh sach rong.
    /// </summary>
    [HttpPost("nhiem-vu/kiem-tra-trung")]
    public async Task<IActionResult> KiemTraTrung([FromBody] KiemTraTrungRequest req, CancellationToken ct)
    {
        var chan = BatBuocBenGiao("dò trùng nội dung nhiệm vụ");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        var kq = await DoTrungFailOpenAsync(req.Items, ct);
        return Ok(kq);
    }

    /// <summary>Do trung noi dung. Nuot loi KY THUAT co chu dich (fail-open §1.2 buoc 4) va ghi log.</summary>
    private async Task<List<TrungNoiDungDto>> DoTrungFailOpenAsync(List<KiemTraTrungItem> items, CancellationToken ct)
    {
        try
        {
            return await DoTrungAsync(items, ct);
        }
        catch (OperationCanceledException)
        {
            throw; // huy request thi de nguyen cho tang tren xu ly
        }
        catch (Exception ex)
        {
            // FAIL-OPEN: khong chan nguoi dung luu nhiem vu chi vi buoc do trung hong.
            _log.LogWarning(ex, "Do trung noi dung nhiem vu that bai — bo qua theo quy tac fail-open (§1.2 buoc 4).");
            return new List<TrungNoiDungDto>();
        }
    }

    private async Task<List<TrungNoiDungDto>> DoTrungAsync(List<KiemTraTrungItem> items, CancellationToken ct)
    {
        var kq = new List<TrungNoiDungDto>();
        if (items.Count == 0) return kq;

        var userIds = items.SelectMany(i => i.DsUserChuTri).Where(x => x != Guid.Empty).Distinct().ToList();
        if (userIds.Count == 0) return kq;

        // Chi so sanh voi nhiem vu CON HIEU LUC: bo qua diem cuoi §2.6 (1, 5 da nghiem thu / 97 da thu hoi).
        var ungVien = await Db.Set<NhiemVuPhanCong>()
            .AsNoTracking()
            .Where(p => p.VaiTro == VaiTroPhanCong.ChuTri
                        && p.TrangThai == TrangThaiPhanCong.ConHieuLuc
                        && userIds.Contains(p.UserId))
            .Join(Db.Set<DmNhiemVuChiTiet>().AsNoTracking(),
                p => p.IdNvChiTiet,
                n => n.Id,
                (p, n) => new { p.UserId, n.Id, n.NoiDung, n.TrangThai })
            .Where(x => x.TrangThai != TrangThaiNv.HoanThanh
                        && x.TrangThai != TrangThaiNv.HoanThanhSauHan
                        && x.TrangThai != TrangThaiNv.DaThuHoi)
            .ToListAsync(ct);

        if (ungVien.Count == 0) return kq;

        foreach (var it in items)
        {
            var chuan = ChuanHoaNoiDung(it.NoiDung);
            if (chuan.Length == 0) continue;

            var dsUser = it.DsUserChuTri.Where(x => x != Guid.Empty).ToHashSet();
            if (dsUser.Count == 0) continue;

            var trung = ungVien.FirstOrDefault(x => dsUser.Contains(x.UserId) && ChuanHoaNoiDung(x.NoiDung) == chuan);
            if (trung is null) continue;

            kq.Add(new TrungNoiDungDto
            {
                Tt = it.Tt,
                NoiDung = it.NoiDung,
                IdNhiemVuTrung = trung.Id,
                NoiDungTrung = trung.NoiDung,
                ThongBao = "Nội dung nhiệm vụ này đã được giao cho cùng người chủ trì và vẫn đang còn hiệu lực."
            });
        }

        return kq;
    }

    /// <summary>Chuan hoa noi dung de so sanh trung: bo dau/cuoi, ha chu thuong, gop khoang trang.</summary>
    private static string ChuanHoaNoiDung(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        var nguon = s.Trim().ToLowerInvariant();
        var sb = new StringBuilder(nguon.Length);
        var truocLaTrang = false;

        foreach (var c in nguon)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!truocLaTrang) sb.Append(' ');
                truocLaTrang = true;
            }
            else
            {
                sb.Append(c);
                truocLaTrang = false;
            }
        }

        return sb.ToString().Trim();
    }

    // =================================================================
    // C3 — GET /api/v1/nhiem-vu
    // =================================================================

    /// <summary>§5.3 C3 — danh sach nhiem vu theo vai (<c>TOI_GIAO</c> / <c>TOI_LAM</c>), M08.</summary>
    [HttpGet("nhiem-vu")]
    public async Task<IActionResult> DanhSach([FromQuery] NhiemVuLocRequest req, CancellationToken ct)
    {
        var chan = BatBuocDangNhap();
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        var nguoi = await _troGiup.TimNguoiDungAsync(UserId, ct);
        if (nguoi is null || !nguoi.DangHoatDong) return Loi403("Tài khoản của bạn không tồn tại hoặc đã bị khoá.");

        if (req.VaiTro is not null && !BoLocVaiTro.HopLe(req.VaiTro))
        {
            return Loi400("Tham số vaiTro chỉ nhận giá trị TOI_GIAO hoặc TOI_LAM.");
        }

        // §6.4 LOP 1 — nguoi thuc hien KHONG co pham vi "toi giao".
        var vaiTroLoc = req.VaiTro;
        if (!MaVaiTro.LaBenGiao(nguoi.VaiTro)) vaiTroLoc = BoLocVaiTro.ToiLam;

        var homNay = HomNay;
        var (trang, kichThuoc) = ChuanHoaTrang(req.Page, req.Size);

        var q = Db.Set<DmNhiemVuChiTiet>().AsNoTracking().AsQueryable();

        var idToiLam = Db.Set<NhiemVuPhanCong>()
            .Where(p => p.UserId == nguoi.Id && p.TrangThai == TrangThaiPhanCong.ConHieuLuc)
            .Select(p => p.IdNvChiTiet);

        var laQuanTri = nguoi.VaiTro == MaVaiTro.QuanTri;

        if (vaiTroLoc == BoLocVaiTro.ToiGiao)
        {
            q = q.Where(n => n.UserIdGiaoViec == nguoi.Id || n.UserIdCreate == nguoi.Id);
        }
        else if (vaiTroLoc == BoLocVaiTro.ToiLam)
        {
            q = q.Where(n => idToiLam.Contains(n.Id));
        }
        else if (!laQuanTri)
        {
            // Khong chi dinh vai: lay ca hai pham vi cua chinh minh (§6.2 dong 18).
            q = q.Where(n => n.UserIdGiaoViec == nguoi.Id
                             || n.UserIdCreate == nguoi.Id
                             || idToiLam.Contains(n.Id));
        }
        // QUAN_TRI khong chi dinh vai => xem toan he thong (§6.2 dong 18/20).

        var dsTrangThai = req.TrangThai ?? new List<int>();
        if (dsTrangThai.Count > 0)
        {
            q = q.Where(n => dsTrangThai.Contains(n.TrangThai));
        }

        var dsTrangThaiDv = req.TrangThaiDvXuly ?? new List<int>();
        if (dsTrangThaiDv.Count > 0)
        {
            q = q.Where(n => n.TrangThaiDvXuly != null && dsTrangThaiDv.Contains(n.TrangThaiDvXuly.Value));
        }

        if (req.QuaHan.HasValue)
        {
            // §2.5 — qua han khi hanxulyth < hom nay. Khong co han => KHONG qua han.
            q = req.QuaHan.Value
                ? q.Where(n => n.HanXuLyTh != null && n.HanXuLyTh < homNay)
                : q.Where(n => n.HanXuLyTh == null || n.HanXuLyTh >= homNay);
        }

        if (req.SapHetHan.HasValue && req.SapHetHan.Value)
        {
            // §3.3 M08 — sap het han = con 0..3 ngay.
            var moc = homNay.AddDays(GioiHan.NguongSapHetHan);
            q = q.Where(n => n.HanXuLyTh != null && n.HanXuLyTh >= homNay && n.HanXuLyTh <= moc);
        }

        if (req.IdVb.HasValue) q = q.Where(n => n.IdVb == req.IdVb.Value);
        if (!string.IsNullOrWhiteSpace(req.LinhVuc)) q = q.Where(n => n.LinhVuc == req.LinhVuc);
        if (!string.IsNullOrWhiteSpace(req.DoKhan)) q = q.Where(n => n.DoKhan == req.DoKhan);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var tu = req.Search.Trim();
            q = q.Where(n => n.NoiDung.Contains(tu));
        }

        var tongSo = await q.CountAsync(ct);
        var ds = await q.OrderByDescending(n => n.CreateDate)
            .Skip((trang - 1) * kichThuoc)
            .Take(kichThuoc)
            .ToListAsync(ct);

        var items = await _troGiup.SangDtoAsync(ds, nguoi, homNay, kemQuyen: true, ct);
        return Ok(new PagedResult<NhiemVuDto>(items, tongSo, trang, kichThuoc));
    }

    // =================================================================
    // C4 — GET /api/v1/nhiem-vu/{id}
    // =================================================================

    /// <summary>§5.3 C4 — chi tiet nhiem vu + phan cong + lich su + tep + danh muc trang thai hop le.</summary>
    [HttpGet("nhiem-vu/{id:guid}")]
    public async Task<IActionResult> ChiTiet(Guid id, CancellationToken ct)
    {
        var chan = BatBuocDangNhap();
        if (chan is not null) return chan;

        var homNay = HomNay;
        var nap = await _troGiup.NapAsync(id, UserId, homNay, ct);
        if (nap is null) return Loi404("Không tìm thấy nhiệm vụ.");

        // §6.4 LOP 2 — §6.2 dong 18.
        if (!nap.Quyen.XemChiTiet) return Loi403("Bạn không có quyền xem nhiệm vụ này.");

        var nvDto = await _troGiup.SangDtoAsync(new[] { nap.NhiemVu }, nap.NguoiThaoTac, homNay, kemQuyen: true, ct);

        var kq = new NhiemVuChiTietDto
        {
            NhiemVu = nvDto[0],
            PhanCong = await _troGiup.PhanCongAsync(id, ct),
            LichSuXuLy = await _troGiup.LichSuXuLyAsync(id, ct),
            LichSuGiaHan = await _troGiup.LichSuGiaHanAsync(id, ct),
            TrangThaiHopLe = await _troGiup.TrangThaiHopLeAsync(nap.NhiemVu, homNay, ct)
        };

        var files = await _troGiup.DocFileAsync(LoaiBanGhiFile.NhiemVu, new[] { id }, ct);
        if (files.TryGetValue(id, out var fs)) kq.Files = fs;

        return Ok(kq);
    }

    // =================================================================
    // C5 — PUT /api/v1/nhiem-vu/{id}
    // =================================================================

    /// <summary>§5.3 C5 / §6.2 dong 6 — sua nhiem vu da giao, CHI khi <c>trangthai = 3</c>.</summary>
    [HttpPut("nhiem-vu/{id:guid}")]
    public async Task<IActionResult> Sua(Guid id, [FromBody] SuaNhiemVuRequest req, CancellationToken ct)
    {
        var chan = BatBuocBenGiao("sửa nhiệm vụ");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        return await ChayAsync(
            id,
            "sửa nhiệm vụ",
            q => q.SuaNhiemVu,
            nap => _mayTrangThai.SuaNhiemVu(nap.NhiemVu, UserId, req, nap.Ctx),
            req.FileIds,
            LoaiBanGhiFile.NhiemVu,
            ganVaoNhiemVu: true,
            ct);
    }

    // =================================================================
    // C6 — POST /api/v1/nhiem-vu/{id}/thu-hoi
    // =================================================================

    /// <summary>§5.3 C6 / §2.4 T14 — nguoi giao thu hoi ca nhiem vu, ve <c>(97, null)</c> (DIEM CUOI §2.6).</summary>
    [HttpPost("nhiem-vu/{id:guid}/thu-hoi")]
    public async Task<IActionResult> ThuHoi(Guid id, [FromBody] ThuHoiNhiemVuRequest? req, CancellationToken ct)
    {
        var chan = BatBuocBenGiao("thu hồi nhiệm vụ");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req ?? new ThuHoiNhiemVuRequest(), ct);
        if (loi is not null) return loi;

        return await ChayAsync(
            id,
            "thu hồi nhiệm vụ",
            q => q.ThuHoiNhiemVu,
            nap => _mayTrangThai.ThuHoiNhiemVu(nap.NhiemVu, UserId, req, nap.Ctx),
            null, null, ganVaoNhiemVu: false, ct);
    }

    // =================================================================
    // C7 — POST /api/v1/nhiem-vu/{id}/thu-hoi-phan-cong
    // =================================================================

    /// <summary>
    /// §5.3 C7 / §6.2 dong 8 — rut phan cong cua mot so nguoi (<c>NHIEMVU_PHANCONG.trangthai := 0</c>).
    /// KHONG doi trang thai nhiem vu (khong phai mot chuyen cua §2.4).
    /// RANG BUOC §4.3: sau khi rut van phai con it nhat 1 CHUTRI con hieu luc.
    /// </summary>
    [HttpPost("nhiem-vu/{id:guid}/thu-hoi-phan-cong")]
    public async Task<IActionResult> ThuHoiPhanCong(Guid id, [FromBody] ThuHoiPhanCongRequest req, CancellationToken ct)
    {
        var chan = BatBuocBenGiao("thu hồi phân công");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        var homNay = HomNay;
        var nap = await _troGiup.NapAsync(id, UserId, homNay, ct);
        if (nap is null) return Loi404("Không tìm thấy nhiệm vụ.");

        // §6.4 LOP 2 — §6.2 dong 8.
        if (!nap.Quyen.ThuHoiPhanCong)
        {
            return Loi403(MoTaTuChoiQuyen("thu hồi phân công", nap));
        }

        var userIds = req.UserIds.Where(x => x != Guid.Empty).Distinct().ToList();
        if (userIds.Count == 0) return Loi400("Chưa chọn người cần thu hồi phân công.");

        var canThuHoi = nap.Ctx.PhanCong
            .Where(p => p.TrangThai == TrangThaiPhanCong.ConHieuLuc && userIds.Contains(p.UserId))
            .ToList();

        if (canThuHoi.Count == 0)
        {
            return Loi409("Những người được chọn không còn phân công hiệu lực trên nhiệm vụ này.",
                MaLoiChung.ViPhamRangBuoc);
        }

        // §4.3 — moi nhiem vu phai co >= 1 CHUTRI con hieu luc.
        var chuTriConLai = nap.Ctx.PhanCong.Count(p =>
            p.TrangThai == TrangThaiPhanCong.ConHieuLuc
            && p.VaiTro == VaiTroPhanCong.ChuTri
            && !userIds.Contains(p.UserId));

        if (chuTriConLai == 0)
        {
            return Loi409(
                "Không thể thu hồi: nhiệm vụ phải còn ít nhất 1 người/đơn vị chủ trì. " +
                "Nếu muốn dừng hẳn, hãy dùng chức năng Thu hồi nhiệm vụ.",
                MaLoiChung.ViPhamRangBuoc);
        }

        foreach (var p in canThuHoi) p.TrangThai = TrangThaiPhanCong.DaThuHoi;

        nap.NhiemVu.UpdateDate = NgayUtil.BayGio();
        await Db.SaveChangesAsync(ct);

        var dto = await _troGiup.SangDtoAsync(new[] { nap.NhiemVu }, nap.NguoiThaoTac, homNay, kemQuyen: true, ct);
        return Ok(dto[0]);
    }

    // =================================================================
    // D1 — POST /api/v1/nhiem-vu/{id}/tiep-nhan
    // =================================================================

    /// <summary>§5.4 D1 / §2.4 T2 — tiep nhan nhiem vu: <c>(3, null)</c> -&gt; <c>(2, null)</c>.</summary>
    [HttpPost("nhiem-vu/{id:guid}/tiep-nhan")]
    public async Task<IActionResult> TiepNhan(Guid id, [FromBody] TiepNhanRequest? req, CancellationToken ct)
    {
        // §6.2 dong 9 — QUAN_TRI va NGUOI_GIAO deu KHONG duoc tiep nhan thay.
        var chan = BatBuocBenLam("tiếp nhận nhiệm vụ");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req ?? new TiepNhanRequest(), ct);
        if (loi is not null) return loi;

        return await ChayAsync(
            id,
            "tiếp nhận nhiệm vụ",
            q => q.TiepNhan,
            nap => _mayTrangThai.TiepNhan(nap.NhiemVu, UserId, req, nap.Ctx),
            null, null, ganVaoNhiemVu: false, ct);
    }

    // =================================================================
    // D2 — POST /api/v1/nhiem-vu/{id}/tu-choi
    // =================================================================

    /// <summary>§5.4 D2 / §2.4 T3 — tu choi nhiem vu (ly do BAT BUOC): -&gt; <c>(6, 10)</c>.</summary>
    [HttpPost("nhiem-vu/{id:guid}/tu-choi")]
    public async Task<IActionResult> TuChoi(Guid id, [FromBody] TuChoiRequest req, CancellationToken ct)
    {
        var chan = BatBuocBenLam("từ chối nhiệm vụ");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        return await ChayAsync(
            id,
            "từ chối nhiệm vụ",
            q => q.TuChoi,
            nap => _mayTrangThai.TuChoi(nap.NhiemVu, UserId, req, nap.Ctx),
            req.FileIds, LoaiBanGhiFile.XuLy, ganVaoNhiemVu: false, ct);
    }

    // =================================================================
    // D3 — POST /api/v1/nhiem-vu/{id}/tien-do
    // =================================================================

    /// <summary>§5.4 D3 / §2.4 T6 — cap nhat tien do (<c>mucdoht</c> 0-100). KHONG doi trang thai.</summary>
    [HttpPost("nhiem-vu/{id:guid}/tien-do")]
    public async Task<IActionResult> TienDo(Guid id, [FromBody] TienDoRequest req, CancellationToken ct)
    {
        var chan = BatBuocBenLam("cập nhật tiến độ");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        return await ChayAsync(
            id,
            "cập nhật tiến độ",
            q => q.CapNhatTienDo,
            nap => _mayTrangThai.CapNhatTienDo(nap.NhiemVu, UserId, req, nap.Ctx),
            req.FileIds, LoaiBanGhiFile.XuLy, ganVaoNhiemVu: false, ct);
    }

    // =================================================================
    // D4 — POST /api/v1/nhiem-vu/{id}/bao-cao
    // =================================================================

    /// <summary>
    /// §5.4 D4 / §2.4 T7 — gui bao cao ket qua: -&gt; <c>(trang thai da chon, 10)</c>.
    /// May trang thai VALIDATE LAI <c>trangthai</c> theo bo loc han cua §5.4 D5 (§6.4).
    /// </summary>
    [HttpPost("nhiem-vu/{id:guid}/bao-cao")]
    public async Task<IActionResult> BaoCao(Guid id, [FromBody] BaoCaoRequest req, CancellationToken ct)
    {
        var chan = BatBuocBenLam("gửi báo cáo kết quả");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        return await ChayAsync(
            id,
            "gửi báo cáo kết quả",
            q => q.GuiBaoCao,
            nap => _mayTrangThai.GuiBaoCao(nap.NhiemVu, UserId, req, nap.Ctx),
            req.FileIds, LoaiBanGhiFile.XuLy, ganVaoNhiemVu: false, ct);
    }

    // =================================================================
    // D5 — GET /api/v1/nhiem-vu/{id}/trang-thai-hop-le
    // =================================================================

    /// <summary>
    /// §5.4 D5 — danh sach trang thai duoc chon khi bao cao, DA LOC THEO HAN
    /// (con han: 1/2/3 · qua han: 5/7/3 · ma 13 luon bi loai).
    /// </summary>
    [HttpGet("nhiem-vu/{id:guid}/trang-thai-hop-le")]
    public async Task<IActionResult> TrangThaiHopLe(Guid id, CancellationToken ct)
    {
        var chan = BatBuocDangNhap();
        if (chan is not null) return chan;

        var homNay = HomNay;
        var nap = await _troGiup.NapAsync(id, UserId, homNay, ct);
        if (nap is null) return Loi404("Không tìm thấy nhiệm vụ.");

        if (!nap.Quyen.XemChiTiet) return Loi403("Bạn không có quyền xem nhiệm vụ này.");

        var kq = await _troGiup.TrangThaiHopLeAsync(nap.NhiemVu, homNay, ct);
        return Ok(kq);
    }

    // =================================================================
    // D6 — POST /api/v1/nhiem-vu/{id}/thu-hoi-bao-cao
    // =================================================================

    /// <summary>§5.4 D6 / §2.4 T8 — thu hoi bao cao: <c>(1|5, 10)</c> -&gt; <c>(2 hoac 7, null)</c>.</summary>
    [HttpPost("nhiem-vu/{id:guid}/thu-hoi-bao-cao")]
    public async Task<IActionResult> ThuHoiBaoCao(Guid id, [FromBody] ThuHoiBaoCaoRequest? req, CancellationToken ct)
    {
        var chan = BatBuocBenLam("thu hồi báo cáo");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req ?? new ThuHoiBaoCaoRequest(), ct);
        if (loi is not null) return loi;

        return await ChayAsync(
            id,
            "thu hồi báo cáo",
            q => q.ThuHoiBaoCao,
            nap => _mayTrangThai.ThuHoiBaoCao(nap.NhiemVu, UserId, req, nap.Ctx),
            null, null, ganVaoNhiemVu: false, ct);
    }

    // =================================================================
    // D7 — GET /api/v1/nhiem-vu/{id}/lich-su
    // =================================================================

    /// <summary>§5.4 D7 — lich su xu ly + lich su gia han cua mot nhiem vu.</summary>
    [HttpGet("nhiem-vu/{id:guid}/lich-su")]
    public async Task<IActionResult> LichSu(Guid id, CancellationToken ct)
    {
        var chan = BatBuocDangNhap();
        if (chan is not null) return chan;

        var nap = await _troGiup.NapAsync(id, UserId, HomNay, ct);
        if (nap is null) return Loi404("Không tìm thấy nhiệm vụ.");

        // §6.2 dong 18 — xem chi tiet + lich su.
        if (!nap.Quyen.XemChiTiet) return Loi403("Bạn không có quyền xem lịch sử của nhiệm vụ này.");

        return Ok(new LichSuNhiemVuDto
        {
            XuLy = await _troGiup.LichSuXuLyAsync(id, ct),
            GiaHan = await _troGiup.LichSuGiaHanAsync(id, ct)
        });
    }

    // =================================================================
    // MO RONG §2.4 T4/T5 — nguoi giao xu ly de nghi tu choi
    // =================================================================

    /// <summary>
    /// §2.4 T4/T5 — nguoi giao xu ly de nghi tu choi tai cap <c>(6, 10)</c>.
    /// MO RONG: §5 khong co endpoint rieng, nhung thieu no thi cap (6, 10) khong co loi ra.
    /// CHAP_NHAN -&gt; <c>(97, null)</c> · BAC_BO -&gt; <c>(2, 12)</c>.
    /// </summary>
    [HttpPost("nhiem-vu/{id:guid}/xu-ly-tu-choi")]
    public async Task<IActionResult> XuLyTuChoi(Guid id, [FromBody] XuLyTuChoiRequest req, CancellationToken ct)
    {
        var chan = BatBuocBenGiao("xử lý đề nghị từ chối");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        return await ChayAsync(
            id,
            "xử lý đề nghị từ chối",
            q => q.XuLyTuChoi,
            nap => _mayTrangThai.XuLyTuChoi(nap.NhiemVu, UserId, req, nap.Ctx),
            null, null, ganVaoNhiemVu: false, ct);
    }

    // =================================================================
    // §1.3 — Nhac viec
    // =================================================================

    /// <summary>§1.3 / §6.2 dong 17 — nhac viec. CHI ghi lich su, KHONG doi trang thai.</summary>
    [HttpPost("nhiem-vu/{id:guid}/nhac-viec")]
    public async Task<IActionResult> NhacViec(Guid id, [FromBody] NhacViecRequest req, CancellationToken ct)
    {
        var chan = BatBuocBenGiao("nhắc việc");
        if (chan is not null) return chan;

        var loi = await KiemTraAsync(req, ct);
        if (loi is not null) return loi;

        return await ChayAsync(
            id,
            "nhắc việc",
            q => q.NhacViec,
            nap => _mayTrangThai.NhacViec(nap.NhiemVu, UserId, req, nap.Ctx),
            null, null, ganVaoNhiemVu: false, ct);
    }

    // =================================================================
    // Khung chung cho moi hanh dong goi may trang thai
    // =================================================================

    /// <summary>
    /// Khung chung: nap ngu canh -&gt; kiem quyen LOP 2 (§6.4) -&gt; goi may trang thai
    /// -&gt; ghi ket xuat -&gt; gan tep -&gt; luu -&gt; tra ve nhiem vu da cap nhat.
    /// </summary>
    /// <param name="ganVaoNhiemVu">
    /// true: tep gan thang vao ban ghi NHIEM VU (C5);
    /// false: tep gan vao ban ghi LICH SU XU LY vua sinh (D2/D3/D4).
    /// </param>
    private async Task<IActionResult> ChayAsync(
        Guid id,
        string tenHanhDong,
        Func<QuyenNhiemVu, bool> coQuyen,
        Func<NhiemVuDaNap, Result<DmNhiemVuChiTiet>> hanhDong,
        List<Guid>? fileIds,
        string? loaiBanGhiFile,
        bool ganVaoNhiemVu,
        CancellationToken ct)
    {
        var homNay = HomNay;
        var nap = await _troGiup.NapAsync(id, UserId, homNay, ct);
        if (nap is null) return Loi404("Không tìm thấy nhiệm vụ.");

        // §6.4 LOP 2 — quyen theo DU LIEU + TRANG THAI, tinh boi IQuyenService.
        if (!coQuyen(nap.Quyen)) return Loi403(MoTaTuChoiQuyen(tenHanhDong, nap));

        var kq = hanhDong(nap);
        var loi = NeuLoi(kq);
        if (loi is not null) return loi;

        _troGiup.ApDungKetXuat(nap.Ctx);

        if (fileIds is not null && fileIds.Count > 0 && loaiBanGhiFile is not null)
        {
            Guid? recordId = null;
            if (ganVaoNhiemVu)
            {
                recordId = nap.NhiemVu.Id;
            }
            else
            {
                var banGhi = nap.Ctx.KetXuat.LichSuXuLyMoi.LastOrDefault();
                if (banGhi is not null) recordId = banGhi.Id;
            }

            if (recordId.HasValue)
            {
                var ganFile = await _troGiup.GanFileAsync(fileIds, recordId.Value, loaiBanGhiFile, UserId, ct);
                var loiFile = NeuLoi(ganFile);
                if (loiFile is not null) return loiFile;
            }
        }

        await Db.SaveChangesAsync(ct);

        var dto = await _troGiup.SangDtoAsync(new[] { nap.NhiemVu }, nap.NguoiThaoTac, homNay, kemQuyen: true, ct);
        return Ok(dto[0]);
    }

    /// <summary>Thong bao 403 co nêu rõ trang thai hien tai de nguoi dung hieu vi sao bi chan.</summary>
    private static string MoTaTuChoiQuyen(string tenHanhDong, NhiemVuDaNap nap)
    {
        var a = TrangThaiNv.Nhan(nap.NhiemVu.TrangThai);
        var b = TrangThaiPh.Nhan(nap.NhiemVu.TrangThaiDvXuly);
        return $"Bạn không có quyền {tenHanhDong} ở trạng thái hiện tại ({a} / {b}).";
    }
}
