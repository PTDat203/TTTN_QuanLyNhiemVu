using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace TaskApp.Api.Services.Ai;

/// <summary>Cấu hình kết nối tới dịch vụ nhúng Python. Đọc từ mục <c>DichVuNhung</c>.</summary>
public sealed class CauHinhDichVuNhung
{
    public const string Muc = "DichVuNhung";

    /// <summary>Tắt thì phần gợi ý dùng thẳng TF-IDF, không gọi sang Python.</summary>
    public bool Bat { get; set; } = true;

    public string Url { get; set; } = "http://localhost:8000";

    /// <summary>
    /// Thời gian chờ mỗi lần gọi. Để rộng tay vì lần gọi đầu sau khi khởi động phải nhúng cả
    /// trăm câu trên CPU; các lần sau gần như chỉ đọc bộ nhớ đệm.
    /// </summary>
    public int ThoiGianChoGiay { get; set; } = 60;

    /// <summary>
    /// Gọi hỏng một lần thì ngưng thử lại trong chừng này giây. Không có nó thì lúc dịch vụ đang
    /// tắt, mọi lần bấm "Gợi ý" đều phải chờ hết thời gian kết nối rồi mới lùi về TF-IDF.
    /// </summary>
    public int TamNgungGiay { get; set; } = 30;
}

/// <summary>Vai của văn bản khi nhúng — mô hình E5 xử lý hai vai khác nhau.</summary>
public enum LoaiNhung
{
    /// <summary>Phía đi tìm: nội dung nhiệm vụ.</summary>
    TruyVan,

    /// <summary>Phía được tìm: hồ sơ nhân viên, mô tả phòng ban, nhóm, kỹ năng.</summary>
    Doan
}

/// <summary>
/// Gọi dịch vụ Python lấy véc-tơ nhúng, kèm bộ nhớ đệm trong RAM.
///
/// <para>
/// <b>Bộ nhớ đệm theo nội dung văn bản</b>, không theo mã người hay mã phòng. Ai đó đổi kỹ năng
/// thì văn bản hồ sơ đổi, tự thành một khoá mới — không bao giờ dùng nhầm véc-tơ cũ, và không
/// cần cơ chế xoá đệm nào. Vài trăm văn bản × 384 số thực chỉ tốn vài trăm KB.
/// </para>
/// <para>
/// Oracle 21c chưa có kiểu VECTOR (có từ bản 23ai) nên không lưu véc-tơ xuống database. Khởi
/// động lại thì lần gợi ý đầu tiên chậm hơn một chút để nhúng lại — chấp nhận được ở quy mô này.
/// </para>
/// <para>
/// Dịch vụ không phản hồi thì trả về <c>null</c> chứ không ném lỗi: phía gọi lùi về TF-IDF và ghi
/// rõ trong kết quả. Chức năng gợi ý không bao giờ chết theo dịch vụ Python.
/// </para>
/// </summary>
public sealed class DichVuNhung
{
    public const string TenHttpClient = "DichVuNhung";

    private const int KichThuocLo = 256;
    private const int SoMucDemToiDa = 20_000;

    private readonly IHttpClientFactory _http;
    private readonly CauHinhDichVuNhung _cauHinh;
    private readonly ILogger<DichVuNhung> _log;
    private readonly ConcurrentDictionary<string, float[]> _dem = new(StringComparer.Ordinal);
    private long _tamNgungDen;

    public DichVuNhung(IHttpClientFactory http, IOptions<CauHinhDichVuNhung> cauHinh, ILogger<DichVuNhung> log)
    {
        _http = http;
        _cauHinh = cauHinh.Value;
        _log = log;
    }

    /// <summary>Tên mô hình mà dịch vụ báo về ở lần gọi thành công gần nhất.</summary>
    public string? TenMoHinh { get; private set; }

    public int SoMucDangDem => _dem.Count;

