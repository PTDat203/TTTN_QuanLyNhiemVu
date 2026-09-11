using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskApp.Api.Common;
using TaskApp.Api.Data;
using TaskApp.Api.Dtos;

namespace TaskApp.Api.Services;

/// <summary>
/// Gợi ý người thực hiện phù hợp — chức năng trọng tâm của đề tài.
///
/// <para><b>Cách chấm điểm.</b> Bốn đặc trưng, mỗi cái quy về [0, 1] rồi cộng có trọng số:</para>
/// <code>
/// Điểm = 0,40 × KỹNăng + 0,20 × KinhNghiệm + 0,25 × ĐúngHạn + 0,15 × KhốiLượng
/// </code>
/// <list type="bullet">
///   <item><b>Kỹ năng</b> — TF-IDF + cosine giữa nội dung nhiệm vụ và hồ sơ kỹ năng.</item>
///   <item><b>Kinh nghiệm</b> — số nhiệm vụ đã hoàn thành, thang log để người làm nhiều
///         không áp đảo tuyệt đối.</item>
///   <item><b>Đúng hạn</b> — tỷ lệ hoàn thành trước hạn, làm mượt Laplace.</item>
///   <item><b>Khối lượng</b> — càng ít việc đang gánh thì điểm càng cao, để san đều việc.</item>
/// </list>
///
/// <para>
/// <b>Giải thích được là yêu cầu bắt buộc, không phải điểm cộng.</b> Mỗi ứng viên trả về
/// kèm điểm từng thành phần, số liệu thô và lý do bằng tiếng Việt. Người giao phải hiểu
/// vì sao hệ thống đề xuất người này thì mới dám tin — và quyết định cuối cùng vẫn là của họ.
/// </para>
/// <para>
/// Không lưu bảng kết quả AI nào. Điểm tính tại thời điểm gọi, vì dữ liệu đầu vào
/// (khối lượng việc, lịch sử hoàn thành) thay đổi liên tục nên lưu lại là lỗi thời ngay.
/// </para>
/// </summary>
public sealed class GoiYService
{
    private readonly TaskDbContext _db;
    private readonly CauHinhGoiY _cauHinh;
    private readonly ILogger<GoiYService> _log;

    public GoiYService(TaskDbContext db, IOptions<CauHinhGoiY> cauHinh, ILogger<GoiYService> log)
    {
        _db = db;
        _cauHinh = cauHinh.Value;
        _cauHinh.KiemTra();
        _log = log;
    }

    /// <summary>Hồ sơ một ứng viên, gom sẵn từ CSDL để chấm điểm.</summary>
    private sealed class HoSo
    {
        public long UserId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public List<(string Ten, int Muc, string? MoTa)> KyNang { get; init; } = new();
        public int SoHoanThanh { get; set; }
        public int SoDungHan { get; set; }
        public int SoDangLam { get; set; }
        public double TaiHienTai { get; set; }
        public List<string> CacTu { get; set; } = new();
        public TfIdf.VanBanVector Vector { get; set; } = new();
    }

    /// <summary>
    /// Chấm điểm và xếp hạng ứng viên cho một nhiệm vụ.
    /// </summary>
    public async Task<KetQua<GoiYResponse>> GoiYAsync(
        GoiYRequest yeuCau, long nguoiGoiId, CancellationToken ct = default)
    {
        var dongHo = Stopwatch.StartNew();
        var canhBao = new List<string>();

        // --- 1. Xác định nội dung nhiệm vụ cần khớp ---
        string tieuDe = yeuCau.Title ?? string.Empty;
        string? moTa = yeuCau.Description;
        long? assigneeHienTai = null;

        if (yeuCau.TaskId.HasValue)
        {
            var nv = await _db.Tasks.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == yeuCau.TaskId.Value, ct);

            if (nv is null)
                return KetQua<GoiYResponse>.KhongTimThay($"Không tìm thấy nhiệm vụ #{yeuCau.TaskId}.");

            if (nv.CreatorId != nguoiGoiId)
                return KetQua<GoiYResponse>.KhongCoQuyen(
                    "Chỉ người tạo nhiệm vụ mới xem được gợi ý cho nhiệm vụ này.");

            tieuDe = nv.Title;
            moTa = nv.Description;
            assigneeHienTai = nv.AssigneeId;
        }

