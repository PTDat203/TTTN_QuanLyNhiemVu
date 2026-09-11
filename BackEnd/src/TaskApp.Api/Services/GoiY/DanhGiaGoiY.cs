using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskApp.Api.Common;
using TaskApp.Api.Data;
using TaskApp.Api.Entities;
using TaskApp.Api.Services.Ai;

namespace TaskApp.Api.Services.GoiY;

/// <summary>
/// Hiệu chỉnh ngưỡng và đánh giá mô hình gợi ý trên dữ liệu lịch sử.
///
/// <para>
/// <b>Nhãn đúng</b> cho phần xếp hạng: người thực sự được giao và làm đạt (chất lượng từ
/// <see cref="ChatLuongNhanDung"/>/5 trở lên). Đây là quyết định của quản lý trong quá khứ — một
/// nhãn xấp xỉ chứ không phải chân lý. Nhãn phòng ban là phòng thực thi; nhãn kỹ năng là các kỹ
/// năng người giao nhập tay (nguồn MANUAL).
/// </para>
/// <para>
/// <b>Chống "biết trước tương lai":</b> mỗi nhiệm vụ được chấm với dữ liệu nhìn từ lúc nó được tạo —
/// lịch sử và khối lượng việc chỉ tính tới thời điểm đó, bỏ chính nó ra, người vào làm sau đó không
/// được làm ứng viên.
/// </para>
/// <para>
/// <b>Chia tập theo thời gian:</b> nhiệm vụ tạo trước <see cref="MocChiaTap"/> để hiệu chỉnh ngưỡng,
/// từ mốc đó trở đi để đánh giá. Hiệu chỉnh và đánh giá trên cùng một tập thì con số đẹp giả tạo.
/// </para>
/// </summary>
public sealed class DanhGiaGoiY
{
    public static readonly DateTime MocChiaTap = new(2026, 7, 1);

    /// <summary>Chất lượng tối thiểu để coi một lần giao việc trong quá khứ là "giao đúng người".</summary>
    public const int ChatLuongNhanDung = 4;

    private readonly TaskDbContext _db;
    private readonly BoXepHang _boXepHang;
    private readonly DoTuongDongNhung _nhung;
    private readonly DoTuongDongTfIdf _tfIdf;
    private readonly CauHinhGoiY _cauHinh;

    public DanhGiaGoiY(
        TaskDbContext db, BoXepHang boXepHang, DoTuongDongNhung nhung, DoTuongDongTfIdf tfIdf,
        IOptions<CauHinhGoiY> cauHinh)
    {
        _db = db;
        _boXepHang = boXepHang;
        _nhung = nhung;
        _tfIdf = tfIdf;
        _cauHinh = cauHinh.Value;
    }

    private sealed record NhiemVuCoNhan(
        long Id, string TieuDe, string NoiDung, long CreatorId, long AssigneeId, long? DepartmentId,
        DateTime TaoLuc, int? ChatLuong, IReadOnlyList<long> KyNangThat);

    // =====================================================================
    // 1. PHÂN BỐ ĐỘ TƯƠNG ĐỒNG THÔ — để chọn cặp ngưỡng chuẩn hoá
    // =====================================================================

