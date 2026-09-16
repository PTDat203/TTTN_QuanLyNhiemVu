using Microsoft.Extensions.Options;
using TaskApp.Api.Common;
using TaskApp.Api.Entities;
using TaskApp.Api.Services.Ai;

namespace TaskApp.Api.Services.GoiY;

/// <summary>
/// Lõi của phần gợi ý người thực hiện: nhận dữ liệu đã nạp sẵn, trả về thứ hạng. Không đụng
/// database — nhờ vậy phần đánh giá chạy lại được đúng bộ này với dữ liệu nhìn từ quá khứ.
///
/// <para><b>Tầng 1 — lọc.</b> Phạm vi giao việc đã được lọc từ trước, ở <see cref="PhamViToChuc"/>.</para>
/// <list type="number">
///   <item>Đoán nhiệm vụ thuộc phòng nào: so nội dung với mô tả từng phòng, và với những việc
///         phòng đó từng làm.</item>
///   <item>Phòng đứng đầu vượt trội thì chỉ xét người phòng đó; hai phòng sát nhau thì xét cả
///         hai; không phòng nào khớp đủ thì xét tất cả. Không bao giờ trả về danh sách rỗng.</item>
///   <item>Trích kỹ năng nhiệm vụ đòi hỏi, nếu người giao chưa nhập.</item>
/// </list>
///
/// <para><b>Tầng 2 — xếp hạng</b> bên trong tập đã lọc:</para>
/// <code>
/// Điểm = 0,35 × NgữNghĩa + 0,15 × MứcKỹNăng + 0,15 × HiệuSuất
///      + 0,10 × ViệcTươngTự + 0,10 × ĐúngHạn + 0,10 × KhốiLượng + 0,05 × ThâmNiên
/// </code>
///
/// <para>
/// Bằng cấp KHÔNG phải điều kiện lọc. Nó chỉ góp một phần nhỏ qua đoạn hồ sơ đem so ngữ nghĩa,
/// nên người học Kinh tế nhưng làm Backend giỏi vẫn được xếp theo đúng năng lực thực tế.
/// </para>
/// </summary>
public sealed class BoXepHang
{
    private readonly DoTuongDongNhung _nhung;
    private readonly DoTuongDongTfIdf _tfIdf;
    private readonly CauHinhGoiY _cauHinh;

    public BoXepHang(DoTuongDongNhung nhung, DoTuongDongTfIdf tfIdf, IOptions<CauHinhGoiY> cauHinh)
    {
        _nhung = nhung;
        _tfIdf = tfIdf;
        _cauHinh = cauHinh.Value;
        _cauHinh.KiemTra();
    }

    /// <param name="dauVao">Dữ liệu đã nạp. Danh sách ứng viên rỗng thì chỉ chạy phần suy luận.</param>
    /// <param name="chiDungTfIdf">Bỏ qua mô hình nhúng — dùng khi đánh giá so sánh hai phương pháp.</param>
    /// <param name="ct">Huỷ giữa chừng khi người gọi ngắt kết nối.</param>
    public async Task<KetQuaXepHang> ChayAsync(DauVaoXepHang dauVao, bool chiDungTfIdf, CancellationToken ct)
    {
        if (chiDungTfIdf) return await ChayVoiAsync(dauVao, _tfIdf, ct);

        try
        {
            return await ChayVoiAsync(dauVao, _nhung, ct);
        }
        catch (MatDichVuNhungException)
        {
            // Chạy lại TOÀN BỘ bằng TF-IDF chứ không nối tiếp giữa chừng: trộn điểm của hai phương
            // pháp trong cùng một lần xếp hạng thì các con số không còn so được với nhau.
            var kq = await ChayVoiAsync(dauVao, _tfIdf, ct);
            kq.DaLuiVeTfIdf = true;
            return kq;
        }
    }

