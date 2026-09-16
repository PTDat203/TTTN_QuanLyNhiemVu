using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskApp.Api.Common;
using TaskApp.Api.Data;
using TaskApp.Api.Dtos;
using TaskApp.Api.Services.Ai;
using TaskApp.Api.Services.GoiY;

namespace TaskApp.Api.Services;

/// <summary>
/// Gợi ý người thực hiện phù hợp — chức năng trọng tâm của đề tài.
///
/// <para>
/// Lớp này lo phần "bên ngoài": kiểm quyền, xác định phạm vi ứng viên, nạp dữ liệu, rồi dịch kết
/// quả sang tiếng Việt cho người đọc. Toàn bộ phần suy luận và chấm điểm nằm ở <see cref="BoXepHang"/>.
/// </para>
/// <para>
/// <b>Giải thích được là yêu cầu bắt buộc, không phải điểm cộng.</b> Mỗi ứng viên trả về kèm điểm
/// từng thành phần, số liệu thô và lý do bằng tiếng Việt; kết luận phòng ban cũng kèm điểm từng
/// phòng. Người giao phải hiểu vì sao hệ thống đề xuất người này thì mới dám tin — và quyết định
/// cuối cùng vẫn là của họ.
/// </para>
/// <para>
/// Không lưu kết quả gợi ý. Khối lượng việc và lịch sử thay đổi liên tục, lưu lại là lỗi thời ngay.
/// </para>
/// </summary>
public sealed class GoiYService
{
    private readonly TaskDbContext _db;
    private readonly BoXepHang _boXepHang;
    private readonly CauHinhGoiY _cauHinh;
    private readonly ILogger<GoiYService> _log;

    public GoiYService(TaskDbContext db, BoXepHang boXepHang, IOptions<CauHinhGoiY> cauHinh, ILogger<GoiYService> log)
    {
        _db = db;
        _boXepHang = boXepHang;
        _cauHinh = cauHinh.Value;
        _log = log;
    }

    /// <summary>Chấm điểm và xếp hạng ứng viên cho một nhiệm vụ.</summary>
    public async Task<KetQua<GoiYResponse>> GoiYAsync(
        GoiYRequest yeuCau, long nguoiGoiId, CancellationToken ct = default)
    {
        var dongHo = Stopwatch.StartNew();

        // --- 1. Nội dung nhiệm vụ ---
        var tieuDe = yeuCau.Title;
        var moTa = yeuCau.Description;
        long? assigneeHienTai = null;
        IReadOnlyList<KyNangCan>? kyNangCoSan = null;

        if (yeuCau.TaskId is { } taskId)
        {
            var nv = await _db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, ct);
            if (nv is null)
                return KetQua<GoiYResponse>.KhongTimThay($"Không tìm thấy nhiệm vụ #{taskId}.");

            if (nv.CreatorId != nguoiGoiId)
                return KetQua<GoiYResponse>.KhongCoQuyen(
                    "Chỉ người tạo nhiệm vụ mới xem được gợi ý cho nhiệm vụ này.");

            tieuDe = nv.Title;
            moTa = nv.Description;
            assigneeHienTai = nv.AssigneeId;
            kyNangCoSan = await NapDuLieuGoiY.KyNangDaLuuAsync(_db, taskId, ct);
        }

        var noiDung = VanBanHoSo.NhiemVu(tieuDe, moTa);
        if (string.IsNullOrWhiteSpace(noiDung))
        {
            return KetQua<GoiYResponse>.DuLieuKhongHopLe(
                "Cần có tiêu đề hoặc mô tả nhiệm vụ để tìm người phù hợp.");
        }

        // --- 2. Phạm vi: chỉ những người mà người gọi ĐƯỢC GIAO VIỆC cho ---
        // Dùng đúng quy tắc của nút "Giao" (PhamViToChuc), nên AI không bao giờ gợi ý một người
        // mà lúc giao thật backend lại từ chối. Trừ người đang giữ chính việc này.
        var viTri = await PhamViToChuc.NapAsync(_db, nguoiGoiId, ct);
        if (viTri is null)
            return KetQua<GoiYResponse>.KhongCoQuyen("Không xác định được người gọi.");

