using System.Text.Json.Serialization;

namespace QLNV.Core.Dtos;

/// <summary>
/// §5.7 G1 — mot phan tu trong mang tra ve cua <c>POST /api/v1/files</c>,
/// dong thoi la dang hien thi tep dinh kem o moi man.
/// </summary>
public sealed class FileDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>Khoa luu tru do <c>IFileStorage</c> sinh (KHONG phai duong dan dia tuyet doi).</summary>
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    [JsonPropertyName("loaiBanGhi")]
    public string? LoaiBanGhi { get; set; }

    [JsonPropertyName("recordId")]
    public Guid? RecordId { get; set; }

    [JsonPropertyName("createBy")]
    public Guid CreateBy { get; set; }

    [JsonPropertyName("createDate")]
    public DateTime CreateDate { get; set; }
}

/// <summary>Ket qua doc mot tep tu kho luu tru (§5.7 G2).</summary>
public sealed class NoiDungTepDto
{
    public NoiDungTepDto(Stream noiDung, string fileName, string contentType, long size)
    {
        NoiDung = noiDung;
        FileName = fileName;
        ContentType = contentType;
        Size = size;
    }

    /// <summary>Luong du lieu. Nguoi goi chiu trach nhiem giai phong.</summary>
    public Stream NoiDung { get; }

    public string FileName { get; }

    public string ContentType { get; }

    public long Size { get; }
}