    /// <summary>
    /// Phần đóng góp vào điểm tổng, làm tròn 4 chữ số. Dùng chung cho cả phía tính tổng lẫn phía
    /// hiển thị, để các con số trên giao diện luôn cộng khớp đúng điểm tổng.
    /// </summary>
    public static double DongGop(double diem, double trongSo) => Math.Round(Math.Round(diem, 4) * trongSo, 4);

    // =====================================================================

    private async Task<KetQuaXepHang> ChayVoiAsync(DauVaoXepHang dv, DoTuongDong phuongPhap, CancellationToken ct)
    {
        async Task<double[]> SoVoi(IReadOnlyList<string> cacDoan, KieuSoSanh kieu)
            => cacDoan.Count == 0
                ? Array.Empty<double>()
                : await phuongPhap.SoVoiAsync(dv.NoiDung, cacDoan, kieu, ct) ?? throw new MatDichVuNhungException();

        // ---- 1. Độ gần giữa nhiệm vụ này và từng việc đã hoàn thành ----------------
        // Tính một lần, dùng cho cả việc đoán phòng ban lẫn điểm "việc tương tự" từng người.
        var diemLichSu = await SoVoi(dv.LichSu.Select(v => v.VanBan).ToList(), KieuSoSanh.DoiXung);
        var doGan = dv.LichSu.Select((v, i) => (Viec: v, DoGan: diemLichSu[i])).ToList();
        var doGanTheoViec = doGan.ToDictionary(x => x.Viec.TaskId, x => x.DoGan);

        // ---- 2. Tầng 1: đoán phòng ban ---------------------------------------------
        var suyLuan = new KetQuaSuyLuan();

        var diemPhong = await SoVoi(dv.PhongBan.Select(p => p.VanBan).ToList(), KieuSoSanh.BatDoiXung);
        suyLuan.CacPhong = dv.PhongBan
            .Select((p, i) => ChamDonVi(p, diemPhong[i],
                doGan.Where(x => x.Viec.DepartmentId == p.Id).Select(x => x.DoGan)))
            .OrderByDescending(x => x.Diem)
            .ToList();
        (suyLuan.KetLuan, suyLuan.PhongDaChon) = QuyetDinh(suyLuan.CacPhong, phuongPhap.HieuChinh);

        // Nhóm trong phòng đứng đầu: chỉ để tham khảo và để gắn nhóm phụ trách khi tạo nhiệm vụ.
        // Không dùng để lọc — người nhóm khác trong cùng phòng vẫn có thể là lựa chọn tốt.
        if (suyLuan.KetLuan != KetLuanPhongBan.KhongRo)
        {
            var phongDau = suyLuan.CacPhong[0].DonVi.Id;
            var nhom = dv.Nhom.Where(n => n.DepartmentId == phongDau).ToList();
            var diemNhom = await SoVoi(nhom.Select(n => n.VanBan).ToList(), KieuSoSanh.BatDoiXung);

            suyLuan.CacNhom = nhom
                .Select((n, i) => ChamDonVi(n, diemNhom[i],
                    doGan.Where(x => x.Viec.TeamId == n.Id).Select(x => x.DoGan)))
                .OrderByDescending(x => x.Diem)
                .ToList();

            if (suyLuan.CacNhom.Count > 0 && QuyetDinh(suyLuan.CacNhom, phuongPhap.HieuChinh).KetLuan == KetLuanPhongBan.ChacChan)
            {
                suyLuan.Nhom = suyLuan.CacNhom[0];
            }
        }

        // ---- 3. Kỹ năng yêu cầu -------------------------------------------------------
        if (dv.KyNangCoSan is { Count: > 0 } coSan)
        {
            suyLuan.KyNang = coSan;
        }
        else
        {
            var diemKyNang = await SoVoi(dv.DanhMucKyNang.Select(k => k.VanBan).ToList(), KieuSoSanh.BatDoiXung);
            var caoNhat = diemKyNang.DefaultIfEmpty(0).Max();
            suyLuan.DiemKyNang = dv.DanhMucKyNang.Select((k, i) => (k.Id, Diem: diemKyNang[i]))
                .ToDictionary(x => x.Id, x => x.Diem);

            suyLuan.KyNang = dv.DanhMucKyNang
                .Select((k, i) => (KyNang: k, Diem: diemKyNang[i]))
                .Where(x => x.Diem >= phuongPhap.HieuChinh.KyNangSan
                            && x.Diem >= caoNhat - phuongPhap.HieuChinh.KyNangKhoangCach)
                .OrderByDescending(x => x.Diem)
                .Take(_cauHinh.SoKyNangTrichToiDa)
                .Select(x => new KyNangCan
                {
                    SkillId = x.KyNang.Id,
                    Code = x.KyNang.Code,
                    Ten = x.KyNang.Ten,
                    Nguon = NguonKyNang.AI,
                    DoKhop = Math.Round(x.Diem, 4)
                })
                .ToList();
        }

        var kq = new KetQuaXepHang
        {
            PhuongPhap = phuongPhap.Ten,
            SuyLuan = suyLuan,
            SoTrongPhamVi = dv.UngVien.Count
        };

        // ---- 4. Lọc theo phòng — không bao giờ để trống --------------------------------
        var xet = dv.UngVien.ToList();
        if (suyLuan.KetLuan != KetLuanPhongBan.KhongRo)
        {
            var trongPhong = xet.Where(u => u.DepartmentId is { } d && suyLuan.PhongDaChon.Contains(d)).ToList();
            if (trongPhong.Count > 0) xet = trongPhong;
            else kq.PhongNgoaiPhamVi = xet.Count > 0;
        }

        // ---- 5. Tầng 2: chấm điểm -------------------------------------------------------
        var diemHoSo = await SoVoi(xet.Select(u => u.VanBanHoSo).ToList(), KieuSoSanh.BatDoiXung);

        // Khi tầng 1 kết luận KHÔNG RÕ thì không lọc ai — nhưng đừng vì thế mà vứt luôn tín hiệu
        // phòng ban. Nó yếu, không đủ để lọc cứng, song vẫn là thông tin phân biệt DUY NHẤT còn
        // lại: KHÔNG RÕ gần như luôn đi kèm việc chẳng hồ sơ cá nhân nào khớp, nên nếu bỏ nốt thì
        // thứ hạng chỉ còn do hiệu suất và khối lượng quyết định — và một hoá đơn tiền điện sẽ
        // được gợi ý cho trưởng nhóm Backend chỉ vì anh ta làm tốt và đang rảnh.
        //
        // Dùng làm SÀN chứ không cộng thêm: ai tự khớp tốt hơn phòng mình thì giữ nguyên điểm của
        // họ, ai không khớp gì thì ít nhất được hưởng độ khớp của phòng. Chỉ áp dụng ở nhánh
        // KHÔNG RÕ; hai nhánh kia đã lọc theo phòng rồi nên cộng vào chỉ là dịch cả cụm.
        var sanTheoPhong = suyLuan.KetLuan == KetLuanPhongBan.KhongRo
            ? suyLuan.CacPhong.ToDictionary(p => p.DonVi.Id, p => p.Diem)
            : null;

        kq.UngVien = xet
            .Select((u, i) => ChamDiem(u, NguNghiaCoSan(u, diemHoSo[i], sanTheoPhong),
                                       suyLuan.KyNang, doGanTheoViec))
            .OrderByDescending(x => x.Tong)
            // Điểm bằng nhau thì ưu tiên người rảnh hơn, rồi đến tên cho thứ tự ổn định.
            .ThenBy(x => x.HoSo.TaiHienTai)
            .ThenBy(x => x.HoSo.FullName, StringComparer.CurrentCulture)
            .ToList();

        return kq;
    }

