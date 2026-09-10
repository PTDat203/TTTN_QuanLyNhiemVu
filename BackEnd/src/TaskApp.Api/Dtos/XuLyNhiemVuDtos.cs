namespace TaskApp.Api.Dtos;

/// <summary>Ghi một lần cập nhật tiến độ.</summary>
public sealed class CapNhatTienDoRequest
{
    /// <summary>Phần trăm hoàn thành, 0..100. Trùng ràng buộc CK_TASK_PROGRESS_PERCENT.</summary>
    public int ProgressPercent { get; set; }

    /// <summary>Mô tả công việc đã làm. Tối đa 2000 ký tự.</summary>
    public string? Content { get; set; }
}

/// <summary>Gửi báo cáo kết quả để người giao xác nhận.</summary>
public sealed class GuiBaoCaoRequest
{
    /// <summary>Nội dung báo cáo. Bắt buộc.</summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>Người giao duyệt một báo cáo: xác nhận hoặc từ chối.</summary>
public sealed class DuyetBaoCaoRequest
{
    /// <summary>
    /// <c>true</c> = xác nhận, nhiệm vụ chuyển sang HOAN_THANH.
    /// <c>false</c> = từ chối, nhiệm vụ chuyển sang YEU_CAU_BO_SUNG.
    /// </summary>
    public bool XacNhan { get; set; }

    /// <summary>
    /// Ý kiến của người duyệt. <b>Bắt buộc khi từ chối</b> — người thực hiện cần biết
    /// phải sửa gì, từ chối mà không nói lý do thì họ không có căn cứ làm lại.
    /// </summary>
    public string? ReviewNote { get; set; }
}
