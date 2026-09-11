namespace TaskApp.Api.Common;

/// <summary>
/// Bộ trọng số và tham số của mô hình gợi ý người thực hiện — phiên bản 2.
///
/// <code>
/// Điểm = 0,40 × NgữNghĩa + 0,15 × MứcKỹNăng + 0,15 × HiệuSuất
///      + 0,10 × ViệcTươngTự + 0,10 × ĐúngHạn + 0,10 × KhốiLượng
/// </code>
///
/// <para>
/// Đọc từ mục <c>GoiY</c> trong cấu hình. Tách thành lớp riêng để chỉnh được mà không phải sửa
/// thuật toán, và để khi bảo vệ có thể chỉ thẳng vào từng con số mà giải thích.
/// </para>
/// </summary>
public sealed class CauHinhGoiY
{
    public const string Muc = "GoiY";

    // ------------------------------------------------------------------ trọng số, tổng = 1

    /// <summary>Độ khớp ngữ nghĩa giữa nội dung nhiệm vụ và hồ sơ người. Cao nhất vì là căn cứ trực tiếp nhất.</summary>
    public double TrongSoNguNghia { get; set; } = 0.40;

    /// <summary>Có đủ mức ở những kỹ năng nhiệm vụ đòi hỏi hay không.</summary>
    public double TrongSoMucKyNang { get; set; } = 0.15;

    /// <summary>Điểm đánh giá chất lượng các việc đã hoàn thành.</summary>
    public double TrongSoHieuSuat { get; set; } = 0.15;

    /// <summary>Đã làm những việc giống việc này chưa, và làm tốt tới đâu.</summary>
    public double TrongSoViecTuongTu { get; set; } = 0.10;

    /// <summary>Tỷ lệ hoàn thành đúng hạn.</summary>
    public double TrongSoDungHan { get; set; } = 0.10;

    /// <summary>Càng ít việc đang gánh càng cao — để việc được san đều, không dồn vào người giỏi nhất.</summary>
    public double TrongSoKhoiLuong { get; set; } = 0.10;

    // ------------------------------------------------------------------ người ít dữ liệu

    /// <summary>
    /// Hiệu suất giả định khi chưa có đánh giá nào — tương đương 3,6/5. Không cho 0: nếu vậy người
    /// mới vĩnh viễn không lọt vào gợi ý, và vĩnh viễn không có cơ hội có dữ liệu.
    /// </summary>
    public double HieuSuatTienNghiem { get; set; } = 0.65;

    /// <summary>Tỷ lệ đúng hạn giả định khi chưa có lịch sử.</summary>
    public double TyLeDungHanTienNghiem { get; set; } = 0.70;

    /// <summary>
    /// Số quan sát ảo khi làm mượt Laplace. Càng lớn thì người ít dữ liệu càng bị kéo về giá trị
    /// tiên nghiệm — tránh chuyện đúng hạn 1/1 việc đã được điểm tuyệt đối.
    /// </summary>
    public double SoQuanSatAo { get; set; } = 5.0;

    // ------------------------------------------------------------------ tham số khác

    /// <summary>Tổng trọng số ưu tiên (HIGH 3, MEDIUM 2, LOW 1) coi là đầy tải — điểm khối lượng về 0.</summary>
    public double NguongKhoiLuong { get; set; } = 8.0;

    /// <summary>Số việc gần nhất đem ra so, cả khi chấm "việc tương tự" lẫn khi chấm phòng ban.</summary>
    public int SoViecTuongTu { get; set; } = 3;

    /// <summary>Mức kỳ vọng khi nhiệm vụ đòi một kỹ năng mà không nói mức. Mức 3 là "tự làm độc lập được".</summary>
    public int MucKyNangMacDinh { get; set; } = 3;

    /// <summary>Số kỹ năng tối đa AI được trích ra từ một nhiệm vụ.</summary>
    public int SoKyNangTrichToiDa { get; set; } = 4;

    /// <summary>Số ứng viên trả về mặc định.</summary>
    public int SoUngVienMacDinh { get; set; } = 5;

    // ------------------------------------------------------------------ hiệu chỉnh riêng từng phương pháp

    /// <summary>Bộ ngưỡng của mô hình nhúng. Giá trị thật hiệu chỉnh bằng dữ liệu, đặt trong appsettings.json.</summary>
    public HieuChinhPhuongPhap Nhung { get; set; } = new()
    {
        BatDoiXung = new NguongChuanHoa { Thap = 0.78, Cao = 0.88 },
        DoiXung = new NguongChuanHoa { Thap = 0.82, Cao = 0.94 }
    };