    /// <summary>
    /// Đo độ tương đồng thô của các cặp LIÊN QUAN và KHÔNG LIÊN QUAN trên tập hiệu chỉnh, cho cả hai
    /// phương pháp. Từ đó chọn cặp ngưỡng chuẩn hoá: Thấp = trung vị nhóm không liên quan, Cao = phân vị
    /// 90 nhóm liên quan. Kèm AUC — xác suất một cặp liên quan được điểm cao hơn một cặp không liên
    /// quan — để thấy phương pháp nào phân biệt tốt hơn.
    /// </summary>
    public async Task<object> PhanBoAsync(CancellationToken ct)
    {
        var tap = (await NapNhiemVuCoNhanAsync(ct))
            .Where(t => t.TaoLuc < MocChiaTap && t.DepartmentId != null).ToList();

        var nguoiNhan = await _db.Users.AsNoTracking()
            .Where(u => u.Status == TrangThaiNguoiDung.HoatDong && u.Role != VaiTro.GiamDoc)
            .Select(u => u.Id).ToListAsync(ct);
        var dv = await NapDuLieuGoiY.NapAsync(_db, string.Empty, nguoiNhan, null, null, null, ct);

        var ketQua = new List<object>();
        foreach (var pp in new DoTuongDong[] { _nhung, _tfIdf })
        {
            var hoSo = new CapPhanBo();
            var phong = new CapPhanBo();
            var kyNang = new CapPhanBo();
            var nhiemVu = new CapPhanBo();
            var dungDuoc = true;

            for (var i = 0; i < tap.Count && dungDuoc; i++)
            {
                var t = tap[i];

                var sHoSo = await pp.SoVoiThoAsync(t.NoiDung, dv.UngVien.Select(u => u.VanBanHoSo).ToList(), KieuSoSanh.BatDoiXung, ct);
                var sPhong = await pp.SoVoiThoAsync(t.NoiDung, dv.PhongBan.Select(p => p.VanBan).ToList(), KieuSoSanh.BatDoiXung, ct);
                var sKyNang = await pp.SoVoiThoAsync(t.NoiDung, dv.DanhMucKyNang.Select(k => k.VanBan).ToList(), KieuSoSanh.BatDoiXung, ct);
                var sau = tap.Skip(i + 1).ToList();
                var sNhiemVu = await pp.SoVoiThoAsync(t.NoiDung, sau.Select(x => x.NoiDung).ToList(), KieuSoSanh.DoiXung, ct);

                if (sHoSo is null || sPhong is null || sKyNang is null || sNhiemVu is null)
                {
                    dungDuoc = false;
                    break;
                }

                // Hồ sơ: người thực làm là liên quan; người KHÁC PHÒNG là không liên quan. Người cùng
                // phòng mà không làm việc này thì bỏ — họ có thể cũng phù hợp, không phải mẫu âm sạch.
                for (var j = 0; j < dv.UngVien.Count; j++)
                {
                    var u = dv.UngVien[j];
                    if (u.UserId == t.AssigneeId) hoSo.LienQuan.Add(sHoSo[j]);
                    else if (u.DepartmentId != t.DepartmentId) hoSo.KhongLienQuan.Add(sHoSo[j]);
                }

                for (var j = 0; j < dv.PhongBan.Count; j++)
                    (dv.PhongBan[j].Id == t.DepartmentId ? phong.LienQuan : phong.KhongLienQuan).Add(sPhong[j]);

                for (var j = 0; j < dv.DanhMucKyNang.Count; j++)
                    (t.KyNangThat.Contains(dv.DanhMucKyNang[j].Id) ? kyNang.LienQuan : kyNang.KhongLienQuan).Add(sKyNang[j]);

                // Nhiệm vụ với nhiệm vụ: cùng người làm là liên quan; khác phòng là không liên quan.
                for (var j = 0; j < sau.Count; j++)
                {
                    if (sau[j].AssigneeId == t.AssigneeId) nhiemVu.LienQuan.Add(sNhiemVu[j]);
                    else if (sau[j].DepartmentId != t.DepartmentId) nhiemVu.KhongLienQuan.Add(sNhiemVu[j]);
                }
            }

            if (!dungDuoc)
            {
                ketQua.Add(new { phuongPhap = pp.Ten, loi = "Không gọi được dịch vụ nhúng." });
                continue;
            }

            ketQua.Add(new
            {
                phuongPhap = pp.Ten,
                hoSo = hoSo.TomTat(),
                phongBan = phong.TomTat(),
                kyNang = kyNang.TomTat(),
                nhiemVu = nhiemVu.TomTat(),
                deXuatNguong = new
                {
                    batDoiXung = new { thap = Tron(PhanVi(hoSo.KhongLienQuan, 0.5)), cao = Tron(PhanVi(hoSo.LienQuan, 0.9)) },
                    doiXung = new { thap = Tron(PhanVi(nhiemVu.KhongLienQuan, 0.5)), cao = Tron(PhanVi(nhiemVu.LienQuan, 0.9)) }
                }
            });
        }

        return new
        {
            tapHieuChinh = tap.Count,
            mocChiaTap = MocChiaTap,
            cachChonNguong = "Thấp = trung vị nhóm KHÔNG liên quan; Cao = phân vị 90 nhóm LIÊN QUAN.",
            ketQua
        };
    }

