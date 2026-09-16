namespace TaskApp.Api.Common;

/// <summary>
/// Bộ trọng số và tham số của mô hình gợi ý người thực hiện — phiên bản 2.
///
/// <code>
/// Điểm = 0,35 × NgữNghĩa + 0,15 × MứcKỹNăng + 0,15 × HiệuSuất
///      + 0,05 × ViệcTươngTự + 0,10 × ĐúngHạn + 0,10 × KhốiLượng + 0,10 × ThâmNiên
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
    public double TrongSoNguNghia { get; set; } = 0.35;

    /// <summary>Có đủ mức ở những kỹ năng nhiệm vụ đòi hỏi hay không.</summary>
    public double TrongSoMucKyNang { get; set; } = 0.15;

    /// <summary>Điểm đánh giá chất lượng các việc đã hoàn thành.</summary>
    public double TrongSoHieuSuat { get; set; } = 0.15;

    /// <summary>
    /// Đã làm những việc giống việc này chưa, và làm tốt tới đâu.
    ///
    /// <para>
    /// Đây là chỗ nhường 0,05 cho thâm niên, chứ không phải ngữ nghĩa. Hai thành phần này trùng
    /// nhau khá nhiều — cùng đo độ gần nghĩa, chỉ khác là một bên so với hồ sơ người, một bên so
    /// với các việc người đó đã làm. Đo trên tập kiểm tra: hạ từ 0,10 xuống 0,05 để lấy chỗ cho
    /// thâm niên thì mọi chỉ tiêu của mô hình nhúng giữ nguyên (Top-1 0,6667 · MRR 0,7886), còn
    /// nếu bớt của ngữ nghĩa thì mất một việc (Top-1 0,6111 · MRR 0,7608).
    /// </para>
    /// </summary>
    public double TrongSoViecTuongTu { get; set; } = 0.05;

    /// <summary>Tỷ lệ hoàn thành đúng hạn.</summary>
    public double TrongSoDungHan { get; set; } = 0.10;

    /// <summary>Càng ít việc đang gánh càng cao — để việc được san đều, không dồn vào người giỏi nhất.</summary>
    public double TrongSoKhoiLuong { get; set; } = 0.10;

    /// <summary>
    /// Thâm niên — số năm đã làm ở công ty.
    ///
    /// <para>
    /// Trọng số cố ý nhỏ hơn nhóm hiệu suất (0,15 hiệu suất + 0,10 đúng hạn): người lâu năm được
    /// cộng thêm so với người vừa vào chưa có kinh nghiệm, nhưng người 1–2 năm làm tốt vẫn phải
    /// hơn người 3–4 năm làm không tốt. Quan hệ này được ép cứng trong <see cref="KiemTra"/>.
    /// </para>
    /// <para>
    /// Chọn 0,10 sau khi quét 0 / 0,05 / 0,10 / 0,15 trên tập kiểm tra: dưới 0,10 thì người vừa
    /// vào chưa làm việc nào vẫn xếp TRÊN người sáu năm làm đúng hạn 100% ở ca #55; trên 0,10 thì
    /// không đổi được thêm ca nào nữa mà chỉ bào mòn các thành phần khác.
    /// </para>
    /// </summary>
    public double TrongSoThamNien { get; set; } = 0.10;

    // ------------------------------------------------------------------ người ít dữ liệu

    /// <summary>
    /// Hiệu suất giả định khi chưa có đánh giá nào — 0,35, tức DƯỚI giữa thang điểm.
    ///
    /// <para>
    /// Không cho 0: nếu vậy người mới vĩnh viễn không lọt vào gợi ý, và vĩnh viễn không có cơ hội
    /// có dữ liệu. Nhưng để dưới giữa thang, vì chưa chứng minh được gì thì chưa được coi ngang
    /// người đã có thành tích. Đây mới là đòn bẩy chính cho người vừa vào — thâm niên chỉ phụ.
    /// </para>
    /// <para>
    /// Hai giá trị này KHÔNG ảnh hưởng quy tắc "1–2 năm làm tốt hơn 3–4 năm làm kém": cả hai người
    /// đó đều đã có lịch sử nên không dùng tới tiên nghiệm. Đo trên tập kiểm tra cũng cho đúng một
    /// kết quả ở cả 0,50/0,60 lẫn 0,35/0,45, vì người chưa có lịch sử chưa từng là đáp án đúng.
    /// </para>
    /// </summary>
    public double HieuSuatTienNghiem { get; set; } = 0.35;

    /// <summary>Tỷ lệ đúng hạn giả định khi chưa có lịch sử. Cùng lý lẽ như trên.</summary>
    public double TyLeDungHanTienNghiem { get; set; } = 0.45;

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

    /// <summary>
    /// Số năm làm việc để đạt điểm thâm niên tối đa; quá mốc này thì mọi người coi như ngang nhau.
    ///
    /// <para>
    /// Để 2 năm chứ không phải 5. Thang log vốn đã dốc ở đoạn đầu, nhưng với trần 5 năm thì người
    /// 1 năm (0,39) còn cách người 4 năm (0,90) tới 0,51 điểm — đúng chỗ quy tắc "1–2 năm làm tốt
    /// hơn 3–4 năm làm kém" dễ vỡ nhất. Hạ trần về 2 năm kéo khoảng cách đó xuống 0,37 mà hầu như
    /// không đụng tới khoảng cách người-mới ↔ cựu-binh (0,913 so với 0,947), nên biên an toàn của
    /// quy tắc 2 tăng từ 1,42 lần lên 1,96 lần trong khi quy tắc 1 giữ nguyên kết quả.
    /// </para>
    /// </summary>
    public double NamThamNienToiDa { get; set; } = 2.0;

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
        TrongSoViecTuongTu + TrongSoDungHan + TrongSoKhoiLuong + TrongSoThamNien;

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

        if (NamThamNienToiDa <= 0)
        {
            throw new InvalidOperationException("GoiY:NamThamNienToiDa phải lớn hơn 0.");
        }

        KiemTraQuyTacThamNien();

        Nhung.KiemTra("GoiY:Nhung");
        TfIdf.KiemTra("GoiY:TfIdf");
    }

    /// <summary>
    /// Ép cứng quy tắc nghiệp vụ: người 1 năm làm tốt phải hơn người 4 năm làm kém.
    ///
    /// <para>
    /// Đây là chỗ dễ hỏng âm thầm nhất của mô hình — chỉ cần ai đó nâng trọng số thâm niên hoặc
    /// nới trần số năm là thứ tự lật ngược mà không có lỗi nào báo ra, và phải soi từng ca gợi ý
    /// mới phát hiện. Nên kiểm ngay lúc khởi động, bằng đúng hai người của tình huống đề bài:
    /// </para>
    /// <list type="bullet">
    ///   <item>người 1 năm: chất lượng 0,85 · đúng hạn 0,90</item>
    ///   <item>người 4 năm: chất lượng 0,60 · đúng hạn 0,55</item>
    /// </list>
    /// <para>
    /// Mọi thành phần còn lại coi như ngang nhau, nên chỉ cần lợi thế hiệu suất của người làm tốt
    /// lớn hơn lợi thế thâm niên của người lâu năm.
    /// </para>
    /// </summary>
    private void KiemTraQuyTacThamNien()
    {
        double DiemThamNien(double soNam) =>
            Math.Min(1.0, Math.Log(1 + soNam) / Math.Log(1 + NamThamNienToiDa));

        var loiTheThamNien = TrongSoThamNien * (DiemThamNien(4) - DiemThamNien(1));
        var loiTheHieuSuat = TrongSoHieuSuat * (0.85 - 0.60) + TrongSoDungHan * (0.90 - 0.55);

        if (loiTheThamNien >= loiTheHieuSuat)
        {
            throw new InvalidOperationException(
                $"Bộ trọng số vi phạm quy tắc nghiệp vụ về thâm niên: người 4 năm làm kém đang được " +
                $"lợi {loiTheThamNien:0.####} nhờ thâm niên, trong khi người 1 năm làm tốt chỉ được lợi " +
                $"{loiTheHieuSuat:0.####} nhờ hiệu suất. Hạ GoiY:TrongSoThamNien " +
                $"(đang {TrongSoThamNien:0.##}) hoặc GoiY:NamThamNienToiDa (đang {NamThamNienToiDa:0.##}), " +
                "hoặc nâng GoiY:TrongSoHieuSuat / GoiY:TrongSoDungHan.");
        }
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