    /// <summary>Nhúng một loạt văn bản. Trả <c>null</c> nếu dịch vụ tắt hoặc không phản hồi.</summary>
    public async Task<float[][]?> NhungAsync(IReadOnlyList<string> vanBan, LoaiNhung loai, CancellationToken ct)
    {
        if (!_cauHinh.Bat) return null;

        var ketQua = new float[vanBan.Count][];
        var viTriThieu = new List<int>();
        for (var i = 0; i < vanBan.Count; i++)
        {
            if (_dem.TryGetValue(Khoa(loai, vanBan[i]), out var v)) ketQua[i] = v;
            else viTriThieu.Add(i);
        }

        if (viTriThieu.Count == 0) return ketQua;
        if (Environment.TickCount64 < Interlocked.Read(ref _tamNgungDen)) return null;

        var moi = new Dictionary<string, float[]>(StringComparer.Ordinal);
        try
        {
            var client = _http.CreateClient(TenHttpClient);
            var canNhung = viTriThieu.Select(i => vanBan[i]).Distinct(StringComparer.Ordinal).ToList();

            for (var dau = 0; dau < canNhung.Count; dau += KichThuocLo)
            {
                var lo = canNhung.GetRange(dau, Math.Min(KichThuocLo, canNhung.Count - dau));
                using var phanHoi = await client.PostAsJsonAsync("embed",
                    new YeuCauNhung(lo, loai == LoaiNhung.TruyVan ? "query" : "passage"), ct);
                phanHoi.EnsureSuccessStatusCode();

                var than = await phanHoi.Content.ReadFromJsonAsync<PhanHoiNhung>(ct)
                           ?? throw new InvalidOperationException("Dịch vụ nhúng trả về rỗng.");
                if (than.Vectors.Count != lo.Count)
                {
                    throw new InvalidOperationException(
                        $"Gửi {lo.Count} văn bản nhưng nhận về {than.Vectors.Count} véc-tơ.");
                }

                TenMoHinh = than.Model;
                for (var j = 0; j < lo.Count; j++) moi[lo[j]] = than.Vectors[j];
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            Interlocked.Exchange(ref _tamNgungDen, Environment.TickCount64 + _cauHinh.TamNgungGiay * 1000L);
            _log.LogWarning("Không gọi được dịch vụ nhúng tại {Url}: {Loi}. Tạm dùng TF-IDF trong {Giay} giây.",
                _cauHinh.Url, ex.GetBaseException().Message, _cauHinh.TamNgungGiay);
            return null;
        }

        if (_dem.Count + moi.Count > SoMucDemToiDa) _dem.Clear();
        foreach (var (vb, v) in moi) _dem[Khoa(loai, vb)] = v;
        foreach (var i in viTriThieu) ketQua[i] = moi[vanBan[i]];

        return ketQua;
    }

    /// <summary>Hỏi dịch vụ đã sẵn sàng chưa. Dùng cho màn hình trạng thái, không dùng khi chấm điểm.</summary>
    public async Task<(bool SanSang, string? MoHinh, string? Loi)> KiemTraAsync(CancellationToken ct)
    {
        if (!_cauHinh.Bat) return (false, null, "Đã tắt trong cấu hình (DichVuNhung:Bat = false).");

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            var sk = await _http.CreateClient(TenHttpClient).GetFromJsonAsync<SucKhoe>("health", cts.Token);
            return sk?.Ready == true ? (true, sk.Model, null) : (false, sk?.Model, "Mô hình chưa nạp xong.");
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            return (false, null, ex.GetBaseException().Message);
        }
    }

    /// <summary>Tích vô hướng. Véc-tơ đã chuẩn hoá độ dài 1 nên đây chính là cosine.</summary>
    public static double TichVoHuong(float[] a, float[] b)
    {
        double tong = 0;
        for (var i = 0; i < a.Length; i++) tong += a[i] * b[i];
        return tong;
    }

    private static string Khoa(LoaiNhung loai, string vanBan)
        => (loai == LoaiNhung.TruyVan ? "q|" : "p|") + vanBan;

    private sealed record YeuCauNhung(
        [property: JsonPropertyName("texts")] IReadOnlyList<string> Texts,
        [property: JsonPropertyName("kind")] string Kind);

    private sealed class PhanHoiNhung
    {
        [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
        [JsonPropertyName("vectors")] public List<float[]> Vectors { get; set; } = new();
    }

    private sealed class SucKhoe
    {
        [JsonPropertyName("ready")] public bool Ready { get; set; }
        [JsonPropertyName("model")] public string? Model { get; set; }
    }
}
