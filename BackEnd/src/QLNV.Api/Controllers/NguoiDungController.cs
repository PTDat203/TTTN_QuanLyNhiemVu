using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNV.Api.Authorization;
using QLNV.Api.Mapping;
using QLNV.Api.Services;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Core.Constants;
using QLNV.Core.Dtos;
using QLNV.Core.Entities;

namespace QLNV.Api.Controllers;

/// <summary>
/// §5.9 I1-I3 — quan tri nguoi dung va ho so nang luc / hieu suat.
///
/// §6.4 (LO HONG CUA HE GOC): he goc khong co route guard cho <c>/admin/...</c>.
/// O day MOI endpoint ghi du lieu deu gan <c>[ChiQuanTri]</c>, khong sot cai nao.
/// Hai endpoint chi de XEM (I2, I3) tuan theo §6.2 dong 22: quan tri xem tat ca,
/// nguoi giao xem duoc nguoi trong don vi minh + don vi con, nguoi thuc hien chi
/// xem cua chinh minh.
/// </summary>
[Route("api/v1/nguoi-dung")]
[Authorize]
public sealed class NguoiDungController : ApiControllerBase
{
    private readonly DbContext _db;
    private readonly IPasswordHasher _bam;
    private readonly IHienTai _hienTai;
    private readonly IThongKeService _thongKe;
    private readonly PhamViDonViService _phamVi;

    public NguoiDungController(
        DbContext db,
        IPasswordHasher bam,
        IHienTai hienTai,
        IThongKeService thongKe,
        PhamViDonViService phamVi)
    {
        _db = db;
        _bam = bam;
        _hienTai = hienTai;
        _thongKe = thongKe;
        _phamVi = phamVi;
    }

    // ------------------------------------------------------------------ I1: CRUD

