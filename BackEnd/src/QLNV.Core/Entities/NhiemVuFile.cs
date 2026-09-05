namespace QLNV.Core.Entities;

/// <summary>
/// §4.6 — bang <c>NHIEMVU_FILE</c> (tep dinh kem). Goc: <c>ResponseModel</c>, rut gon.
/// <c>filePath</c> la khoa logic do <c>IFileStorage</c> quan ly, KHONG phai duong dan
/// tuyet doi tren dia — de sau nay thay he thong tep cuc bo bang MinIO ma khong sua controller.
/// </summary>
public class NhiemVuFile
{
    /// <summary>Khoa chinh. Cot <c>id</c>.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Id ban ghi nghiep vu so huu tep. Cot <c>recordid</c>.
    /// Co the null trong khoang thoi gian tu luc upload (§5.7 G1) den luc gan vao ban ghi.
    /// </summary>
    public Guid? RecordId { get; set; }

    /// <summary>
    /// VANBAN / NHIEMVU / XULY / GIAHAN (xem <see cref="Constants.LoaiBanGhiFile"/>).
    /// Cot <c>loai_ban_ghi</c>, varchar(20).
    /// </summary>
    public string? LoaiBanGhi { get; set; }

    /// <summary>Ten tep goc do nguoi dung tai len. Cot <c>fileName</c>.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Khoa luu tru do <c>IFileStorage</c> sinh. Cot <c>filePath</c>.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Kich thuoc byte. Toi da 20 MB (§5.7 G1). Cot <c>fileSize</c>.</summary>
    public long FileSize { get; set; }

    /// <summary>MIME type. Cot <c>contentType</c>.</summary>
    public string? ContentType { get; set; }

    /// <summary>Nguoi tai len. Cot <c>createBy</c>. §5.7 G3: chi nguoi nay duoc xoa.</summary>
    public Guid CreateBy { get; set; }

    /// <summary>Thoi diem tai len. Cot <c>createDate</c>.</summary>
    public DateTime CreateDate { get; set; }
}