    /// <summary>
    /// Điểm ngữ nghĩa của một ứng viên, có tính tới độ khớp của phòng họ khi tầng 1 không kết
    /// luận được. Trả về đúng <paramref name="diemHoSo"/> trong mọi trường hợp còn lại.
    /// </summary>
    private static double NguNghiaCoSan(
        HoSoUngVien u, double diemHoSo, IReadOnlyDictionary<long, double>? sanTheoPhong)
    {
        if (sanTheoPhong is null || u.DepartmentId is not { } phong) return diemHoSo;
        return sanTheoPhong.TryGetValue(phong, out var san) ? Math.Max(diemHoSo, san) : diemHoSo;
    }

    /// <summary>
    /// Điểm của một phòng hay một nhóm = trung bình của (a) độ khớp với đoạn mô tả đơn vị và
    /// (b) độ gần trung bình với vài việc gần nhất đơn vị đó từng làm.
    ///
    /// <para>
    /// (a) là cái đơn vị TỰ NÓI mình làm gì, (b) là cái đơn vị THỰC SỰ đã làm. Hai nguồn bù cho
    /// nhau: phòng mới chưa có lịch sử thì chỉ dựa vào (a); phòng có mô tả sơ sài thì (b) gánh.
    /// </para>
    /// </summary>
    private DiemDonVi ChamDonVi(HoSoDonVi donVi, double diemHoSo, IEnumerable<double> doGanLichSu)
    {
        var gan = doGanLichSu.OrderByDescending(x => x).Take(_cauHinh.SoViecTuongTu).ToList();
        double? diemLichSu = gan.Count == 0 ? null : gan.Average();

        return new DiemDonVi
        {
            DonVi = donVi,
            DiemHoSo = diemHoSo,
            DiemLichSu = diemLichSu,
            Diem = diemLichSu is { } ls ? (diemHoSo + ls) / 2 : diemHoSo
        };
    }

