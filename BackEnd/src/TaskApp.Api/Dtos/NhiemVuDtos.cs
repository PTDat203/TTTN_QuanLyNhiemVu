namespace TaskApp.Api.Dtos;

/// <summary>Điều kiện lọc danh sách nhiệm vụ.</summary>
public sealed class NhiemVuLocRequest
{
    /// <summary>Lọc theo mã trạng thái, ví dụ <c>DANG_THUC_HIEN</c>. Bỏ trống = mọi trạng thái.</summary>
    public string? StatusCode { get; set; }

    /// <summary>Lọc theo mức ưu tiên: LOW / MEDIUM / HIGH.</summary>
    public string? Priority { get; set; }

    /// <summary>Lọc theo người thực hiện.</summary>
    public long? AssigneeId { get; set; }

    /// <summary>Lọc theo người tạo.</summary>
    public long? CreatorId { get; set; }

    /// <summary>Tìm trong tiêu đề, không phân biệt hoa thường.</summary>
    public string? TuKhoa { get; set; }

    /// <summary>Chỉ lấy nhiệm vụ chưa có người thực hiện.</summary>
    public bool? ChuaGiao { get; set; }

    /// <summary>Chỉ lấy nhiệm vụ quá hạn mà chưa hoàn thành.</summary>
    public bool? QuaHan { get; set; }

    public DateTime? HanTuNgay { get; set; }
    public DateTime? HanDenNgay { get; set; }

    public int Trang { get; set; } = 1;
    public int KichThuocTrang { get; set; } = 20;

    /// <summary>Sắp xếp: <c>duedate</c> | <c>priority</c> | <c>created</c> | <c>title</c>.</summary>
    public string? SapXep { get; set; }

    /// <summary>Giảm dần hay không.</summary>
    public bool GiamDan { get; set; }
}

/// <summary>Nhiệm vụ ở dạng rút gọn, dùng cho danh sách.</summary>
public class NhiemVuTomTatDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;
    public string TenUuTien { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;
    public string TenTrangThai { get; set; } = string.Empty;

    public long CreatorId { get; set; }
    public string? TenNguoiTao { get; set; }

    public long? AssigneeId { get; set; }
    public string? TenNguoiThucHien { get; set; }

    /// <summary>Phòng thực thi. Rỗng khi nhiệm vụ chưa giao và chưa được gắn phòng.</summary>
    public long? DepartmentId { get; set; }
    public string? TenPhongBan { get; set; }

    /// <summary>Nhóm phụ trách. Rỗng với phòng không chia nhóm.</summary>
    public long? TeamId { get; set; }
    public string? TenNhom { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }

    /// <summary>Số ngày còn lại tới hạn. Âm là đã quá hạn. Null khi chưa đặt hạn.</summary>
    public int? SoNgayConLai { get; set; }

    /// <summary>Quá hạn mà chưa hoàn thành.</summary>
    public bool QuaHan { get; set; }

    /// <summary>Phần trăm tiến độ mới nhất, lấy từ bản ghi TASK_PROGRESS gần nhất.</summary>
    public int? TienDoPhanTram { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Nhiệm vụ đầy đủ, kèm lịch sử tiến độ và báo cáo.</summary>
public sealed class NhiemVuChiTietDto : NhiemVuTomTatDto
{
    public string? Description { get; set; }

    /// <summary>Các trạng thái hợp lệ có thể chuyển tới từ trạng thái hiện tại.</summary>
    public IReadOnlyList<string> TrangThaiKeTiep { get; set; } = Array.Empty<string>();

    public IReadOnlyList<TienDoDto> LichSuTienDo { get; set; } = Array.Empty<TienDoDto>();
    public IReadOnlyList<BaoCaoDto> DanhSachBaoCao { get; set; } = Array.Empty<BaoCaoDto>();
    public IReadOnlyList<TepDinhKemDto> TepDinhKem { get; set; } = Array.Empty<TepDinhKemDto>();
}

/// <summary>Một lần cập nhật tiến độ.</summary>
public sealed class TienDoDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string? TenNguoiCapNhat { get; set; }
    public int ProgressPercent { get; set; }
    public string? Content { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>Một bản báo cáo kết quả.</summary>
public sealed class BaoCaoDto
{
    public long Id { get; set; }
    public long ReporterId { get; set; }
    public string? TenNguoiBaoCao { get; set; }
    public string Content { get; set; } = string.Empty;

    /// <summary>CHO_XAC_NHAN / DA_XAC_NHAN / TU_CHOI. Cột REPORT_STATUS.</summary>
    public string ReportStatus { get; set; } = string.Empty;
    public string TenTrangThaiBaoCao { get; set; } = string.Empty;

    public long? ReviewerId { get; set; }
    public string? TenNguoiDuyet { get; set; }
    public string? ReviewNote { get; set; }

    /// <summary>Chất lượng kết quả, thang 1..5. Rỗng khi chưa duyệt.</summary>
    public int? QualityScore { get; set; }

    /// <summary>Mức đáp ứng đủ yêu cầu, thang 1..5. Rỗng khi chưa duyệt.</summary>
    public int? CompletionScore { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>Tệp đính kèm (chỉ metadata, không chứa nội dung tệp).</summary>
public sealed class TepDinhKemDto
{
    public long Id { get; set; }
    public long? ReportId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public long UploadedBy { get; set; }
    public string? TenNguoiTaiLen { get; set; }
    public DateTime? UploadedAt { get; set; }
}

/// <summary>Tạo nhiệm vụ mới. Trạng thái khởi tạo luôn là MOI_TAO do server đặt.</summary>
public sealed class TaoNhiemVuRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>LOW / MEDIUM / HIGH. Bỏ trống thì mặc định MEDIUM.</summary>
    public string? Priority { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Giao luôn cho người này khi tạo. Bỏ trống thì nhiệm vụ ở MOI_TAO, giao sau.
    /// Có giá trị thì nhiệm vụ được tạo thẳng ở trạng thái DA_GIAO.
    /// </summary>
    public long? AssigneeId { get; set; }
}

/// <summary>Sửa nhiệm vụ. Chỉ cho phép khi trạng thái là MOI_TAO hoặc DA_GIAO.</summary>
public sealed class SuaNhiemVuRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
}

/// <summary>Giao nhiệm vụ cho một người thực hiện.</summary>
public sealed class GiaoNhiemVuRequest
{
    public long AssigneeId { get; set; }
}
