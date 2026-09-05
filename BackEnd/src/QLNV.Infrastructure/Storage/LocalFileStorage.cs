using System.Globalization;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;

namespace QLNV.Infrastructure.Storage;

/// <summary>Tuy chon cho <see cref="LocalFileStorage"/>.</summary>
public sealed class TuyChonLuuTep
{
    /// <summary>
    /// Thu muc goc luu tep. Duong dan tuong doi duoc hieu theo thu muc lam viec cua tien trinh.
    /// Mac dinh <c>App_Data/files</c> (§7.2 — ban rut gon cua MinIO).
    /// </summary>
    public string ThuMucGoc { get; set; } = Path.Combine("App_Data", "files");
}

/// <summary>
/// §5.7 G1-G3 — cai dat <see cref="IFileStorage"/> tren HE THONG TEP CUC BO.
///
/// Khoa luu tru co dang <c>yyyy/MM/&lt;guid&gt;&lt;duoi&gt;</c> — la khoa LOGIC, khong phai
/// duong dan tuyet doi. Nho vay khi doi sang MinIO chi phai thay lop nay, KHONG sua controller
/// va KHONG phai chuyen doi du lieu trong cot <c>NHIEMVU_FILE.filePath</c>.
///
/// Kiem soat an toan:
///  - whitelist duoi tep (§5.7 G1): pdf, doc, docx, xls, xlsx, png, jpg, jpeg;
///  - gioi han 20 MB (§5.7 G1);
///  - chan duyet thu muc (path traversal): moi duong dan vat ly deu phai nam trong thu muc goc;
///  - ten tep vat ly do he thong sinh (Guid), KHONG lay tu ten nguoi dung tai len.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _thuMucGoc;

    /// <summary>Anh xa duoi tep -&gt; MIME type, dung khi ban ghi khong luu contentType.</summary>
    private static readonly IReadOnlyDictionary<string, string> MimeTheoDuoi =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg"
        };

    public LocalFileStorage(TuyChonLuuTep tuyChon)
    {
        ArgumentNullException.ThrowIfNull(tuyChon);
        string goc = string.IsNullOrWhiteSpace(tuyChon.ThuMucGoc)
            ? Path.Combine("App_Data", "files")
            : tuyChon.ThuMucGoc;

        _thuMucGoc = Path.GetFullPath(goc);
    }

    /// <summary>Duong dan tuyet doi cua thu muc goc — tien cho kiem thu va chan doan.</summary>
    public string ThuMucGoc => _thuMucGoc;

    /// <inheritdoc />
    public async Task<Result<string>> LuuAsync(Stream noiDung, string tenTep, string? contentType, CancellationToken ct)
    {
        if (noiDung is null)
        {
            return Result<string>.ThatBai("Không có nội dung tệp để lưu.", MaLoiChung.LoiTep);
        }

        if (string.IsNullOrWhiteSpace(tenTep))
        {
            return Result<string>.ThatBai("Tên tệp không được để trống.", MaLoiChung.DuLieuKhongHopLe);
        }

        // §5.7 G1 — whitelist duoi tep
        string duoi = Path.GetExtension(tenTep);
        if (string.IsNullOrEmpty(duoi) || !LaDuoiChoPhep(duoi))
        {
            return Result<string>.ThatBai(
                "Định dạng tệp không được phép. Chỉ chấp nhận: " + string.Join(", ", GioiHan.DuoiTepChoPhep) + ".",
                MaLoiChung.DuLieuKhongHopLe);
        }

        // §5.7 G1 — gioi han 20 MB. Kiem truoc neu luong biet do dai.
        if (noiDung.CanSeek && noiDung.Length > GioiHan.KichThuocTepToiDa)
        {
            return Result<string>.ThatBai("Tệp vượt quá dung lượng cho phép (20 MB).", MaLoiChung.DuLieuKhongHopLe);
        }

        var homNay = DateTime.Now;
        string thuMucCon = Path.Combine(
            homNay.Year.ToString("D4", CultureInfo.InvariantCulture),
            homNay.Month.ToString("D2", CultureInfo.InvariantCulture));

        // Ten tep vat ly do HE THONG sinh — khong bao gio dung ten nguoi dung gui len
        string tenVatLy = Guid.NewGuid().ToString("N") + duoi.ToLowerInvariant();
        string khoaLuuTru = thuMucCon.Replace('\\', '/') + "/" + tenVatLy;

        string duongDan = Path.Combine(_thuMucGoc, thuMucCon, tenVatLy);
        string? thuMuc = Path.GetDirectoryName(duongDan);
        if (thuMuc is null)
        {
            return Result<string>.ThatBai("Không xác định được thư mục lưu tệp.", MaLoiChung.LoiTep);
        }

        try
        {
            Directory.CreateDirectory(thuMuc);

            await using var dich = new FileStream(
                duongDan, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true);

            long daGhi = await SaoChepCoGioiHanAsync(noiDung, dich, GioiHan.KichThuocTepToiDa, ct)
                .ConfigureAwait(false);

            if (daGhi < 0)
            {
                // Vuot nguong khi dang ghi (luong khong biet truoc do dai) — don dep roi bao loi
                await dich.DisposeAsync().ConfigureAwait(false);
                XoaImLang(duongDan);
                return Result<string>.ThatBai("Tệp vượt quá dung lượng cho phép (20 MB).",
                    MaLoiChung.DuLieuKhongHopLe);
            }
        }
        catch (IOException ex)
        {
            return Result<string>.ThatBai("Không ghi được tệp lên máy chủ: " + ex.Message, MaLoiChung.LoiTep);
        }
        catch (UnauthorizedAccessException)
        {
            return Result<string>.ThatBai("Máy chủ không có quyền ghi vào thư mục lưu tệp.", MaLoiChung.LoiTep);
        }

        return Result<string>.Ok(khoaLuuTru);
    }

    /// <inheritdoc />
    public Task<Result<NoiDungTepDto>> DocAsync(string khoaLuuTru, CancellationToken ct)
    {
        if (!ThuDuongDan(khoaLuuTru, out string duongDan))
        {
            return Task.FromResult(Result<NoiDungTepDto>.ThatBai(
                "Khoá lưu trữ không hợp lệ.", MaLoiChung.DuLieuKhongHopLe));
        }

        if (!File.Exists(duongDan))
        {
            return Task.FromResult(Result<NoiDungTepDto>.ThatBai(
                "Không tìm thấy tệp đính kèm trên máy chủ.", MaLoiChung.KhongTimThay));
        }

        try
        {
            var thongTin = new FileInfo(duongDan);
            var luong = new FileStream(duongDan, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 81920, useAsync: true);

            string tenTep = Path.GetFileName(duongDan);
            string mime = DoanMime(Path.GetExtension(duongDan));

            return Task.FromResult(Result<NoiDungTepDto>.Ok(
                new NoiDungTepDto(luong, tenTep, mime, thongTin.Length)));
        }
        catch (IOException ex)
        {
            return Task.FromResult(Result<NoiDungTepDto>.ThatBai(
                "Không đọc được tệp đính kèm: " + ex.Message, MaLoiChung.LoiTep));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(Result<NoiDungTepDto>.ThatBai(
                "Máy chủ không có quyền đọc tệp đính kèm.", MaLoiChung.LoiTep));
        }
    }

    /// <inheritdoc />
    public Task<Result> XoaAsync(string khoaLuuTru, CancellationToken ct)
    {
        if (!ThuDuongDan(khoaLuuTru, out string duongDan))
        {
            return Task.FromResult(Result.ThatBai("Khoá lưu trữ không hợp lệ.", MaLoiChung.DuLieuKhongHopLe));
        }

        try
        {
            if (File.Exists(duongDan)) File.Delete(duongDan);
            return Task.FromResult(Result.Ok());
        }
        catch (IOException ex)
        {
            return Task.FromResult(Result.ThatBai("Không xoá được tệp: " + ex.Message, MaLoiChung.LoiTep));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(Result.ThatBai("Máy chủ không có quyền xoá tệp.", MaLoiChung.LoiTep));
        }
    }

    /// <inheritdoc />
    public Task<bool> TonTaiAsync(string khoaLuuTru, CancellationToken ct) =>
        Task.FromResult(ThuDuongDan(khoaLuuTru, out string duongDan) && File.Exists(duongDan));

    // -------------------------------------------------------------------------
    // Ham phu
    // -------------------------------------------------------------------------

    private static bool LaDuoiChoPhep(string duoi)
    {
        foreach (var d in GioiHan.DuoiTepChoPhep)
        {
            if (string.Equals(d, duoi, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string DoanMime(string duoi) =>
        MimeTheoDuoi.TryGetValue(duoi, out var mime) ? mime : "application/octet-stream";

    /// <summary>
    /// Doi khoa luu tru sang duong dan vat ly, CHAN duyet thu muc.
    /// Tra <c>false</c> khi khoa rong, chua "..", la duong dan tuyet doi, hoac tro ra
    /// ngoai thu muc goc.
    /// </summary>
    private bool ThuDuongDan(string khoaLuuTru, out string duongDan)
    {
        duongDan = string.Empty;
        if (string.IsNullOrWhiteSpace(khoaLuuTru)) return false;

        string khoa = khoaLuuTru.Replace('\\', '/').Trim();
        if (khoa.Contains("..", StringComparison.Ordinal)) return false;
        if (khoa.StartsWith('/')) return false;
        if (Path.IsPathRooted(khoa)) return false;
        if (khoa.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return false;

        string dayDu = Path.GetFullPath(Path.Combine(_thuMucGoc, khoa));

        // Bat buoc nam trong thu muc goc
        string gocCoDauPhanCach = _thuMucGoc.EndsWith(Path.DirectorySeparatorChar)
            ? _thuMucGoc
            : _thuMucGoc + Path.DirectorySeparatorChar;

        if (!dayDu.StartsWith(gocCoDauPhanCach, StringComparison.OrdinalIgnoreCase)) return false;

        duongDan = dayDu;
        return true;
    }

    /// <summary>
    /// Sao chep co gioi han. Tra ve so byte da ghi, hoac -1 neu vuot
    /// <paramref name="toiDa"/> (khi do luong nguon khong biet truoc do dai).
    /// </summary>
    private static async Task<long> SaoChepCoGioiHanAsync(Stream nguon, Stream dich, long toiDa, CancellationToken ct)
    {
        byte[] bo = new byte[81920];
        long tong = 0;
        int doc;

        while ((doc = await nguon.ReadAsync(bo.AsMemory(0, bo.Length), ct).ConfigureAwait(false)) > 0)
        {
            tong += doc;
            if (tong > toiDa) return -1;
            await dich.WriteAsync(bo.AsMemory(0, doc), ct).ConfigureAwait(false);
        }

        await dich.FlushAsync(ct).ConfigureAwait(false);
        return tong;
    }

    /// <summary>Xoa tep don dep, nuot loi vi day la duong don dep sau khi da co loi chinh.</summary>
    private static void XoaImLang(string duongDan)
    {
        try
        {
            if (File.Exists(duongDan)) File.Delete(duongDan);
        }
        catch (IOException)
        {
            // Khong lam gi: tep rac se duoc don o lan bao tri sau.
        }
        catch (UnauthorizedAccessException)
        {
            // Khong lam gi.
        }
    }
}
