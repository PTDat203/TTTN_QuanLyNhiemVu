using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QLNV.Api.Auth;
using QLNV.Api.Common;
using QLNV.Api.Extensions;
using QLNV.Api.Middleware;
using QLNV.Api.Services;
using QLNV.Api.Validators;
using QLNV.Core.Abstractions;
using QLNV.Core.Common;
using QLNV.Infrastructure;
using QLNV.Infrastructure.Data;

// =====================================================================================
//  QLNV.Api - diem khoi dong (§5 danh sach API, §6.4 thuc thi phan quyen, §7.2 stack).
//
//  HOP DONG VOI QLNV.Infrastructure (tang duoi phai cung cap DUNG ba thu nay):
//    1. QLNV.Infrastructure.DangKyHaTang.AddInfrastructure(IServiceCollection, IConfiguration)
//       - dang ky DbContext theo cau hinh Database:Provider (sqlite mac dinh / sqlserver)
//         va cac cai dat cua interface trong QLNV.Core.Abstractions:
//         IQuyenService, INhiemVuStateMachine, IRecommendationService, IThongKeService,
//         IMetricsService, IFileStorage.
//    2. QLNV.Infrastructure.Data.QlnvDbContext - lop DbContext cu the.
//    3. QLNV.Infrastructure.Data.DbInitializer - lop THUONG, ctor (QlnvDbContext,
//       IPasswordHasher), co ham KhoiTaoAsync(...) dung luoc do + nap du lieu mau
//       (§8 giai doan 1). Tep nay tu new no ra nen khong can no duoc dang ky trong DI.
//  Neu tang Infrastructure dat ten khac, CHI phai sua dung hai dong duoi day
//  (AddInfrastructure + AddScoped<DbContext>) va khoi seed o cuoi tep:
//  cac controller chi phu thuoc kieu co so Microsoft.EntityFrameworkCore.DbContext
//  va truy cap bang DbContext.Set<TEntity>(), khong dung ten thuoc tinh DbSet nao.
// =====================================================================================

// §6.1 - giu nguyen ten claim do he thong tu phat hanh ("sub", "vaitro", "unitcode"...),
// khong de thu vien anh xa sang URI dai cua WS-Federation.
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------------------------------
// 1. Cau hinh JWT (§7.2, §10.11)
//    Thu tu uu tien khoa ky: bien moi truong QLNV_JWT_KEY  ->  muc cau hinh Jwt:Key.
//    TUYET DOI khong co gia tri mac dinh trong ma nguon; thieu khoa thi dung ung dung.
// -------------------------------------------------------------------------------------
var cauHinhJwt = builder.Configuration.GetSection(CauHinhJwt.TenMuc).Get<CauHinhJwt>() ?? new CauHinhJwt();

var khoaTuMoiTruong = builder.Configuration["QLNV_JWT_KEY"];
if (!string.IsNullOrWhiteSpace(khoaTuMoiTruong))
{
    cauHinhJwt.Key = khoaTuMoiTruong.Trim();
}

if (string.IsNullOrWhiteSpace(cauHinhJwt.Key))
{
    throw new InvalidOperationException(
        "Thiếu khoá ký JWT. Hãy đặt biến môi trường QLNV_JWT_KEY (khuyến nghị) " +
        "hoặc mục cấu hình Jwt:Key qua dotnet user-secrets. " +
        "Theo §10.11, khoá ký tuyệt đối không được ghi sẵn trong mã nguồn hay commit vào kho mã.");
}

if (Encoding.UTF8.GetByteCount(cauHinhJwt.Key) < 32)
{
    throw new InvalidOperationException(
        "Khoá ký JWT quá ngắn: thuật toán HS256 yêu cầu tối thiểu 32 byte (256 bit). " +
        "Hãy sinh một khoá ngẫu nhiên đủ dài, ví dụ bằng lệnh: " +
        "openssl rand -base64 48");
}

if (cauHinhJwt.SoPhutSongAccessToken <= 0 || cauHinhJwt.SoNgaySongRefreshToken <= 0)
{
    throw new InvalidOperationException(
        "Cấu hình Jwt:SoPhutSongAccessToken và Jwt:SoNgaySongRefreshToken phải là số dương.");
}

builder.Services.AddSingleton(cauHinhJwt);

// -------------------------------------------------------------------------------------
// 2. Tang ha tang (EF Core, seed, luu tep, cac cai dat interface cua QLNV.Core)
// -------------------------------------------------------------------------------------
builder.Services.AddInfrastructure(builder.Configuration);

// Controller chi phu thuoc kieu co so DbContext + Set<TEntity>() -> doi ten lop DbContext
// cu the o Infrastructure chi phai sua dung dong nay.
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<QlnvDbContext>());

// -------------------------------------------------------------------------------------
// 3. Dich vu cua tang API
// -------------------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IHienTai, HienTaiService>();
builder.Services.AddScoped<PhamViDonViService>();