    // =====================================================================
    // 2. DÒ NGƯỠNG QUYẾT ĐỊNH — trên thang đã chuẩn hoá
    // =====================================================================

    /// <summary>
    /// Dò ngưỡng phòng ban và ngưỡng trích kỹ năng trên tập hiệu chỉnh, với cặp ngưỡng chuẩn hoá
    /// đang có trong cấu hình.
    ///
    /// <para>
    /// Phòng ban: trong các bộ ngưỡng GIỮ ĐÚNG PHÒNG ở ít nhất 95% nhiệm vụ, chọn bộ lọc gọn nhất
    /// (trung bình giữ lại ít phòng nhất). Lọc sai phòng là loại luôn người đúng khỏi danh sách —
    /// lỗi nặng hơn nhiều so với lọc chưa gọn, nên đặt điều kiện giữ đúng lên trước.
    /// </para>
    /// <para>Kỹ năng: chọn bộ ngưỡng có F1 cao nhất so với kỹ năng người giao nhập tay.</para>
    /// </summary>
    public async Task<object> DoNguongAsync(bool chiDungTfIdf, CancellationToken ct)
    {
        var tap = (await NapNhiemVuCoNhanAsync(ct))
            .Where(t => t.TaoLuc < MocChiaTap && t.DepartmentId != null).ToList();

        var mau = new List<(long PhongThat, IReadOnlyList<DiemDonVi> CacPhong, IReadOnlyDictionary<long, double> DiemKyNang, IReadOnlyList<long> KyNangThat)>();
        var phuongPhap = string.Empty;
        var daLuiVe = false;

        foreach (var t in tap)
        {
            var dv = await NapDuLieuGoiY.NapAsync(_db, t.NoiDung, Array.Empty<long>(), t.TaoLuc, t.Id, null, ct);
            var kq = await _boXepHang.ChayAsync(dv, chiDungTfIdf, ct);
            phuongPhap = kq.PhuongPhap;
            daLuiVe |= kq.DaLuiVeTfIdf;
            mau.Add((t.DepartmentId!.Value, kq.SuyLuan.CacPhong, kq.SuyLuan.DiemKyNang, t.KyNangThat));
        }

        // ---- Phòng ban ----
        var luoiPhong = new List<NguongPhong>();
        for (var a = 0; a <= 19; a++)
        for (var b = 0; b <= 20; b++)
        {
            double san = Math.Round(a * 0.05, 2), cach = Math.Round(b * 0.02, 2);
            int giuDung = 0, chacChan = 0, chacChanDung = 0;
            double tongPhongGiu = 0;

            foreach (var m in mau)
            {
                var p = m.CacPhong;
                if (p.Count == 0) continue;

                if (p[0].Diem < san)
                {
                    giuDung++;
                    tongPhongGiu += p.Count;
                }
                else if (p.Count == 1 || p[0].Diem - p[1].Diem >= cach)
                {
                    chacChan++;
                    tongPhongGiu += 1;
                    if (p[0].DonVi.Id == m.PhongThat) { giuDung++; chacChanDung++; }
                }
                else
                {
                    tongPhongGiu += 2;
                    if (p[0].DonVi.Id == m.PhongThat || p[1].DonVi.Id == m.PhongThat) giuDung++;
                }
            }

            luoiPhong.Add(new NguongPhong(san, cach,
                Tron(giuDung / (double)mau.Count)!.Value,
                Tron(tongPhongGiu / mau.Count)!.Value,
                Tron(chacChan / (double)mau.Count)!.Value,
                chacChan == 0 ? null : Tron(chacChanDung / (double)chacChan)));
        }

        // Trong các bộ giữ đúng phòng ≥ 95%, lấy những bộ gọn gần bằng bộ gọn nhất (chênh không quá
        // 0,05 phòng), rồi chọn bộ có SÀN CAO NHẤT. Tập hiệu chỉnh toàn nhiệm vụ đúng chuyên môn nên
        // bộ tối ưu thuần tuý hay đẩy sàn về gần 0 — tức là xoá mất nhánh "không rõ phòng" dành cho
        // những nhiệm vụ lạ hẳn. Nhường 0,05 phòng để giữ lại nhánh đó.
        var hopLe = luoiPhong.Where(x => x.TyLeGiuDungPhong >= 0.95).ToList();
        var gonNhat = hopLe.Count == 0 ? 0 : hopLe.Min(x => x.SoPhongGiuTrungBinh);
        var phongHopLe = hopLe.Where(x => x.SoPhongGiuTrungBinh <= gonNhat + 0.05 + 1e-9)
            .OrderByDescending(x => x.San).ThenBy(x => x.CachBiet)
            .ToList();

        // ---- Kỹ năng ----
        var luoiKyNang = new List<NguongKyNang>();
        for (var a = 0; a <= 19; a++)
        for (var b = 0; b <= 10; b++)
        {
            double san = Math.Round(a * 0.05, 2), khoang = Math.Round(b * 0.05, 2);
            int dungRa = 0, saiRa = 0, bo = 0;

            foreach (var m in mau)
            {
                var cao = m.DiemKyNang.Values.DefaultIfEmpty(0).Max();
                var chon = m.DiemKyNang
                    .Where(x => x.Value >= san && x.Value >= cao - khoang)
                    .OrderByDescending(x => x.Value).Take(_cauHinh.SoKyNangTrichToiDa)
                    .Select(x => x.Key).ToHashSet();

                dungRa += chon.Count(m.KyNangThat.Contains);
                saiRa += chon.Count(x => !m.KyNangThat.Contains(x));
                bo += m.KyNangThat.Count(x => !chon.Contains(x));
            }

            luoiKyNang.Add(TaoNguongKyNang(san, khoang, dungRa, saiRa, bo));
        }

        return new
        {
            phuongPhap,
            daLuiVeTfIdf = daLuiVe,
            soMau = mau.Count,
            doChinhXacPhongDauTien = Tron(mau.Count(m => m.CacPhong.Count > 0 && m.CacPhong[0].DonVi.Id == m.PhongThat) / (double)mau.Count),
            phongBan = new { deXuat = phongHopLe.FirstOrDefault(), cacLuaChonKhac = phongHopLe.Skip(1).Take(5) },
            kyNang = new
            {
                deXuat = luoiKyNang.OrderByDescending(x => x.F1).ThenByDescending(x => x.San).FirstOrDefault(),
                cacLuaChonKhac = luoiKyNang.OrderByDescending(x => x.F1).ThenByDescending(x => x.San).Skip(1).Take(5)
            }
        };
    }