    /// <summary>Quyết định theo độ tin cậy: chắc chắn, lưỡng lự hay không rõ.</summary>
    private static (KetLuanPhongBan KetLuan, IReadOnlyList<long> DaChon) QuyetDinh(
        IReadOnlyList<DiemDonVi> xepHang, HieuChinhPhuongPhap nguong)
    {
        if (xepHang.Count == 0 || xepHang[0].Diem < nguong.PhongBanSan)
            return (KetLuanPhongBan.KhongRo, Array.Empty<long>());

        if (xepHang.Count == 1 || xepHang[0].Diem - xepHang[1].Diem >= nguong.PhongBanCachBiet)
            return (KetLuanPhongBan.ChacChan, new[] { xepHang[0].DonVi.Id });

        return (KetLuanPhongBan.LuongLu, new[] { xepHang[0].DonVi.Id, xepHang[1].DonVi.Id });
    }

    private KetQuaUngVien ChamDiem(
        HoSoUngVien u, double nguNghia, IReadOnlyList<KyNangCan> kyNangCan,
        IReadOnlyDictionary<long, double> doGanTheoViec)
    {
        var c = _cauHinh;

        // Mức kỹ năng: đủ mức yêu cầu thì 1, có một nửa mức thì 0,5, không có kỹ năng thì 0.
        // Nhiệm vụ không đòi kỹ năng cụ thể nào thì trung tính 0,5.
        var doiChieu = kyNangCan
            .Select(k => (KyNang: k, MucCo: u.MucKyNang.TryGetValue(k.SkillId, out var m) ? m : (int?)null))
            .ToList();
        var mucKyNang = doiChieu.Count == 0
            ? 0.5
            : doiChieu.Average(x => x.MucCo is { } co
                ? Math.Min(1.0, co / (double)Math.Max(1, x.KyNang.Muc ?? c.MucKyNangMacDinh))
                : 0.0);

        // Hiệu suất: điểm đánh giá trung bình, làm mượt Laplace về giá trị tiên nghiệm. Người mới
        // chưa có đánh giá nào nhận đúng giá trị tiên nghiệm — trung tính, không phải 0.
        var danhGia = u.ViecDaXong.Where(v => v.HieuSuat.HasValue).Select(v => v.HieuSuat!.Value).ToList();
        var hieuSuat = (danhGia.Sum() + c.SoQuanSatAo * c.HieuSuatTienNghiem) / (danhGia.Count + c.SoQuanSatAo);

        // Việc tương tự: đã làm những việc gần với việc này chưa, và làm tốt tới đâu.
        //   = mức liên quan × kết quả các việc gần + (1 − mức liên quan) × 0,5
        // Chưa làm gì giống thì trung tính 0,5; từng làm việc rất giống và làm tốt thì gần 1;
        // từng làm việc rất giống mà làm kém thì thấp.
        var gan = u.ViecDaXong
            .Select(v => (Viec: v, DoGan: doGanTheoViec.GetValueOrDefault(v.TaskId)))
            .Where(x => x.DoGan > 0)
            .OrderByDescending(x => x.DoGan)
            .Take(c.SoViecTuongTu)
            .ToList();
        var viecTuongTu = 0.5;
        if (gan.Count > 0)
        {
            var lienQuan = gan[0].DoGan;
            var ketQuaViecGan = gan.Sum(x => x.DoGan * (x.Viec.HieuSuat ?? c.HieuSuatTienNghiem))
                                / gan.Sum(x => x.DoGan);
            viecTuongTu = lienQuan * ketQuaViecGan + (1 - lienQuan) * 0.5;
        }

        // Đúng hạn: làm mượt Laplace như hiệu suất.
        var soXong = u.ViecDaXong.Count;
        var soDung = u.ViecDaXong.Count(v => v.DungHan);
        var dungHan = (soDung + c.SoQuanSatAo * c.TyLeDungHanTienNghiem) / (soXong + c.SoQuanSatAo);

        // Khối lượng: càng rảnh càng cao, đầy tải thì 0.
        var khoiLuong = Math.Clamp(1.0 - u.TaiHienTai / c.NguongKhoiLuong, 0.0, 1.0);

        // Thâm niên: người làm lâu được cộng thêm so với người vừa vào chưa có kinh nghiệm.
        // Thang log để một hai năm đầu đáng giá hơn hẳn năm thứ bảy, thứ tám. Trọng số nhỏ nên
        // không bao giờ lật ngược được chênh lệch về hiệu suất.
        var thamNien = Math.Min(1.0, Math.Log(1 + u.SoNamLamViec) / Math.Log(1 + c.NamThamNienToiDa));

        var diem = new DiemThanhPhan
        {
            NguNghia = nguNghia,
            MucKyNang = mucKyNang,
            HieuSuat = hieuSuat,
            ViecTuongTu = viecTuongTu,
            DungHan = dungHan,
            KhoiLuong = khoiLuong,
            ThamNien = thamNien
        };

        var chatLuong = u.ViecDaXong.Where(v => v.ChatLuong.HasValue).Select(v => (double)v.ChatLuong!.Value).ToList();

        return new KetQuaUngVien
        {
            HoSo = u,
            Diem = diem,
            Tong = Math.Round(
                DongGop(diem.NguNghia, c.TrongSoNguNghia) + DongGop(diem.MucKyNang, c.TrongSoMucKyNang) +
                DongGop(diem.HieuSuat, c.TrongSoHieuSuat) + DongGop(diem.ViecTuongTu, c.TrongSoViecTuongTu) +
                DongGop(diem.DungHan, c.TrongSoDungHan) + DongGop(diem.KhoiLuong, c.TrongSoKhoiLuong) +
                DongGop(diem.ThamNien, c.TrongSoThamNien), 4),
            SoHoanThanh = soXong,
            SoDungHan = soDung,
            ChatLuongTrungBinh = chatLuong.Count == 0 ? null : chatLuong.Average(),
            ViecGanNhat = gan,
            DoiChieuKyNang = doiChieu
        };
    }

    /// <summary>Dịch vụ nhúng không phản hồi giữa chừng — báo để chạy lại bằng TF-IDF.</summary>
    private sealed class MatDichVuNhungException : Exception;
}