// Tro giup dung chung cho cac controller nghiep vu (VanBan / NhiemVu / GiaHan /
// NghiemThu): nap ngu canh §6.2, ghi ket xuat may trang thai, gan tep dinh kem.
builder.Services.AddScoped<TroGiupNghiepVu>();

// TryAdd: neu tang Infrastructure da dang ky ban bam mat khau rieng thi giu ban do,
// nguoc lai dung ban PBKDF2 thuan BCL cua tang API.
builder.Services.TryAddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

builder.Services
    .AddControllers()
    .AddJsonOptions(tuyChon =>
    {
        var json = tuyChon.JsonSerializerOptions;

        // Hop dong §5/§9.6 dung camelCase; rieng cac khoa lech chuan
        // (trangthaiDvXuly, hsChatluong, hanxulyth...) da duoc chot bang
        // [JsonPropertyName] ngay tren DTO o QLNV.Core (§7.4 muc 1) va se thang.
        json.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        json.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        json.PropertyNameCaseInsensitive = true;

        // CO Y giu khoa co gia tri null: §9.6 khai soLieu.mucThanhThao va diemChatLuongTb
        // luon null o phien ban 1 nhung PHAI con khoa de FE khong in ra "undefined".
        json.DefaultIgnoreCondition = JsonIgnoreCondition.Never;

        // Khong escape ky tu tieng Viet thanh \uXXXX -> nhat ky va Swagger de doc.
        json.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

// Cung bo tuy chon cho cac phan hoi ghi thang ra HttpResponse (ProblemDetails o middleware).
builder.Services.ConfigureHttpJsonOptions(tuyChon =>
{
    tuyChon.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    tuyChon.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    tuyChon.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

// FluentValidation: tu dong kiem tra model truoc khi vao action.
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<DangNhapRequestValidator>();

// 400 do [ApiController] sinh ra cung phai la ProblemDetails tieng Viet.
builder.Services.Configure<ApiBehaviorOptions>(tuyChon =>
{
    tuyChon.InvalidModelStateResponseFactory = nguCanh =>
    {
        var vanDe = new ValidationProblemDetails(nguCanh.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = LoiApi.TieuDe(StatusCodes.Status400BadRequest),
            Detail = "Dữ liệu gửi lên không hợp lệ. Vui lòng kiểm tra lại các trường được liệt kê.",
            Instance = nguCanh.HttpContext.Request.Path
        };

        vanDe.Extensions[LoiApi.KhoaThongBao] = vanDe.Detail;
        vanDe.Extensions[LoiApi.KhoaMaLoi] = MaLoiChung.DuLieuKhongHopLe;
        vanDe.Extensions[LoiApi.KhoaMaVet] = nguCanh.HttpContext.TraceIdentifier;

        return new ObjectResult(vanDe)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { LoiApi.LoaiNoiDung }
        };
    };
});

builder.Services.ThemXacThucJwt(cauHinhJwt);
builder.Services.ThemCorsAngular(builder.Configuration);
builder.Services.ThemSwaggerQlnv();

var app = builder.Build();

// -------------------------------------------------------------------------------------
// 4. Duong ong xu ly yeu cau
//    Middleware bat loi phai o NGOAI CUNG de bao duoc ca 401/403 cua tang xac thuc.
// -------------------------------------------------------------------------------------
app.UseXuLyLoiTapTrung();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(tuyChon =>
    {
        tuyChon.SwaggerEndpoint("/swagger/v1/swagger.json", "QLNV API v1");
        tuyChon.DocumentTitle = "QLNV API - Giao nhiệm vụ";
    });
}

app.UseCors(DichVuApiExtensions.TenChinhSachCors);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Kiem tra suc khoe - khong yeu cau dang nhap.
app.MapGet("/api/v1/health", () => Results.Ok(new
{
    trangThai = "OK",
    thoiGian = DateTime.UtcNow
})).AllowAnonymous();

// -------------------------------------------------------------------------------------
// 5. Seed du lieu mau - CHI o moi truong Development (§8 giai doan 1)
// -------------------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    using (var phamVi = app.Services.CreateScope())
    {
        // DbInitializer la lop THUONG (khong phai lop tinh). Tu dung the hien tu
        // QlnvDbContext + IPasswordHasher de khong phu thuoc viec tang Infrastructure
        // co dang ky no trong DI hay khong.
        var db = phamVi.ServiceProvider.GetRequiredService<QlnvDbContext>();
        var bamMatKhau = phamVi.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var khoiTao = new DbInitializer(db, bamMatKhau);

        // dungMigration: false -> EnsureCreatedAsync (chua co thu muc Migrations).
        var thongDiep = await khoiTao.KhoiTaoAsync(
            dungMigration: false,
            napDuLieuMau: true,
            ct: CancellationToken.None);

        app.Logger.LogInformation("Khởi tạo cơ sở dữ liệu: {ThongDiep}", thongDiep);
    }
}

app.Run();

/// <summary>
/// Lop Program duoc mo rong de du an kiem thu (QLNV.Tests) dung duoc
/// <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program
{
}