    private sealed record NguongPhong(
        double San, double CachBiet, double TyLeGiuDungPhong, double SoPhongGiuTrungBinh,
        double TyLeChacChan, double? DoChinhXacKhiChacChan);

    private sealed record NguongKyNang(
        double San, double KhoangCach, double DoChinhXac, double DoPhu, double F1);

    private static NguongKyNang TaoNguongKyNang(double san, double khoang, int dungRa, int saiRa, int bo)
    {
        var p = dungRa + saiRa == 0 ? 0 : dungRa / (double)(dungRa + saiRa);
        var r = dungRa + bo == 0 ? 0 : dungRa / (double)(dungRa + bo);
        var f1 = p + r == 0 ? 0 : 2 * p * r / (p + r);
        return new NguongKyNang(san, khoang, Tron(p)!.Value, Tron(r)!.Value, Tron(f1)!.Value);
    }

    // =====================================================================
    // 3. ĐÁNH GIÁ — so ba phương pháp
    // =====================================================================

    private sealed class KetQuaMotNhiemVu
    {
        public NhiemVuCoNhan NhiemVu { get; init; } = null!;
        public int SoUngVien { get; init; }
        public bool CoNguoiThat { get; init; }
        public int? HangQuyTac { get; init; }
        public int? HangTfIdf { get; init; }
        public int? HangNhung { get; init; }
        public KetQuaSuyLuan SuyLuanTfIdf { get; init; } = new();
        public KetQuaSuyLuan SuyLuanNhung { get; init; } = new();
        public IReadOnlyList<long> KyNangQuyTac { get; init; } = Array.Empty<long>();
        public bool NhungLuiVe { get; init; }
    }

