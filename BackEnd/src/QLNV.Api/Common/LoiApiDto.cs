using System.Text.Json.Serialization;

namespace QLNV.Api.Common;

/// <summary>
/// Goi loi chuan tra ve cho FE. Moi thong bao deu la TIENG VIET CO DAU (yeu cau bat buoc).
/// <c>maLoi</c> lay tu <see cref="QLNV.Core.Common.MaLoiChung"/> de FE xu ly may doc duoc.
/// </summary>
public sealed class LoiApiDto
{
    public LoiApiDto()
    {
    }

    public LoiApiDto(string thongBao, string? maLoi = null)
    {
        ThongBao = thongBao;
        MaLoi = maLoi;
    }

    /// <summary>Ma loi may doc duoc (KHONG_TIM_THAY, KHONG_CO_QUYEN, SAI_TRANG_THAI...).</summary>
    [JsonPropertyName("maLoi")]
    public string? MaLoi { get; set; }

    /// <summary>Thong bao hien thi cho nguoi dung — tieng Viet co dau.</summary>
    [JsonPropertyName("thongBao")]
    public string ThongBao { get; set; } = string.Empty;

    /// <summary>Chi tiet tung loi (dung cho ket qua FluentValidation nhieu dong).</summary>
    [JsonPropertyName("chiTiet")]
    public List<string> ChiTiet { get; set; } = new();
}
