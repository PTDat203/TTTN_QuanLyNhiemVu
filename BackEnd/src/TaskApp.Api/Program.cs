using Microsoft.EntityFrameworkCore;
using TaskApp.Api.Common;
using TaskApp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Ket noi Oracle
// ---------------------------------------------------------------------------
// Schema TASK_APP da duoc tao san bang script SQL chay tay. Ung dung KHONG tao bang,
// KHONG goi EnsureCreated(), KHONG dung Migrations. Database la nguon chuan.
var chuoiKetNoi = builder.Configuration.GetConnectionString("OracleConnection");

if (string.IsNullOrWhiteSpace(chuoiKetNoi))
{
    // Dung ngay luc khoi dong voi thong bao chi ro cach khac phuc, thay vi de loi mo ho
    // xuat hien o truy van dau tien. Mat khau khong duoc nam trong ma nguon (§10.11).
    throw new InvalidOperationException(
        "Thiếu chuỗi kết nối 'ConnectionStrings:OracleConnection'.\n" +
        "Đặt bằng lệnh:\n" +
        "  dotnet user-secrets set \"ConnectionStrings:OracleConnection\" " +
        "\"User Id=TASK_APP;Password=<mật_khẩu>;Data Source=127.0.0.1:1521/XEPDB1;\" " +
        "--project src/TaskApp.Api");
}

builder.Services.AddDbContext<TaskDbContext>(options =>
{
    options.UseOracle(chuoiKetNoi, oracle =>
    {
        // Phai khop phien ban Oracle THAT dang ket noi toi. May nay chay 21c XE.
        // Dat sai se khien provider sinh SQL theo cu phap ban Oracle khac —
        // loi chi lo ra luc chay truy van, khong lo luc build.
        oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion21);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});

// ---------------------------------------------------------------------------
// Dich vu web
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TaskApp API",
        Version = "v1",
        Description = "Mini app Quản lý nhiệm vụ — Oracle 21c XE, schema TASK_APP"
    });
});

var cacNguonChoPhep = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                      ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(cacNguonChoPhep)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
app.SuDungXuLyLoi();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

// ---------------------------------------------------------------------------
// Endpoint chan doan ket noi + anh xa
// ---------------------------------------------------------------------------
// Doc thu ca 7 bang. Dung de xac nhan anh xa EF Core khop schema Oracle that:
// neu mot cot map sai ten, Oracle bao ORA-00904 va endpoint nay chi ra ngay bang nao hong.
app.MapGet("/api/he-thong/kiem-tra-ket-noi", async (TaskDbContext db, CancellationToken ct) =>
{
    var kq = await KiemTraKetNoi.ChayAsync(db, ct);
    return kq.KetNoiDuoc ? Results.Ok(kq) : Results.Problem(kq.LoiKetNoi, statusCode: 503);
})
.WithName("KiemTraKetNoi")
.WithTags("Hệ thống");

// Vat thu mot dong moi bang — kiem duoc ca TEN COT, thu ma COUNT(*) khong kiem duoc.
app.MapGet("/api/he-thong/kiem-tra-anh-xa", async (TaskDbContext db, CancellationToken ct) =>
{
    var ds = await KiemTraKetNoi.DocThuMotDongAsync(db, ct);
    return ds.All(x => x.AnhXaDung) ? Results.Ok(ds) : Results.Problem(
        "Có bảng ánh xạ sai — xem chi tiết trong danh sách.", statusCode: 500);
})
.WithName("KiemTraAnhXa")
.WithTags("Hệ thống");

// ---------------------------------------------------------------------------
// Tu kiem luc khoi dong — chi ghi log, khong chan ung dung chay
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("KhoiDong");
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();

    try
    {
        var kq = await KiemTraKetNoi.ChayAsync(db);

        if (!kq.KetNoiDuoc)
        {
            log.LogError("Không kết nối được Oracle: {Loi}", kq.LoiKetNoi);
        }
        else
        {
            log.LogInformation("Kết nối Oracle thành công. {PhienBan}", kq.PhienBanOracle);

            foreach (var bang in kq.CacBang)
            {
                if (bang.AnhXaDung)
                {
                    log.LogInformation("  {Bang}: {SoDong} dòng", bang.Bang, bang.SoDong);
                }
                else
                {
                    log.LogError("  {Bang}: ÁNH XẠ SAI — {Loi}", bang.Bang, bang.Loi);
                }
            }

            if (kq.CacBang.All(b => b.SoDong == 0))
            {
                log.LogWarning(
                    "Toàn bộ bảng đang rỗng. Chưa có tài khoản nào nên chưa đăng nhập được.");
            }
        }
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Lỗi khi tự kiểm tra kết nối lúc khởi động");
    }
}

app.Run();