    /// <summary>
    /// So ba phương pháp trên cùng các nhiệm vụ:
    /// <list type="number">
    ///   <item><b>Theo luật</b> — kỹ năng nào có tên xuất hiện nguyên văn trong nội dung thì coi là
    ///         cần; điểm = 0,6 × mức kỹ năng + 0,4 × độ rảnh. Không phòng ban, không ngữ nghĩa.</item>
    ///   <item><b>TF-IDF</b> — cùng quy trình hai tầng, đo độ gần nghĩa bằng TF-IDF + cosine.</item>
    ///   <item><b>Nhúng ngữ nghĩa</b> — cùng quy trình hai tầng, đo bằng mô hình E5.</item>
    /// </list>
    /// Chỉ số: Top-1, Top-3, MRR cho xếp hạng; độ chính xác đoán phòng; độ chính xác, độ phủ, F1
    /// cho trích kỹ năng. Kèm mức ngẫu nhiên để có mốc so sánh.
    /// </summary>
    public async Task<object> DanhGiaAsync(CancellationToken ct)
    {
        var tatCa = await NapNhiemVuCoNhanAsync(ct);
        var danhMuc = await _db.Skills.AsNoTracking()
            .Select(s => new KyNangDanhMuc { Id = s.Id, Code = s.Code, Ten = s.Name }).ToListAsync(ct);

        var ds = new List<KetQuaMotNhiemVu>();
        foreach (var t in tatCa.Where(t => t.DepartmentId != null))
        {
            var ungVien = await UngVienTaiMocAsync(t.CreatorId, t.TaoLuc, ct);
            var dv = await NapDuLieuGoiY.NapAsync(_db, t.NoiDung, ungVien, t.TaoLuc, t.Id, null, ct);

            var kqNhung = await _boXepHang.ChayAsync(dv, chiDungTfIdf: false, ct);
            var kqTfIdf = await _boXepHang.ChayAsync(dv, chiDungTfIdf: true, ct);
            var kyNangQuyTac = TrichKyNangTuKhoa(t.NoiDung, danhMuc);

            ds.Add(new KetQuaMotNhiemVu
            {
                NhiemVu = t,
                SoUngVien = ungVien.Count,
                CoNguoiThat = ungVien.Contains(t.AssigneeId),
                HangQuyTac = Hang(XepHangQuyTac(dv, kyNangQuyTac), t.AssigneeId),
                HangTfIdf = Hang(kqTfIdf.UngVien.Select(u => u.HoSo.UserId), t.AssigneeId),
                HangNhung = Hang(kqNhung.UngVien.Select(u => u.HoSo.UserId), t.AssigneeId),
                SuyLuanTfIdf = kqTfIdf.SuyLuan,
                SuyLuanNhung = kqNhung.SuyLuan,
                KyNangQuyTac = kyNangQuyTac,
                NhungLuiVe = kqNhung.DaLuiVeTfIdf
            });
        }

        var kiemTra = ds.Where(x => x.NhiemVu.TaoLuc >= MocChiaTap).ToList();
        var hieuChinh = ds.Where(x => x.NhiemVu.TaoLuc < MocChiaTap).ToList();

        return new
        {
            mocChiaTap = MocChiaTap,
            phienBanThamSo = _cauHinh.PhienBan,
            nhanDung = $"Người thực sự được giao và làm đạt chất lượng từ {ChatLuongNhanDung}/5.",
            canhBao = ds.Any(x => x.NhungLuiVe)
                ? "Dịch vụ nhúng không phản hồi ở một số nhiệm vụ — cột nhúng ngữ nghĩa KHÔNG hợp lệ."
                : null,
            tapKiemTra = TomTat(kiemTra),
            tapHieuChinh = TomTat(hieuChinh),
            chiTietTapKiemTra = kiemTra
                .Where(x => x.NhiemVu.ChatLuong >= ChatLuongNhanDung && x.CoNguoiThat)
                .Select(x => new
                {
                    x.NhiemVu.Id,
                    x.NhiemVu.TieuDe,
                    nguoiThat = x.NhiemVu.AssigneeId,
                    soUngVien = x.SoUngVien,
                    hang = new { quyTac = x.HangQuyTac, tfIdf = x.HangTfIdf, nhung = x.HangNhung },
                    phongDoan = new
                    {
                        that = x.NhiemVu.DepartmentId,
                        tfIdf = x.SuyLuanTfIdf.CacPhong.FirstOrDefault()?.DonVi.Id,
                        nhung = x.SuyLuanNhung.CacPhong.FirstOrDefault()?.DonVi.Id,
                        ketLuanNhung = x.SuyLuanNhung.KetLuan.ToString()
                    }
                })
        };
    }

