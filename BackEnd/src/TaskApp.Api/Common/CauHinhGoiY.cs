namespace TaskApp.Api.Common;

/// <summary>
/// Bộ trọng số và tham số của mô hình gợi ý người thực hiện.
///
/// <para>
/// Tách ra thành lớp riêng để chỉnh được mà không phải sửa thuật toán, và để khi bảo vệ
/// có thể chỉ thẳng vào từng con số mà giải thích. Đọc từ mục <c>GoiY</c> trong cấu hình.
/// </para>
/// </summary>
public sealed class CauHinhGoiY
{
    public const string Muc = "GoiY";

    /// <summary>Trọng số độ khớp kỹ năng (TF-IDF + cosine). Cao nhất vì đây là căn cứ trực tiếp nhất.</summary>
    public double TrongSoKyNang { get; set; } = 0.40;

    /// <summary>Trọng số kinh nghiệm — tổng số nhiệm vụ đã hoàn thành.</summary>
    public double TrongSoKinhNghiem { get; set; } = 0.20;

    /// <summary>Trọng số tỷ lệ đúng hạn.</summary>
    public double TrongSoDungHan { get; set; } = 0.25;

    /// <summary>Trọng số mức độ rảnh — càng ít việc đang gánh thì càng cao.</summary>
    public double TrongSoKhoiLuong { get; set; } = 0.15;

    /// <summary>
    /// Số nhiệm vụ hoàn thành để đạt điểm kinh nghiệm tối đa.
    /// Thang log nên 10 việc đạt 1,0 còn 3 việc đã được khoảng 0,6 — người mới vẫn có cơ hội.
    /// </summary>
    public int NguongKinhNghiem { get; set; } = 10;

    /// <summary>
    /// Số việc đang mở coi là đã đầy tải. Vượt ngưỡng này thì điểm khối lượng bằng 0.
    /// Tính theo tổng trọng số ưu tiên chứ không phải đếm đầu việc.
    /// </summary>
    public double NguongKhoiLuong { get; set; } = 8.0;

    /// <summary>
    /// Tỷ lệ đúng hạn giả định cho người chưa có lịch sử (làm mượt Laplace).
    /// Không cho 0 vì như thế người mới vĩnh viễn không bao giờ được gợi ý.
    /// </summary>
    public double TyLeDungHanTienNghiem { get; set; } = 0.70;

    /// <summary>
    /// Số quan sát ảo của làm mượt Laplace. Càng lớn thì người ít dữ liệu càng bị kéo về
    /// giá trị tiên nghiệm, tránh việc làm đúng hạn 1/1 việc đã được điểm tuyệt đối.
    /// </summary>
    public double SoQuanSatAo { get; set; } = 5.0;

    /// <summary>Số ứng viên trả về mặc định.</summary>
    public int SoUngVienMacDinh { get; set; } = 5;

    /// <summary>
    /// Ngưỡng điểm cosine để coi là "thật sự khớp kỹ năng".
    ///
    /// <para>
    /// Dưới ngưỡng này thường chỉ là trùng vài từ vụn trong mô tả kỹ năng chứ không phải
    /// liên quan chuyên môn thật. Ví dụ nhiệm vụ "Chuẩn bị tiệc tất niên" vẫn được điểm
    /// khoảng 0,1 với một hồ sơ bất kỳ chỉ vì chung một hai từ thông dụng.
    /// </para>
    /// <para>
    /// Ngưỡng chỉ dùng khi <b>viết lý do</b> và khi quyết định có cảnh báo hay không —
    /// không cắt điểm, vì cắt sẽ làm mất thông tin xếp hạng giữa các ứng viên đều khớp yếu.
    /// </para>
    /// </summary>
    public double NguongKhopKyNang { get; set; } = 0.15;

    /// <summary>Phiên bản bộ trọng số, ghi kèm kết quả để đối chiếu khi hiệu chỉnh.</summary>
    public string PhienBan { get; set; } = "v1.0";

    /// <summary>Tổng bốn trọng số. Phải bằng 1 để điểm tổng nằm trong [0, 1].</summary>
    public double TongTrongSo =>
        TrongSoKyNang + TrongSoKinhNghiem + TrongSoDungHan + TrongSoKhoiLuong;

    /// <summary>
    /// Kiểm cấu hình. Tổng trọng số lệch khỏi 1 sẽ khiến điểm tổng vượt 1 hoặc không bao giờ
    /// đạt 1, làm mọi so sánh và ngưỡng phía sau sai theo.
    /// </summary>
    public void KiemTra()
    {
        if (Math.Abs(TongTrongSo - 1.0) > 0.001)
        {
            throw new InvalidOperationException(
                $"Tổng trọng số của mô hình gợi ý phải bằng 1,00 nhưng đang là {TongTrongSo:0.###}. " +
                "Kiểm tra lại mục 'GoiY' trong cấu hình.");
        }
    }
}