        var ungVienIds = await _db.Users.AsNoTracking()
            .NguoiNhanDuoc(viTri)
            .Where(u => assigneeHienTai == null || u.Id != assigneeHienTai)
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (ungVienIds.Count == 0)
        {
            return KetQua<GoiYResponse>.ThatBai(
                "Không có ai trong phạm vi của bạn để giao việc.", MaLoiChung.LoiNghiepVu);
        }

        // --- 3. Suy luận và xếp hạng ---
        var dauVao = await NapDuLieuGoiY.NapAsync(
            _db, noiDung, ungVienIds, moc: null, boQuaTaskId: yeuCau.TaskId, kyNangCoSan, ct);
        var kq = await _boXepHang.ChayAsync(dauVao, chiDungTfIdf: false, ct);

        // --- 4. Dịch sang dạng người đọc được ---
        var soLuong = Math.Clamp(yeuCau.SoLuong ?? _cauHinh.SoUngVienMacDinh, 1, 50);
        var ungVien = kq.UngVien.Take(soLuong).Select((x, i) => ChuyenDoi(x, i + 1)).ToList();

        dongHo.Stop();
        _log.LogInformation(
            "Gợi ý cho \"{TieuDe}\" bằng {PhuongPhap}: phòng {KetLuan}, xét {SoXet}/{SoPhamVi} người " +
            "trong {Ms}ms, dẫn đầu là {Top} ({Diem})",
            tieuDe, kq.PhuongPhap, kq.SuyLuan.KetLuan, kq.UngVien.Count, kq.SoTrongPhamVi,
            dongHo.ElapsedMilliseconds, ungVien.FirstOrDefault()?.FullName, ungVien.FirstOrDefault()?.Diem);