    /// <summary>
    /// I1 — Danh sách người dùng, có phân trang và lọc.
    /// </summary>
    /// <param name="loc">Tham số lọc: từ khoá, đơn vị, vai trò, trạng thái, trang, cỡ trang.</param>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="200">Trang dữ liệu người dùng.</response>
    /// <response code="401">Chưa đăng nhập.</response>
    /// <response code="403">Chỉ quản trị hệ thống được xem danh sách người dùng (§6.2 dòng 21).</response>
    [HttpGet]
    [ChiQuanTri]
    [ProducesResponseType(typeof(PagedResult<NguoiDungDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DanhSachAsync([FromQuery] NguoiDungLocRequest loc, CancellationToken ct)
    {
        loc ??= new NguoiDungLocRequest();

        var trang = loc.Page < 1 ? 1 : loc.Page;
        var kichThuoc = loc.Size < 1
            ? GioiHan.KichThuocTrangMacDinh
            : Math.Min(loc.Size, GioiHan.KichThuocTrangToiDa);

        // KHONG dung Include(): tang API khong phu thuoc cach tang Infrastructure cau hinh
        // navigation SysUser.DonVi. Ten don vi duoc nap bang mot truy van rieng ben duoi.
        var truyVan = _db.Set<SysUser>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(loc.Search))
        {
            var tuKhoa = loc.Search.Trim().ToLower();
            truyVan = truyVan.Where(x =>
                x.UserName.ToLower().Contains(tuKhoa)
                || x.FullName.ToLower().Contains(tuKhoa)
                || (x.Email != null && x.Email.ToLower().Contains(tuKhoa)));
        }

        if (!string.IsNullOrWhiteSpace(loc.UnitCode))
        {
            var maDonVi = loc.UnitCode.Trim();
            truyVan = truyVan.Where(x => x.UnitCode == maDonVi);
        }

        if (!string.IsNullOrWhiteSpace(loc.VaiTro))
        {
            var vaiTro = loc.VaiTro.Trim().ToUpperInvariant();
            if (!VaiTro.HopLe(vaiTro))
            {
                return LoiDuLieu($"Vai trò không hợp lệ. Chỉ chấp nhận: {string.Join(", ", VaiTro.ToanBo())}.");
            }

            truyVan = truyVan.Where(x => x.VaiTro == vaiTro);
        }

        if (loc.TrangThai.HasValue)
        {
            var trangThai = loc.TrangThai.Value;
            truyVan = truyVan.Where(x => x.TrangThai == trangThai);
        }

        var tongSo = await truyVan.CountAsync(ct);

        var duLieu = await truyVan
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.UserName)
            .Skip((trang - 1) * kichThuoc)
            .Take(kichThuoc)
            .ToListAsync(ct);

        // Ten bien khac "maDonVi" o khoi loc phia tren de tranh CS0136 (trung ten
        // giua khoi long nhau va khoi bao ngoai cua cung mot phuong thuc).
        var dsMaDonVi = duLieu.Select(x => x.UnitCode).Distinct().ToList();
        var tenDonVi = await _db.Set<SysUnit>()
            .AsNoTracking()
            .Where(x => dsMaDonVi.Contains(x.UnitCode))
            .ToDictionaryAsync(x => x.UnitCode, x => x.TenDonVi, ct);

        var items = duLieu
            .Select(x => AnhXa.SangDto(x, tenDonVi.TryGetValue(x.UnitCode, out var ten) ? ten : null))
            .ToList();

        return Ok(new PagedResult<NguoiDungDto>(items, tongSo, trang, kichThuoc));
    }

    /// <summary>
    /// I1 — Chi tiết một người dùng.
    /// Quản trị xem được tất cả; người dùng khác chỉ xem được hồ sơ của chính mình
    /// hoặc của người trong phạm vi đơn vị mình (§6.2 dòng 22).
    /// </summary>
    /// <param name="id">Định danh người dùng.</param>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="200">Hồ sơ người dùng.</response>
    /// <response code="401">Chưa đăng nhập.</response>
    /// <response code="403">Ngoài phạm vi được phép xem.</response>
    /// <response code="404">Không tìm thấy người dùng.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NguoiDungDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChiTietAsync(Guid id, CancellationToken ct)
    {
        if (!await DuocXemHoSoAsync(id, ct))
        {
            return LoiKhongCoQuyen("Bạn không có quyền xem hồ sơ của người dùng này.");
        }

        var nguoiDung = await _db.Set<SysUser>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (nguoiDung is null)
        {
            return LoiKhongTimThay("Không tìm thấy người dùng.");
        }

        var tenDonVi = await TenDonViAsync(nguoiDung.UnitCode, ct);
        return Ok(AnhXa.SangDto(nguoiDung, tenDonVi));
    }

    /// <summary>
    /// I1 — Tạo mới người dùng. Chỉ quản trị hệ thống (§6.2 dòng 21).
    /// </summary>
    /// <param name="yeuCau">Thông tin tài khoản mới; mật khẩu bắt buộc và tối thiểu 8 ký tự.</param>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="201">Tạo thành công, trả về hồ sơ vừa tạo.</response>
    /// <response code="400">Dữ liệu không hợp lệ.</response>
    /// <response code="401">Chưa đăng nhập.</response>
    /// <response code="403">Không phải quản trị hệ thống.</response>
    /// <response code="409">Tên đăng nhập đã tồn tại.</response>
    [HttpPost]
    [ChiQuanTri]
    [ProducesResponseType(typeof(NguoiDungDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TaoMoiAsync([FromBody] LuuNguoiDungRequest yeuCau, CancellationToken ct)
    {
        if (yeuCau is null)
        {
            return LoiDuLieu("Thiếu dữ liệu người dùng.");
        }

        var kiemTra = await KiemTraChungAsync(yeuCau, null, ct);
        if (kiemTra is not null)
        {
            return kiemTra;
        }

        if (string.IsNullOrWhiteSpace(yeuCau.Password) || yeuCau.Password.Length < 8)
        {
            return LoiDuLieu("Mật khẩu bắt buộc khi tạo mới và phải có ít nhất 8 ký tự.");
        }

        var nguoiDung = new SysUser
        {
            Id = Guid.NewGuid(),
            UserName = yeuCau.UserName.Trim().ToLowerInvariant(),
            PasswordHash = _bam.Bam(yeuCau.Password),
            FullName = yeuCau.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(yeuCau.Email) ? null : yeuCau.Email.Trim(),
            UnitCode = yeuCau.UnitCode.Trim(),
            ChucVu = string.IsNullOrWhiteSpace(yeuCau.ChucVu) ? null : yeuCau.ChucVu.Trim(),
            VaiTro = yeuCau.VaiTro.Trim().ToUpperInvariant(),
            TrangThai = yeuCau.TrangThai == 0 ? 0 : 1,
            MaxConcurrentTasks = yeuCau.MaxConcurrentTasks,
            CreateDate = DateTime.UtcNow
        };

        _db.Add(nguoiDung);
        await _db.SaveChangesAsync(ct);

        var tenDonVi = await TenDonViAsync(nguoiDung.UnitCode, ct);

        // Khong dung CreatedAtAction: MVC cat hau to "Async" khoi ten hanh dong nen
        // nameof(ChiTietAsync) khong sinh duoc lien ket.
        return Created($"/api/v1/nguoi-dung/{nguoiDung.Id}", AnhXa.SangDto(nguoiDung, tenDonVi));
    }

    /// <summary>
    /// I1 — Cập nhật người dùng. Chỉ quản trị hệ thống (§6.2 dòng 21).
    /// Bỏ trống trường mật khẩu nếu không muốn đổi.
    /// </summary>
    /// <param name="id">Định danh người dùng cần sửa.</param>
    /// <param name="yeuCau">Thông tin cập nhật.</param>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="200">Cập nhật thành công.</response>
    /// <response code="400">Dữ liệu không hợp lệ.</response>
    /// <response code="401">Chưa đăng nhập.</response>
    /// <response code="403">Không phải quản trị hệ thống.</response>
    /// <response code="404">Không tìm thấy người dùng.</response>
    /// <response code="409">Tên đăng nhập đã thuộc về tài khoản khác.</response>
    [HttpPut("{id:guid}")]
    [ChiQuanTri]
    [ProducesResponseType(typeof(NguoiDungDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CapNhatAsync(Guid id, [FromBody] LuuNguoiDungRequest yeuCau, CancellationToken ct)
    {
        if (yeuCau is null)
        {
            return LoiDuLieu("Thiếu dữ liệu người dùng.");
        }

        var nguoiDung = await _db.Set<SysUser>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (nguoiDung is null)
        {
            return LoiKhongTimThay("Không tìm thấy người dùng.");
        }

        var kiemTra = await KiemTraChungAsync(yeuCau, id, ct);
        if (kiemTra is not null)
        {
            return kiemTra;
        }

        var vaiTroMoi = yeuCau.VaiTro.Trim().ToUpperInvariant();
        var trangThaiMoi = yeuCau.TrangThai == 0 ? 0 : 1;

        // Chan tu khoa chinh minh / tu ha quyen chinh minh -> tranh mat het tai khoan quan tri.
        if (_hienTai.UserId == id && (vaiTroMoi != VaiTro.QuanTri || trangThaiMoi != 1))
        {
            return LoiDuLieu("Không thể tự khoá tài khoản hoặc tự bỏ quyền quản trị của chính mình.");
        }

        nguoiDung.UserName = yeuCau.UserName.Trim().ToLowerInvariant();
        nguoiDung.FullName = yeuCau.FullName.Trim();
        nguoiDung.Email = string.IsNullOrWhiteSpace(yeuCau.Email) ? null : yeuCau.Email.Trim();
        nguoiDung.UnitCode = yeuCau.UnitCode.Trim();
        nguoiDung.ChucVu = string.IsNullOrWhiteSpace(yeuCau.ChucVu) ? null : yeuCau.ChucVu.Trim();
        nguoiDung.VaiTro = vaiTroMoi;
        nguoiDung.TrangThai = trangThaiMoi;
        nguoiDung.MaxConcurrentTasks = yeuCau.MaxConcurrentTasks;

        if (!string.IsNullOrWhiteSpace(yeuCau.Password))
        {
            if (yeuCau.Password.Length < 8)
            {
                return LoiDuLieu("Mật khẩu mới phải có ít nhất 8 ký tự.");
            }

            nguoiDung.PasswordHash = _bam.Bam(yeuCau.Password);

            // Doi mat khau thi vo hieu phien cu.
            nguoiDung.RefreshToken = null;
            nguoiDung.RefreshTokenHetHan = null;
        }

        // Tai khoan bi khoa thi cat luon refresh token dang co (§9.3 dieu 1).
        if (nguoiDung.TrangThai == 0)
        {
            nguoiDung.RefreshToken = null;
            nguoiDung.RefreshTokenHetHan = null;
        }

        await _db.SaveChangesAsync(ct);

        var tenDonVi = await TenDonViAsync(nguoiDung.UnitCode, ct);
        return Ok(AnhXa.SangDto(nguoiDung, tenDonVi));
    }

    // ------------------------------------------------------- I2 / I3: ho so nang luc

    /// <summary>
    /// I2 — Năng lực suy diễn từ lịch sử: số nhiệm vụ đã nghiệm thu theo từng lĩnh vực.
    /// CHỈ ĐỂ XEM — §9.2 / §10.1: hệ gốc không lưu chuyên môn người dùng, app mới
    /// không nhập tay mà suy từ dữ liệu vận hành.
    /// </summary>
    /// <param name="id">Định danh người dùng.</param>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="200">Bảng năng lực theo lĩnh vực.</response>
    /// <response code="401">Chưa đăng nhập.</response>
    /// <response code="403">Ngoài phạm vi được phép xem (§6.2 dòng 22).</response>
    /// <response code="404">Không tìm thấy người dùng.</response>
    [HttpGet("{id:guid}/nang-luc")]
    [ProducesResponseType(typeof(NangLucNguoiDungDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NangLucAsync(Guid id, CancellationToken ct)
    {
        if (!await DuocXemHoSoAsync(id, ct))
        {
            return LoiKhongCoQuyen("Bạn không có quyền xem hồ sơ năng lực của người dùng này.");
        }

        if (!await TonTaiNguoiDungAsync(id, ct))
        {
            return LoiKhongTimThay("Không tìm thấy người dùng.");
        }

        var ketQua = await _thongKe.NangLucAsync(id, ct);
        return Ok(ketQua);
    }

    /// <summary>
    /// I3 — Chỉ số hiệu quả (§4.8 <c>USER_HIEUSUAT</c> + view tải hiện tại),
    /// đầu vào cho các đặc trưng S2 / S3 / S4 của §9.4.
    /// </summary>
    /// <param name="id">Định danh người dùng.</param>
    /// <param name="linhvuc">Mã lĩnh vực; bỏ trống để lấy số tổng hợp mọi lĩnh vực.</param>
    /// <param name="ct">Thẻ huỷ yêu cầu.</param>
    /// <response code="200">Chỉ số hiệu quả.</response>
    /// <response code="401">Chưa đăng nhập.</response>
    /// <response code="403">Ngoài phạm vi được phép xem (§6.2 dòng 22).</response>
    /// <response code="404">Không tìm thấy người dùng.</response>
    [HttpGet("{id:guid}/hieu-suat")]
    [ProducesResponseType(typeof(HieuSuatNguoiDungDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HieuSuatAsync(
        Guid id,
        [FromQuery] string? linhvuc,
        CancellationToken ct)
    {
        if (!await DuocXemHoSoAsync(id, ct))
        {
            return LoiKhongCoQuyen("Bạn không có quyền xem chỉ số hiệu quả của người dùng này.");
        }

        if (!await TonTaiNguoiDungAsync(id, ct))
        {
            return LoiKhongTimThay("Không tìm thấy người dùng.");
        }

        var ma = string.IsNullOrWhiteSpace(linhvuc) ? null : linhvuc.Trim();
        var ketQua = await _thongKe.HieuSuatAsync(id, ma, ct);
        return Ok(ketQua);
    }

    // ------------------------------------------------------------------ noi bo

    private Task<bool> TonTaiNguoiDungAsync(Guid id, CancellationToken ct) =>
        _db.Set<SysUser>().AsNoTracking().AnyAsync(x => x.Id == id, ct);

    /// <summary>
    /// Tra ten don vi theo ma. Dung truy van rieng thay cho Include() de tang API
    /// khong phu thuoc cau hinh navigation cua tang Infrastructure.
    /// </summary>
    private Task<string?> TenDonViAsync(string unitCode, CancellationToken ct) =>
        _db.Set<SysUnit>()
            .AsNoTracking()
            .Where(x => x.UnitCode == unitCode)
            .Select(x => x.TenDonVi)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// §6.2 dong 22 — pham vi duoc xem ho so cua nguoi khac.
    /// QUAN_TRI: tat ca. NGUOI_GIAO: don vi minh + don vi con. NGUOI_THUC_HIEN: chi chinh minh.
    /// </summary>
    private async Task<bool> DuocXemHoSoAsync(Guid idMucTieu, CancellationToken ct)
    {
        if (_hienTai.LaQuanTri)
        {
            return true;
        }

        var userId = _hienTai.UserId;
        if (userId is null)
        {
            return false;
        }

        if (userId.Value == idMucTieu)
        {
            return true;
        }

        if (!_hienTai.LaBenGiao)
        {
            return false;
        }

        var maDonViMucTieu = await _db.Set<SysUser>()
            .AsNoTracking()
            .Where(x => x.Id == idMucTieu)
            .Select(x => x.UnitCode)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(maDonViMucTieu))
        {
            return false;
        }

        var phamVi = await _phamVi.DonViVaConAsync(_hienTai.UnitCode, ct);
        return phamVi.Contains(maDonViMucTieu);
    }

    /// <summary>
    /// Kiem tra chung cho tao moi va cap nhat. Tra ve null neu hop le.
    /// </summary>
    /// <param name="yeuCau">Du lieu gui len.</param>
    /// <param name="idDangSua">Id ban ghi dang sua; null khi tao moi.</param>
    /// <param name="ct">The huy yeu cau.</param>
    private async Task<IActionResult?> KiemTraChungAsync(
        LuuNguoiDungRequest yeuCau,
        Guid? idDangSua,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(yeuCau.UserName))
        {
            return LoiDuLieu("Tên đăng nhập không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(yeuCau.FullName))
        {
            return LoiDuLieu("Họ tên không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(yeuCau.UnitCode))
        {
            return LoiDuLieu("Đơn vị không được để trống.");
        }

        var vaiTro = yeuCau.VaiTro?.Trim().ToUpperInvariant();
        if (!VaiTro.HopLe(vaiTro))
        {
            return LoiDuLieu($"Vai trò không hợp lệ. Chỉ chấp nhận: {string.Join(", ", VaiTro.ToanBo())}.");
        }

        if (yeuCau.MaxConcurrentTasks < 1 || yeuCau.MaxConcurrentTasks > 100)
        {
            return LoiDuLieu("Ngưỡng số nhiệm vụ đồng thời phải nằm trong khoảng từ 1 đến 100.");
        }

        var maDonVi = yeuCau.UnitCode.Trim();
        var coDonVi = await _db.Set<SysUnit>().AsNoTracking().AnyAsync(x => x.UnitCode == maDonVi, ct);
        if (!coDonVi)
        {
            return LoiDuLieu($"Không tìm thấy đơn vị có mã \"{maDonVi}\".");
        }

        var ten = yeuCau.UserName.Trim().ToLowerInvariant();

        // Tinh san gia tri loai tru de bieu thuc LINQ khong chua Nullable<Guid>.Value
        // (mot so provider EF khong dich duoc). Guid.Empty khong trung Id thuc te nao.
        var idLoaiTru = idDangSua ?? Guid.Empty;

        var daCo = await _db.Set<SysUser>()
            .AsNoTracking()
            .AnyAsync(x => x.UserName.ToLower() == ten && x.Id != idLoaiTru, ct);

        if (daCo)
        {
            return Loi(StatusCodes.Status409Conflict,
                $"Tên đăng nhập \"{ten}\" đã tồn tại.", MaLoiChung.ViPhamRangBuoc);
        }

        return null;
    }
}