    /// <summary>Bộ ngưỡng của TF-IDF.</summary>
    public HieuChinhPhuongPhap TfIdf { get; set; } = new()
    {
        BatDoiXung = new NguongChuanHoa { Thap = 0.00, Cao = 0.35 },
        DoiXung = new NguongChuanHoa { Thap = 0.00, Cao = 0.40 }
    };

    /// <summary>Phiên bản bộ tham số, ghi kèm kết quả để đối chiếu khi hiệu chỉnh.</summary>
    public string PhienBan { get; set; } = "v2.0-chua-hieu-chinh";

    public double TongTrongSo =>
        TrongSoNguNghia + TrongSoMucKyNang + TrongSoHieuSuat +
        TrongSoViecTuongTu + TrongSoDungHan + TrongSoKhoiLuong;

    /// <summary>
    /// Kiểm cấu hình ngay khi khởi động. Tổng trọng số lệch khỏi 1 thì điểm tổng vượt 1 hoặc không
    /// bao giờ đạt 1, làm mọi so sánh và ngưỡng phía sau sai theo.
    /// </summary>
    public void KiemTra()
    {
        if (Math.Abs(TongTrongSo - 1.0) > 0.001)
        {
            throw new InvalidOperationException(
                $"Tổng trọng số của mô hình gợi ý phải bằng 1,00 nhưng đang là {TongTrongSo:0.###}. " +
                "Kiểm tra lại mục 'GoiY' trong cấu hình.");
        }

        Nhung.KiemTra("GoiY:Nhung");
        TfIdf.KiemTra("GoiY:TfIdf");
    }
}

/// <summary>
/// Bộ ngưỡng của MỘT phương pháp đo độ gần nghĩa.
///
/// <para>
/// Mỗi phương pháp một bộ riêng, vì thang điểm thô của chúng khác hẳn nhau: cosine của mô hình
/// nhúng dồn trong khoảng 0,8–0,9, cosine TF-IDF trải từ 0 tới 0,4. Dùng chung một bộ thì phương
/// pháp này đúng, phương pháp kia sai — và đem so hai phương pháp cũng không còn công bằng.
/// </para>
/// </summary>
public sealed class HieuChinhPhuongPhap
{
    /// <summary>Quy điểm thô về 0..1 khi so nhiệm vụ với hồ sơ / mô tả phòng / nhóm / kỹ năng.</summary>
    public NguongChuanHoa BatDoiXung { get; set; } = new() { Thap = 0, Cao = 1 };

    /// <summary>Quy điểm thô về 0..1 khi so nhiệm vụ với nhiệm vụ.</summary>
    public NguongChuanHoa DoiXung { get; set; } = new() { Thap = 0, Cao = 1 };

    /// <summary>Phòng đứng đầu dưới mức này (thang 0..1) thì coi là không xác định được phòng.</summary>
    public double PhongBanSan { get; set; } = 0.35;

    /// <summary>Phòng đứng đầu phải hơn phòng thứ hai ít nhất chừng này mới coi là chắc chắn.</summary>
    public double PhongBanCachBiet { get; set; } = 0.10;

    /// <summary>Kỹ năng khớp dưới mức này thì không coi là nhiệm vụ đòi hỏi.</summary>
    public double KyNangSan { get; set; } = 0.40;

    /// <summary>Chỉ lấy những kỹ năng không kém kỹ năng khớp nhất quá chừng này.</summary>
    public double KyNangKhoangCach { get; set; } = 0.15;

    public void KiemTra(string ten)
    {
        if (BatDoiXung.Cao <= BatDoiXung.Thap || DoiXung.Cao <= DoiXung.Thap)
            throw new InvalidOperationException($"Ngưỡng chuẩn hoá ở '{ten}': Cao phải lớn hơn Thap.");
    }
}

/// <summary>Cặp ngưỡng quy độ tương đồng thô về thang 0..1.</summary>
public sealed class NguongChuanHoa
{
    /// <summary>Điểm thô coi là "không liên quan" — quy về 0.</summary>
    public double Thap { get; set; }

    /// <summary>Điểm thô coi là "rất liên quan" — quy về 1.</summary>
    public double Cao { get; set; }

    public double ChuanHoa(double tho) => Math.Clamp((tho - Thap) / (Cao - Thap), 0.0, 1.0);
}