    private object TomTat(List<KetQuaMotNhiemVu> ds)
    {
        var xepHang = ds.Where(x => x.NhiemVu.ChatLuong >= ChatLuongNhanDung && x.CoNguoiThat).ToList();

        object ChiSo(Func<KetQuaMotNhiemVu, int?> hang) => new
        {
            top1 = Tron(xepHang.Count == 0 ? 0 : xepHang.Average(x => hang(x) == 1 ? 1.0 : 0.0)),
            top3 = Tron(xepHang.Count == 0 ? 0 : xepHang.Average(x => hang(x) is <= 3 ? 1.0 : 0.0)),
            mrr = Tron(xepHang.Count == 0 ? 0 : xepHang.Average(x => hang(x) is { } h ? 1.0 / h : 0.0))
        };

        object Phong(Func<KetQuaMotNhiemVu, KetQuaSuyLuan> suyLuan) => new
        {
            doChinhXacPhongDauTien = Tron(ds.Count == 0 ? 0 : ds.Average(x =>
                suyLuan(x).CacPhong.FirstOrDefault()?.DonVi.Id == x.NhiemVu.DepartmentId ? 1.0 : 0.0)),
            tyLeGiuDungPhong = Tron(ds.Count == 0 ? 0 : ds.Average(x =>
                suyLuan(x).KetLuan == KetLuanPhongBan.KhongRo ||
                suyLuan(x).PhongDaChon.Contains(x.NhiemVu.DepartmentId!.Value) ? 1.0 : 0.0)),
            tyLeChacChan = Tron(ds.Count == 0 ? 0 : ds.Average(x => suyLuan(x).KetLuan == KetLuanPhongBan.ChacChan ? 1.0 : 0.0))
        };

