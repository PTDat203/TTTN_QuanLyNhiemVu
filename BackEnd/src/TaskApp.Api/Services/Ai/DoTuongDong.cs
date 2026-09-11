using Microsoft.Extensions.Options;
using TaskApp.Api.Common;

namespace TaskApp.Api.Services.Ai;

/// <summary>Hai loại phép so, mỗi loại có thang điểm thô riêng.</summary>
public enum KieuSoSanh
{
    /// <summary>Nhiệm vụ so với một loại văn bản khác: hồ sơ người, mô tả phòng, nhóm, kỹ năng.</summary>
    BatDoiXung,

    /// <summary>Nhiệm vụ so với nhiệm vụ: hai bên cùng một loại văn bản.</summary>
    DoiXung
}

/// <summary>
/// Một cách đo "hai đoạn văn gần nghĩa nhau đến đâu". Có hai cách: nhúng ngữ nghĩa (chính) và
/// TF-IDF (dự phòng khi dịch vụ Python tắt, đồng thời là phương pháp cơ sở để so sánh).
///
/// <para>
/// Hai cách cho ra con số thô ở hai thang rất khác nhau. Cosine của E5 dồn trong khoảng hẹp — trên
/// dữ liệu thật, cặp không liên quan có trung vị 0,816 còn cặp liên quan 0,845. Cosine TF-IDF thì
/// trải từ 0 tới chừng 0,4. Đem con số thô vào công thức thì thành phần ngữ nghĩa gần như không
/// phân biệt được ai với ai. Vì vậy mỗi cách có bộ ngưỡng riêng (<see cref="HieuChinh"/>) để quy
/// về cùng thang 0..1, hiệu chỉnh bằng dữ liệu.
/// </para>
/// </summary>
public abstract class DoTuongDong
{
    public abstract string Ten { get; }

    /// <summary>Bộ ngưỡng đã hiệu chỉnh riêng cho phương pháp này.</summary>
    public abstract HieuChinhPhuongPhap HieuChinh { get; }

    /// <summary>Độ tương đồng thô giữa một truy vấn và từng đoạn. <c>null</c> nếu lúc này không tính được.</summary>
    public abstract Task<double[]?> SoVoiThoAsync(
        string truyVan, IReadOnlyList<string> cacDoan, KieuSoSanh kieu, CancellationToken ct);

    /// <summary>Cặp ngưỡng quy điểm thô về thang 0..1 cho từng loại phép so.</summary>
    public NguongChuanHoa Nguong(KieuSoSanh kieu)
        => kieu == KieuSoSanh.DoiXung ? HieuChinh.DoiXung : HieuChinh.BatDoiXung;

    /// <summary>Như <see cref="SoVoiThoAsync"/> nhưng đã quy về thang 0..1.</summary>
    public async Task<double[]?> SoVoiAsync(
        string truyVan, IReadOnlyList<string> cacDoan, KieuSoSanh kieu, CancellationToken ct)
    {
        var tho = await SoVoiThoAsync(truyVan, cacDoan, kieu, ct);
        if (tho is null) return null;

        var nguong = Nguong(kieu);
        return tho.Select(nguong.ChuanHoa).ToArray();
    }
}

/// <summary>Đo bằng véc-tơ nhúng của mô hình E5 qua dịch vụ Python.</summary>
public sealed class DoTuongDongNhung : DoTuongDong
{
    private readonly DichVuNhung _nhung;
    private readonly CauHinhGoiY _cauHinh;

    public DoTuongDongNhung(DichVuNhung nhung, IOptions<CauHinhGoiY> cauHinh)
    {
        _nhung = nhung;
        _cauHinh = cauHinh.Value;
    }

    public override string Ten => $"Nhúng ngữ nghĩa ({_nhung.TenMoHinh ?? "intfloat/multilingual-e5-small"})";

    public override HieuChinhPhuongPhap HieuChinh => _cauHinh.Nhung;

    public override async Task<double[]?> SoVoiThoAsync(
        string truyVan, IReadOnlyList<string> cacDoan, KieuSoSanh kieu, CancellationToken ct)
    {
        if (cacDoan.Count == 0) return Array.Empty<double>();

        var vTruyVan = await _nhung.NhungAsync(new[] { truyVan }, LoaiNhung.TruyVan, ct);
        if (vTruyVan is null) return null;

        // So nhiệm vụ với nhiệm vụ thì hai bên cùng vai "truy vấn": E5 khuyên dùng một vai
        // duy nhất cho bài toán đối xứng.
        var vDoan = await _nhung.NhungAsync(
            cacDoan, kieu == KieuSoSanh.DoiXung ? LoaiNhung.TruyVan : LoaiNhung.Doan, ct);
        if (vDoan is null) return null;

        return vDoan.Select(v => DichVuNhung.TichVoHuong(vTruyVan[0], v)).ToArray();
    }
}

/// <summary>Đo bằng TF-IDF + cosine. Không cần dịch vụ ngoài, luôn tính được.</summary>
public sealed class DoTuongDongTfIdf : DoTuongDong
{
    private readonly CauHinhGoiY _cauHinh;

    public DoTuongDongTfIdf(IOptions<CauHinhGoiY> cauHinh) => _cauHinh = cauHinh.Value;

    public override string Ten => "TF-IDF + cosine";

    public override HieuChinhPhuongPhap HieuChinh => _cauHinh.TfIdf;

    public override Task<double[]?> SoVoiThoAsync(
        string truyVan, IReadOnlyList<string> cacDoan, KieuSoSanh kieu, CancellationToken ct)
    {
        if (cacDoan.Count == 0) return Task.FromResult<double[]?>(Array.Empty<double>());

        var tuDoan = cacDoan.Select(XuLyVanBan.TachTu).ToList();

        // IDF tính trên chính tập đoạn đang so: từ nào hiếm GIỮA CÁC ĐOẠN mới là từ giúp phân
        // biệt đoạn này với đoạn kia.
        var idf = TfIdf.DungIdf(tuDoan);
        var vTruyVan = TfIdf.VectorHoa(XuLyVanBan.TachTu(truyVan), idf);

        var ketQua = tuDoan.Select(t => TfIdf.Cosine(vTruyVan, TfIdf.VectorHoa(t, idf))).ToArray();
        return Task.FromResult<double[]?>(ketQua);
    }
}