        var noiDung = $"{tieuDe} {moTa}".Trim();
        if (string.IsNullOrWhiteSpace(noiDung))
        {
            return KetQua<GoiYResponse>.DuLieuKhongHopLe(
                "Cần có tiêu đề hoặc mô tả nhiệm vụ để tìm người phù hợp.");
        }

        // --- 2. Lọc cứng: chỉ những người mà người gọi ĐƯỢC GIAO VIỆC cho ---
        // Dùng đúng quy tắc của nút "Giao" (PhamViToChuc), nên AI không bao giờ gợi ý một
        // người mà lúc giao thật backend lại từ chối. Trừ người đang giữ chính việc này.
        var viTriNguoiGoi = await PhamViToChuc.NapAsync(_db, nguoiGoiId, ct);
        if (viTriNguoiGoi is null)
            return KetQua<GoiYResponse>.KhongCoQuyen("Không xác định được người gọi.");

        var ungVien = await _db.Users.AsNoTracking()
            .NguoiNhanDuoc(viTriNguoiGoi)
            .Where(u => assigneeHienTai == null || u.Id != assigneeHienTai)
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);

        if (ungVien.Count == 0)
        {
            return KetQua<GoiYResponse>.ThatBai(
                "Không có ai trong phạm vi của bạn để giao việc.", MaLoiChung.LoiNghiepVu);
        }

        var hoSo = ungVien.ToDictionary(
            u => u.Id,
            u => new HoSo { UserId = u.Id, FullName = u.FullName });

        await NapKyNangAsync(hoSo, ct);
        await NapLichSuAsync(hoSo, ct);
        await NapKhoiLuongAsync(hoSo, ct);

        // --- 3. Vector hoá: dựng IDF trên chính tập ứng viên ---
        // IDF phải tính trên tập hồ sơ chứ không phải tập nhiệm vụ, vì mục tiêu là tìm từ
        // nào hiếm GIỮA CÁC ỨNG VIÊN — đó mới là từ giúp phân biệt người này với người kia.
        foreach (var hs in hoSo.Values)
        {
            hs.CacTu = TachTuHoSo(hs);
        }

        var idf = TfIdf.DungIdf(hoSo.Values.Select(h => h.CacTu).ToList());

        foreach (var hs in hoSo.Values)
        {
            hs.Vector = TfIdf.VectorHoa(hs.CacTu, idf);
        }

        var vectorNhiemVu = TfIdf.VectorHoa(XuLyVanBan.TachTu(noiDung), idf);

        // --- 4. Chấm điểm ---
        var ketQua = hoSo.Values
            .Select(hs => ChamDiem(hs, vectorNhiemVu, noiDung))
            .OrderByDescending(x => x.Diem)
            // Điểm bằng nhau thì ưu tiên người rảnh hơn, rồi đến tên cho thứ tự ổn định.
            .ThenBy(x => x.SoLieu.TaiHienTai)
            .ThenBy(x => x.FullName, StringComparer.CurrentCulture)
            .ToList();

        // Cảnh báo theo NGƯỠNG chứ không theo "bằng 0". Điểm cosine rất nhỏ (0,05–0,12)
        // hầu như luôn xuất hiện do trùng vài từ thông dụng, nên nếu chỉ cảnh báo khi tất cả
        // bằng đúng 0 thì gần như không bao giờ cảnh báo — và người giao sẽ tưởng gợi ý
        // dựa trên chuyên môn trong khi thực chất không phải.
        var diemKyNangCaoNhat = ketQua.Count > 0 ? ketQua.Max(x => x.ChiTietDiem.KyNang.Diem) : 0;
        if (diemKyNangCaoNhat < _cauHinh.NguongKhopKyNang)
        {
            canhBao.Add(
                "Không nhân viên nào có kỹ năng khai báo khớp rõ rệt với nội dung nhiệm vụ " +
                $"(độ khớp cao nhất chỉ {diemKyNangCaoNhat:0.00}). Thứ hạng dưới đây chủ yếu dựa trên " +
                "kinh nghiệm, tỷ lệ đúng hạn và khối lượng việc, không phải chuyên môn.");
        }

        var soLuong = Math.Clamp(yeuCau.SoLuong ?? _cauHinh.SoUngVienMacDinh, 1, 50);
        var topN = ketQua.Take(soLuong).ToList();
        for (var i = 0; i < topN.Count; i++) topN[i].ThuHang = i + 1;

        dongHo.Stop();
        _log.LogInformation(
            "Gợi ý cho \"{TieuDe}\": xét {SoUngVien} ứng viên trong {Ms}ms, dẫn đầu là {Top} ({Diem})",
            tieuDe, ungVien.Count, dongHo.ElapsedMilliseconds,
            topN.FirstOrDefault()?.FullName, topN.FirstOrDefault()?.Diem);

        return KetQua<GoiYResponse>.Ok(new GoiYResponse
        {
            PhienBanTrongSo = _cauHinh.PhienBan,
            NoiDungDaDung = noiDung,
            SoUngVienDaXet = ungVien.Count,
            CanhBao = canhBao,
            UngVien = topN,
            ThoiGianMs = dongHo.ElapsedMilliseconds
        });
    }

    // =====================================================================
    // NẠP DỮ LIỆU
    // =====================================================================

    private async Task NapKyNangAsync(Dictionary<long, HoSo> hoSo, CancellationToken ct)
    {
        var ds = await _db.UserSkills.AsNoTracking()
            .Where(s => hoSo.Keys.Contains(s.UserId))
            .Select(s => new { s.UserId, s.SkillName, s.SkillLevel, s.Description })
            .ToListAsync(ct);

        foreach (var s in ds)
        {
            if (hoSo.TryGetValue(s.UserId, out var hs))
            {
                hs.KyNang.Add((s.SkillName, s.SkillLevel ?? 1, s.Description));
            }
        }
    }

    /// <summary>
    /// Đếm số nhiệm vụ đã hoàn thành và số hoàn thành đúng hạn.
    ///
    /// <para>
    /// "Đúng hạn" xác định bằng thời điểm báo cáo được XÁC NHẬN so với hạn của nhiệm vụ,
    /// chứ không phải thời điểm gửi báo cáo. Gửi kịp mà kết quả không đạt, phải làm lại
    /// và mãi mới được duyệt thì không thể tính là đúng hạn.
    /// </para>
    /// </summary>
    private async Task NapLichSuAsync(Dictionary<long, HoSo> hoSo, CancellationToken ct)
    {
        var ds = await _db.Tasks.AsNoTracking()
            .Where(t => t.AssigneeId != null
                        && hoSo.Keys.Contains(t.AssigneeId.Value)
                        && t.StatusCode == TrangThaiNhiemVu.HoanThanh)
            .Select(t => new
            {
                UserId = t.AssigneeId!.Value,
                t.DueDate,
                NgayDuyet = t.Reports
                    .Where(r => r.Status == TrangThaiBaoCao.DaXacNhan)
                    .OrderByDescending(r => r.ReviewedAt)
                    .Select(r => r.ReviewedAt)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        foreach (var t in ds)
        {
            if (!hoSo.TryGetValue(t.UserId, out var hs)) continue;

            hs.SoHoanThanh++;

            // Không có hạn thì không thể trễ. Chưa có ngày duyệt thì không tính là đúng hạn.
            if (t.DueDate is null ||
                (t.NgayDuyet.HasValue && t.NgayDuyet.Value.Date <= t.DueDate.Value.Date))
            {
                hs.SoDungHan++;
            }
        }
    }

    private async Task NapKhoiLuongAsync(Dictionary<long, HoSo> hoSo, CancellationToken ct)
    {
        var ds = await _db.Tasks.AsNoTracking()
            .Where(t => t.AssigneeId != null
                        && hoSo.Keys.Contains(t.AssigneeId.Value)
                        && TrangThaiNhiemVu.DangXuLy.Contains(t.StatusCode))
            .Select(t => new { UserId = t.AssigneeId!.Value, t.Priority })
            .ToListAsync(ct);

        foreach (var t in ds)
        {
            if (!hoSo.TryGetValue(t.UserId, out var hs)) continue;

            hs.SoDangLam++;
            // Đếm theo trọng số ưu tiên: một việc HIGH nặng gấp ba một việc LOW,
            // nên đếm đầu việc suông sẽ đánh giá sai mức bận thật sự.
            hs.TaiHienTai += Math.Max(1, MucUuTien.TrongSo(t.Priority));
        }
    }

    // =====================================================================
    // CHẤM ĐIỂM
    // =====================================================================

    /// <summary>
    /// Ghép hồ sơ kỹ năng thành một "văn bản" để vector hoá.
    /// Kỹ năng mức càng cao thì lặp lại càng nhiều lần, nhờ đó TF của nó cao hơn —
    /// đây là cách đưa mức thành thạo vào mô hình mà không phải sửa công thức TF-IDF.
    /// </summary>
    private static List<string> TachTuHoSo(HoSo hs)
    {
        var cacTu = new List<string>();

        foreach (var (ten, muc, moTa) in hs.KyNang)
        {
            var tuKyNang = XuLyVanBan.TachTu(ten);
            var soLan = Math.Clamp(muc, 1, 5);

            for (var i = 0; i < soLan; i++) cacTu.AddRange(tuKyNang);

            // Mô tả chỉ tính một lần: nó là thông tin phụ, lặp lại sẽ lấn át tên kỹ năng.
            if (!string.IsNullOrWhiteSpace(moTa)) cacTu.AddRange(XuLyVanBan.TachTu(moTa));
        }

        return cacTu;
    }

    private UngVienDto ChamDiem(HoSo hs, TfIdf.VanBanVector vectorNhiemVu, string noiDung)
    {
        // --- Kỹ năng: cosine giữa nội dung nhiệm vụ và hồ sơ ---
        var diemKyNang = TfIdf.Cosine(vectorNhiemVu, hs.Vector);

        // --- Kinh nghiệm: thang log ---
        // Dùng log để chênh lệch giữa 0 và 3 việc lớn hơn hẳn giữa 20 và 23 việc:
        // vài việc đầu chứng minh được nhiều điều, việc thứ hai mươi thì gần như không.
        var diemKinhNghiem = Math.Min(1.0,
            Math.Log(1 + hs.SoHoanThanh) / Math.Log(1 + _cauHinh.NguongKinhNghiem));

        // --- Đúng hạn: làm mượt Laplace ---
        // Người mới chưa có việc nào sẽ nhận đúng giá trị tiên nghiệm thay vì 0 —
        // nếu để 0 thì họ không bao giờ lọt vào gợi ý, và mãi mãi không có cơ hội có dữ liệu.
        var diemDungHan =
            (hs.SoDungHan + _cauHinh.SoQuanSatAo * _cauHinh.TyLeDungHanTienNghiem) /
            (hs.SoHoanThanh + _cauHinh.SoQuanSatAo);

        // --- Khối lượng: càng rảnh càng cao ---
        var diemKhoiLuong = Math.Clamp(1.0 - hs.TaiHienTai / _cauHinh.NguongKhoiLuong, 0.0, 1.0);

        var chiTiet = new ChiTietDiem
        {
            KyNang = Tao(diemKyNang, _cauHinh.TrongSoKyNang),
            KinhNghiem = Tao(diemKinhNghiem, _cauHinh.TrongSoKinhNghiem),
            DungHan = Tao(diemDungHan, _cauHinh.TrongSoDungHan),
            KhoiLuong = Tao(diemKhoiLuong, _cauHinh.TrongSoKhoiLuong)
        };

        // Điểm tổng cộng từ chính các phần đóng góp đã làm tròn, để tổng bốn dòng hiển thị
        // luôn khớp con số tổng. Cộng từ giá trị thô rồi mới làm tròn thì hai bên có thể lệch
        // ở chữ số cuối, và người xem sẽ nghi ngờ toàn bộ kết quả.
        var tong = chiTiet.KyNang.DongGop + chiTiet.KinhNghiem.DongGop
                 + chiTiet.DungHan.DongGop + chiTiet.KhoiLuong.DongGop;

        var kyNangKhop = TimKyNangKhop(hs, noiDung);

        return new UngVienDto
        {
            UserId = hs.UserId,
            FullName = hs.FullName,
            Diem = Math.Round(tong, 4),
            ChiTietDiem = chiTiet,
            SoLieu = new SoLieuUngVien
            {
                SoNhiemVuHoanThanh = hs.SoHoanThanh,
                SoNhiemVuDungHan = hs.SoDungHan,
                SoNhiemVuDangLam = hs.SoDangLam,
                TaiHienTai = hs.TaiHienTai,
                KyNangKhop = kyNangKhop.Select(k => $"{k.Ten} ({k.Muc}/5)").ToList()
            },
            LyDo = SinhLyDo(hs, chiTiet, kyNangKhop)
        };
    }

    private static ThanhPhanDiem Tao(double diem, double trongSo) => new()
    {
        Diem = Math.Round(diem, 4),
        TrongSo = trongSo,
        DongGop = Math.Round(diem * trongSo, 4)
    };

    /// <summary>
    /// Tìm những kỹ năng của ứng viên thực sự xuất hiện trong nội dung nhiệm vụ.
    /// Dùng để viết lý do cụ thể thay vì nói chung chung "phù hợp về kỹ năng".
    /// </summary>
    private static List<(string Ten, int Muc)> TimKyNangKhop(HoSo hs, string noiDung)
    {
        var tuNhiemVu = XuLyVanBan.TachTu(noiDung).ToHashSet(StringComparer.Ordinal);

        return hs.KyNang
            .Where(k => XuLyVanBan.TachTu(k.Ten).Any(t => tuNhiemVu.Contains(t)))
            .OrderByDescending(k => k.Muc)
            .Select(k => (k.Ten, k.Muc))
            .ToList();
    }

    /// <summary>
    /// Sinh lý do bằng tiếng Việt theo mẫu cố định.
    ///
    /// <para>
    /// Cố tình không dùng mô hình ngôn ngữ: mẫu cố định thì kết quả ổn định, tái lập được
    /// khi bảo vệ, không tốn chi phí gọi API và không bao giờ bịa ra số liệu không có thật.
    /// </para>
    /// </summary>
    private List<string> SinhLyDo(HoSo hs, ChiTietDiem ct, List<(string Ten, int Muc)> kyNangKhop)
    {
        var lyDo = new List<string>();

        if (kyNangKhop.Count > 0)
        {
            var ds = string.Join(", ", kyNangKhop.Take(3).Select(k => $"{k.Ten} ({k.Muc}/5)"));
            lyDo.Add($"Khớp kỹ năng: {ds}.");
        }
        else if (ct.KyNang.Diem >= _cauHinh.NguongKhopKyNang)
        {
            // Không có kỹ năng nào trùng tên trực tiếp nhưng cosine vẫn đủ cao — thường là
            // khớp qua phần mô tả kỹ năng.
            lyDo.Add("Hồ sơ kỹ năng có liên quan tới nội dung nhiệm vụ.");
        }
        else
        {
            // Điểm cosine khác 0 nhưng dưới ngưỡng thì chỉ là trùng từ vụn, không phải
            // liên quan chuyên môn. Nói "có liên quan một phần" ở đây là gây hiểu nhầm.
            lyDo.Add("Chưa khai kỹ năng nào khớp với nội dung nhiệm vụ này.");
        }

        lyDo.Add(hs.SoHoanThanh == 0
            ? "Chưa hoàn thành nhiệm vụ nào — chưa có dữ liệu lịch sử để đánh giá."
            : $"Đã hoàn thành {hs.SoHoanThanh} nhiệm vụ.");

        if (hs.SoHoanThanh > 0)
        {
            var tyLe = (int)Math.Round(100.0 * hs.SoDungHan / hs.SoHoanThanh);
            lyDo.Add($"Tỷ lệ đúng hạn {tyLe}% ({hs.SoDungHan}/{hs.SoHoanThanh}).");
        }

        lyDo.Add(hs.SoDangLam == 0
            ? "Hiện không giữ nhiệm vụ nào, có thể nhận việc ngay."
            : $"Đang giữ {hs.SoDangLam} nhiệm vụ " +
              $"(tải {hs.TaiHienTai:0.#}/{_cauHinh.NguongKhoiLuong:0.#}).");

        return lyDo;
    }
}