        object KyNang(Func<KetQuaMotNhiemVu, IEnumerable<long>> chon)
        {
            int dungRa = 0, saiRa = 0, bo = 0;
            foreach (var x in ds)
            {
                var c = chon(x).ToHashSet();
                dungRa += c.Count(x.NhiemVu.KyNangThat.Contains);
                saiRa += c.Count(k => !x.NhiemVu.KyNangThat.Contains(k));
                bo += x.NhiemVu.KyNangThat.Count(k => !c.Contains(k));
            }
            var n = TaoNguongKyNang(0, 0, dungRa, saiRa, bo);
            return new { doChinhXac = n.DoChinhXac, doPhu = n.DoPhu, f1 = n.F1 };
        }

        return new
        {
            soNhiemVu = ds.Count,
            xepHang = new
            {
                soNhiemVuCoNhan = xepHang.Count,
                ngauNhien = new
                {
                    top1 = Tron(xepHang.Count == 0 ? 0 : xepHang.Average(x => 1.0 / x.SoUngVien)),
                    top3 = Tron(xepHang.Count == 0 ? 0 : xepHang.Average(x => Math.Min(3.0, x.SoUngVien) / x.SoUngVien)),
                    mrr = Tron(xepHang.Count == 0 ? 0 : xepHang.Average(x => Enumerable.Range(1, x.SoUngVien).Sum(k => 1.0 / k) / x.SoUngVien))
                },
                quyTac = ChiSo(x => x.HangQuyTac),
                tfIdf = ChiSo(x => x.HangTfIdf),
                nhung = ChiSo(x => x.HangNhung)
            },
            phongBan = new { tfIdf = Phong(x => x.SuyLuanTfIdf), nhung = Phong(x => x.SuyLuanNhung) },
            kyNang = new
            {
                quyTac = KyNang(x => x.KyNangQuyTac),
                tfIdf = KyNang(x => x.SuyLuanTfIdf.KyNang.Select(k => k.SkillId)),
                nhung = KyNang(x => x.SuyLuanNhung.KyNang.Select(k => k.SkillId))
            }
        };
    }

    // =====================================================================
    // PHƯƠNG PHÁP CƠ SỞ THEO LUẬT
    // =====================================================================

    /// <summary>Kỹ năng nào có tên xuất hiện nguyên văn trong nội dung (không phân biệt dấu, hoa thường).</summary>
    private static List<long> TrichKyNangTuKhoa(string noiDung, IReadOnlyList<KyNangDanhMuc> danhMuc)
    {
        var vanBan = XuLyVanBan.BoDau(noiDung).ToLowerInvariant();
        return danhMuc
            .Where(k => vanBan.Contains(XuLyVanBan.BoDau(k.Ten).ToLowerInvariant()))
            .Select(k => k.Id)
            .ToList();
    }

    private IEnumerable<long> XepHangQuyTac(DauVaoXepHang dv, IReadOnlyList<long> kyNang)
    {
        return dv.UngVien
            .Select(u =>
            {
                var muc = kyNang.Count == 0
                    ? 0.5
                    : kyNang.Average(id => u.MucKyNang.TryGetValue(id, out var m)
                        ? Math.Min(1.0, m / (double)_cauHinh.MucKyNangMacDinh)
                        : 0.0);
                var ranh = Math.Clamp(1.0 - u.TaiHienTai / _cauHinh.NguongKhoiLuong, 0.0, 1.0);
                return (u, Diem: 0.6 * muc + 0.4 * ranh);
            })
            .OrderByDescending(x => x.Diem)
            .ThenBy(x => x.u.TaiHienTai)
            .ThenBy(x => x.u.FullName, StringComparer.CurrentCulture)
            .Select(x => x.u.UserId);
    }

    // =====================================================================
    // HỖ TRỢ
    // =====================================================================

    private async Task<List<NhiemVuCoNhan>> NapNhiemVuCoNhanAsync(CancellationToken ct)
    {
        var nv = await _db.Tasks.AsNoTracking()
            .Where(t => t.AssigneeId != null)
            .Select(t => new
            {
                t.Id, t.Title, t.Description, t.CreatorId, AssigneeId = t.AssigneeId!.Value,
                t.DepartmentId, t.CreatedAt
            })
            .ToListAsync(ct);

        var chatLuong = (await _db.TaskReports.AsNoTracking()
                .Where(r => r.Status == TrangThaiBaoCao.DaXacNhan)
                .Select(r => new { r.TaskId, r.QualityScore, r.ReviewedAt })
                .ToListAsync(ct))
            .GroupBy(r => r.TaskId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.ReviewedAt).First().QualityScore);