        return KetQua<GoiYResponse>.Ok(new GoiYResponse
        {
            PhienBanTrongSo = _cauHinh.PhienBan,
            PhuongPhap = kq.PhuongPhap,
            NoiDungDaDung = noiDung,
            SoUngVienTrongPhamVi = kq.SoTrongPhamVi,
            SoUngVienDaXet = kq.UngVien.Count,
            SuyLuanPhongBan = ChuyenDoi(kq.SuyLuan),
            KyNangYeuCau = kq.SuyLuan.KyNang.Select(k => new KyNangYeuCauDto
            {
                SkillId = k.SkillId, Code = k.Code, Ten = k.Ten, MucYeuCau = k.Muc, Nguon = k.Nguon, DoKhop = k.DoKhop
            }).ToList(),
            CanhBao = SinhCanhBao(kq),
            UngVien = ungVien,
            ThoiGianMs = dongHo.ElapsedMilliseconds
        });
    }

    /// <summary>
    /// Chỉ đoán phòng, nhóm và kỹ năng cho một nội dung — không chấm ai. Dùng khi tạo nhiệm vụ chưa
    /// giao, để gắn sẵn phòng thực thi thay vì bắt người giao tự chọn.
    /// </summary>
    public async Task<KetQuaXepHang> SuyLuanAsync(string noiDung, CancellationToken ct = default)
    {
        var dauVao = await NapDuLieuGoiY.NapAsync(
            _db, noiDung, Array.Empty<long>(), moc: null, boQuaTaskId: null, kyNangCoSan: null, ct);
        return await _boXepHang.ChayAsync(dauVao, chiDungTfIdf: false, ct);
    }

    // =====================================================================
    // DỊCH KẾT QUẢ
    // =====================================================================

    private UngVienDto ChuyenDoi(KetQuaUngVien x, int thuHang)
    {
        var c = _cauHinh;
        var d = x.Diem;

        var khop = x.DoiChieuKyNang.Where(k => k.MucCo.HasValue)
            .Select(k => $"{k.KyNang.Ten} {k.MucCo}/5" + (k.KyNang.Muc is { } can ? $" (cần {can})" : string.Empty))
            .ToList();
        var thieu = x.DoiChieuKyNang.Where(k => !k.MucCo.HasValue).Select(k => k.KyNang.Ten).ToList();

        return new UngVienDto
        {
            UserId = x.HoSo.UserId,
            FullName = x.HoSo.FullName,
            ChucDanh = x.HoSo.ChucDanh,
            TenPhongBan = x.HoSo.TenPhong,
            TenNhom = x.HoSo.TenNhom,
            Diem = x.Tong,
            ThuHang = thuHang,
            ChiTietDiem = new ChiTietDiem
            {
                NguNghia = ThanhPhan(d.NguNghia, c.TrongSoNguNghia),
                MucKyNang = ThanhPhan(d.MucKyNang, c.TrongSoMucKyNang),
                HieuSuat = ThanhPhan(d.HieuSuat, c.TrongSoHieuSuat),
                ViecTuongTu = ThanhPhan(d.ViecTuongTu, c.TrongSoViecTuongTu),
                DungHan = ThanhPhan(d.DungHan, c.TrongSoDungHan),
                KhoiLuong = ThanhPhan(d.KhoiLuong, c.TrongSoKhoiLuong),
                ThamNien = ThanhPhan(d.ThamNien, c.TrongSoThamNien)
            },
            SoLieu = new SoLieuUngVien
            {
                SoNhiemVuHoanThanh = x.SoHoanThanh,
                SoNhiemVuDungHan = x.SoDungHan,
                SoNhiemVuDangLam = x.HoSo.SoDangLam,
                TaiHienTai = x.HoSo.TaiHienTai,
                SoNamLamViec = Math.Round(x.HoSo.SoNamLamViec, 1),
                ChatLuongTrungBinh = x.ChatLuongTrungBinh is { } cl ? Math.Round(cl, 2) : null,
                ChuaCoLichSu = x.SoHoanThanh == 0,
                KyNangKhop = khop,
                KyNangThieu = thieu,
                ViecTuongTu = x.ViecGanNhat.Select(v => new ViecTuongTuDto
                {
                    TaskId = v.Viec.TaskId,
                    TieuDe = v.Viec.TieuDe,
                    DoGan = Math.Round(v.DoGan, 4),
                    ChatLuong = v.Viec.ChatLuong
                }).ToList()
            },
            LyDo = SinhLyDo(x, khop, thieu)
        };
    }

    private static ThanhPhanDiem ThanhPhan(double diem, double trongSo) => new()
    {
        Diem = Math.Round(diem, 4),
        TrongSo = trongSo,
        DongGop = BoXepHang.DongGop(diem, trongSo)
    };

    private static SuyLuanPhongBanDto ChuyenDoi(KetQuaSuyLuan s)
    {
        static DiemDonViDto DonVi(DiemDonVi x) => new()
        {
            Id = x.DonVi.Id,
            Ten = x.DonVi.Ten,
            Diem = Math.Round(x.Diem, 4),
            DiemHoSo = Math.Round(x.DiemHoSo, 4),
            DiemLichSu = x.DiemLichSu is { } ls ? Math.Round(ls, 4) : null
        };

        var (ma, moTa) = s.KetLuan switch
        {
            KetLuanPhongBan.ChacChan => ("CHAC_CHAN", $"Nhiệm vụ thuộc {s.CacPhong[0].DonVi.Ten}."),
            KetLuanPhongBan.LuongLu => ("LUONG_LU",
                $"Nhiệm vụ có thể thuộc {s.CacPhong[0].DonVi.Ten} hoặc {s.CacPhong[1].DonVi.Ten}."),
            _ => ("KHONG_RO", "Không xác định được nhiệm vụ thuộc phòng nào.")
        };

        return new SuyLuanPhongBanDto
        {
            KetLuan = ma,
            MoTa = moTa,
            CacPhong = s.CacPhong.Select(DonVi).ToList(),
            PhongDaChon = s.PhongDaChon,
            Nhom = s.Nhom is null ? null : DonVi(s.Nhom)
        };
    }

    /// <summary>
    /// Sinh lý do theo mẫu câu cố định.
    ///
    /// <para>
    /// Cố tình không dùng mô hình ngôn ngữ: mẫu cố định thì kết quả ổn định, tái lập được khi bảo
    /// vệ, không tốn chi phí và không bao giờ bịa ra số liệu không có thật.
    /// </para>
    /// </summary>
    private List<string> SinhLyDo(KetQuaUngVien x, List<string> khop, List<string> thieu)
    {
        var lyDo = new List<string>();
        var d = x.Diem;

        lyDo.Add(d.NguNghia switch
        {
            >= 0.7 => $"Hồ sơ rất sát với nội dung nhiệm vụ (độ khớp {d.NguNghia:0.00}).",
            >= 0.4 => $"Hồ sơ khá phù hợp với nội dung nhiệm vụ (độ khớp {d.NguNghia:0.00}).",
            _ => $"Hồ sơ ít liên quan tới nội dung nhiệm vụ (độ khớp {d.NguNghia:0.00})."
        });

        if (khop.Count > 0) lyDo.Add($"Có kỹ năng: {string.Join(", ", khop.Take(3))}.");
        if (thieu.Count > 0) lyDo.Add($"Chưa có kỹ năng: {string.Join(", ", thieu.Take(3))}.");

        if (x.ViecGanNhat.Count > 0 && x.ViecGanNhat[0].DoGan >= 0.5)
        {
            var viec = x.ViecGanNhat[0].Viec;
            lyDo.Add($"Từng làm việc tương tự: \"{viec.TieuDe}\"" +
                     (viec.ChatLuong is { } q ? $", được chấm {q}/5." : "."));
        }

        var thamNien = x.HoSo.SoNamLamViec >= 0.1
            ? $"Thâm niên {x.HoSo.SoNamLamViec:0.#} năm. "
            : "Vừa vào công ty. ";

        if (x.SoHoanThanh == 0)
        {
            lyDo.Add(thamNien + "Chưa hoàn thành việc nào — hiệu suất và đúng hạn đang dùng giá trị mặc định.");
        }
        else
        {
            var tyLe = (int)Math.Round(100.0 * x.SoDungHan / x.SoHoanThanh);
            lyDo.Add(thamNien + $"Đã hoàn thành {x.SoHoanThanh} việc, đúng hạn {tyLe}%" +
                     (x.ChatLuongTrungBinh is { } cl ? $", chất lượng trung bình {cl:0.0}/5." : "."));
        }

        lyDo.Add(x.HoSo.SoDangLam == 0
            ? "Hiện không giữ việc nào, nhận được ngay."
            : $"Đang giữ {x.HoSo.SoDangLam} việc (tải {x.HoSo.TaiHienTai:0.#}/{_cauHinh.NguongKhoiLuong:0.#})" +
              (d.KhoiLuong <= 0 ? " — đã đầy tải." : "."));

        return lyDo;
    }

    private static List<string> SinhCanhBao(KetQuaXepHang kq)
    {
        var canhBao = new List<string>();
        var s = kq.SuyLuan;

        if (kq.DaLuiVeTfIdf)
        {
            canhBao.Add("Dịch vụ AI nhúng ngữ nghĩa không phản hồi — đang dùng TF-IDF làm phương án dự phòng, " +
                        "kết quả kém chính xác hơn. Kiểm tra dịch vụ ở thư mục BackEnd/ai.");
        }

        if (s.KetLuan == KetLuanPhongBan.KhongRo && s.CacPhong.Count > 0)
        {
            canhBao.Add($"Không xác định được nhiệm vụ thuộc phòng nào (khớp nhất là {s.CacPhong[0].DonVi.Ten} " +
                        $"nhưng chỉ đạt {s.CacPhong[0].Diem:0.00}). Đang xét mọi người trong phạm vi của bạn.");
        }
        else if (s.KetLuan == KetLuanPhongBan.LuongLu)
        {
            canhBao.Add($"Nhiệm vụ có thể thuộc {s.CacPhong[0].DonVi.Ten} hoặc {s.CacPhong[1].DonVi.Ten} — " +
                        "đang xét ứng viên của cả hai phòng.");
        }

        if (kq.PhongNgoaiPhamVi)
        {
            canhBao.Add($"AI đoán nhiệm vụ thuộc {s.CacPhong[0].DonVi.Ten}, nhưng trong phạm vi giao việc của bạn " +
                        "không có ai ở phòng đó. Đang xét toàn bộ người bạn giao được — nên cân nhắc chuyển " +
                        "nhiệm vụ cho phòng phù hợp.");
        }

        if (kq.UngVien.Count > 0)
        {
            var khongAiCo = s.KyNang
                .Where(k => kq.UngVien.All(u => !u.HoSo.MucKyNang.ContainsKey(k.SkillId)))
                .Select(k => k.Ten)
                .ToList();
            if (khongAiCo.Count > 0)
                canhBao.Add($"Không ai trong danh sách có kỹ năng: {string.Join(", ", khongAiCo)}.");

            if (kq.UngVien.Max(u => u.Diem.NguNghia) < 0.2)
            {
                canhBao.Add("Không ai có hồ sơ khớp rõ với nội dung nhiệm vụ. Thứ hạng dưới đây chủ yếu dựa trên " +
                            "hiệu suất và khối lượng việc, không phải chuyên môn.");
            }
        }

        return canhBao;
    }
}