        var kyNang = (await _db.TaskRequiredSkills.AsNoTracking()
                .Where(r => r.Source == NguonKyNang.ThuCong)
                .Select(r => new { r.TaskId, r.SkillId })
                .ToListAsync(ct))
            .ToLookup(r => r.TaskId, r => r.SkillId);

        return nv.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id)
            .Select(t => new NhiemVuCoNhan(
                t.Id, t.Title, VanBanHoSo.NhiemVu(t.Title, t.Description), t.CreatorId, t.AssigneeId,
                t.DepartmentId, t.CreatedAt, chatLuong.GetValueOrDefault(t.Id), kyNang[t.Id].ToList()))
            .ToList();
    }

    /// <summary>Những người người tạo nhiệm vụ giao được việc cho, và đã vào làm trước mốc.</summary>
    private async Task<List<long>> UngVienTaiMocAsync(long creatorId, DateTime moc, CancellationToken ct)
    {
        var viTri = await PhamViToChuc.NapAsync(_db, creatorId, ct);
        if (viTri is null) return new List<long>();

        return await _db.Users.AsNoTracking()
            .NguoiNhanDuoc(viTri)
            .Where(u => u.HiredDate == null || u.HiredDate <= moc)
            .Select(u => u.Id)
            .ToListAsync(ct);
    }

    private static int? Hang(IEnumerable<long> thuTu, long nguoiThat)
    {
        var i = 0;
        foreach (var id in thuTu)
        {
            i++;
            if (id == nguoiThat) return i;
        }
        return null;
    }

    private static double? PhanVi(IReadOnlyList<double> ds, double p)
    {
        if (ds.Count == 0) return null;
        var s = ds.OrderBy(x => x).ToList();
        var viTri = p * (s.Count - 1);
        var duoi = (int)Math.Floor(viTri);
        var tren = (int)Math.Ceiling(viTri);
        return s[duoi] + (s[tren] - s[duoi]) * (viTri - duoi);
    }

    private static double? Tron(double? x) => x is { } v ? Math.Round(v, 4) : null;

    /// <summary>Hai nhóm điểm: cặp liên quan và cặp không liên quan.</summary>
    private sealed class CapPhanBo
    {
        public List<double> LienQuan { get; } = new();
        public List<double> KhongLienQuan { get; } = new();

        public object TomTat() => new
        {
            lienQuan = MoTa(LienQuan),
            khongLienQuan = MoTa(KhongLienQuan),
            auc = Tron(Auc())
        };

        private static object MoTa(List<double> x) => new
        {
            n = x.Count,
            p10 = Tron(PhanVi(x, 0.10)),
            p25 = Tron(PhanVi(x, 0.25)),
            p50 = Tron(PhanVi(x, 0.50)),
            p75 = Tron(PhanVi(x, 0.75)),
            p90 = Tron(PhanVi(x, 0.90))
        };

        /// <summary>
        /// Xác suất một cặp liên quan được điểm cao hơn một cặp không liên quan (bằng nhau tính nửa).
        /// 0,5 là đoán mò, 1 là phân biệt hoàn hảo. Không phụ thuộc cặp ngưỡng chuẩn hoá nào.
        /// </summary>
        private double? Auc()
        {
            if (LienQuan.Count == 0 || KhongLienQuan.Count == 0) return null;
            double thang = 0;
            foreach (var a in LienQuan)
            foreach (var b in KhongLienQuan)
                thang += a > b ? 1 : a == b ? 0.5 : 0;
            return thang / ((double)LienQuan.Count * KhongLienQuan.Count);
        }
    }
}
