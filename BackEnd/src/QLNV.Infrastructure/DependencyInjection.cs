using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QLNV.Core.Abstractions;
using QLNV.Infrastructure.Ai;
using QLNV.Infrastructure.Data;
using QLNV.Infrastructure.Jobs;
using QLNV.Infrastructure.Security;
using QLNV.Infrastructure.Storage;

namespace QLNV.Infrastructure;

/// <summary>
/// Dang ky toan bo dich vu cua tang ha tang.
///
/// Cau hinh doc tu <c>appsettings.json</c>:
/// <code>
/// "Database": {
///   "Provider": "Sqlite",              // "Sqlite" (mac dinh) | "SqlServer"
///   "AutoMigrate": false               // true khi da co thu muc Migrations
/// },
/// "ConnectionStrings": {
///   "Default": "Data Source=App_Data/qlnv.db"
/// },
/// "Storage": {
///   "ThuMucGoc": "App_Data/files"
/// }
/// </code>
///
/// §10.11: KHONG hard-code chuoi ket noi hay secret trong ma nguon. Chuoi ket noi mac dinh
/// duoi day chi la tep SQLite cuc bo (khong chua mat khau) de chay duoc ngay khi phat trien.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Ten provider SQLite trong cau hinh.</summary>
    public const string ProviderSqlite = "Sqlite";

    /// <summary>Ten provider SQL Server trong cau hinh.</summary>
    public const string ProviderSqlServer = "SqlServer";

    /// <summary>Chuoi ket noi SQLite mac dinh khi cau hinh khong khai bao gi.</summary>
    public const string ChuoiKetNoiSqliteMacDinh = "Data Source=App_Data/qlnv.db";

    /// <summary>
    /// Dang ky DbContext, kho luu tep, bam mat khau, nguon du lieu AI va hai job nen.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string provider = configuration["Database:Provider"] ?? ProviderSqlite;
        string? chuoiKetNoi = configuration.GetConnectionString("Default");

        services.AddDbContext<QlnvDbContext>(options =>
        {
            if (string.Equals(provider, ProviderSqlServer, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(chuoiKetNoi))
                {
                    throw new InvalidOperationException(
                        "Thiếu chuỗi kết nối \"ConnectionStrings:Default\" khi dùng SQL Server. " +
                        "Hãy khai báo trong appsettings hoặc biến môi trường.");
                }

                options.UseSqlServer(chuoiKetNoi, sql => sql.EnableRetryOnFailure());
            }
            else
            {
                // Mac dinh: SQLite — chay duoc ngay, khong can cai dat gi them
                string chuoi = string.IsNullOrWhiteSpace(chuoiKetNoi) ? ChuoiKetNoiSqliteMacDinh : chuoiKetNoi;
                BaoDamThuMucSqlite(chuoi);
                options.UseSqlite(chuoi);
            }
        });

        // §5.7 — kho luu tep cuc bo (thay MinIO o luot nay)
        var tuyChonTep = new TuyChonLuuTep();
        string? thuMucTep = configuration["Storage:ThuMucGoc"];
        if (!string.IsNullOrWhiteSpace(thuMucTep)) tuyChonTep.ThuMucGoc = thuMucTep;

        // Dung factory tuong minh: hai lop duoi day co tham so ham dung co GIA TRI MAC DINH,
        // ma bo chua DI mac dinh cua .NET khong biet dung gia tri mac dinh do.
        services.AddSingleton(tuyChonTep);
        services.AddSingleton<IFileStorage>(sp => new LocalFileStorage(sp.GetRequiredService<TuyChonLuuTep>()));

        // §10.11 — bam mat khau bang BCrypt
        services.AddSingleton<IPasswordHasher>(_ => new BcryptPasswordHasher());

        // §9 — nguon du lieu tho cho bo cham diem AI
        services.AddScoped<IAiDataSource, EfAiDataSource>();

        // §5.9 I4 / I5 — hai job nen goi tay (khong dung Hangfire, xem ghi chu trong tung lop)
        services.AddScoped<CapNhatQuaHanJob>();
        services.AddScoped<TongHopHieuSuatJob>();

        // Khoi tao CSDL + nap du lieu mau
        services.AddScoped<DbInitializer>();

        return services;
    }

    /// <summary>
    /// Doc cau hinh <c>Database:AutoMigrate</c> — tang API dung de quyet dinh goi
    /// <c>MigrateAsync</c> hay <c>EnsureCreatedAsync</c> luc khoi dong.
    /// </summary>
    public static bool DungMigration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return bool.TryParse(configuration["Database:AutoMigrate"], out bool bat) && bat;
    }

    /// <summary>
    /// Tao san thu muc chua tep CSDL SQLite. Khong co buoc nay thi lan chay dau tien
    /// se hong voi loi "unable to open database file" khi thu muc <c>App_Data</c> chua ton tai.
    /// </summary>
    private static void BaoDamThuMucSqlite(string chuoiKetNoi)
    {
        // Chuoi ket noi SQLite dang "Data Source=<duong dan>;..." — lay phan duong dan
        foreach (var phan in chuoiKetNoi.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int dau = phan.IndexOf('=');
            if (dau <= 0) continue;

            string khoa = phan.Substring(0, dau).Trim();
            if (!khoa.Equals("Data Source", StringComparison.OrdinalIgnoreCase)
                && !khoa.Equals("DataSource", StringComparison.OrdinalIgnoreCase)
                && !khoa.Equals("Filename", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string duongDan = phan.Substring(dau + 1).Trim();
            if (duongDan.Length == 0
                || duongDan.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
                || duongDan.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                string? thuMuc = Path.GetDirectoryName(Path.GetFullPath(duongDan));
                if (!string.IsNullOrEmpty(thuMuc)) Directory.CreateDirectory(thuMuc);
            }
            catch (IOException)
            {
                // De EF bao loi ro rang hon o buoc mo ket noi
            }
            catch (UnauthorizedAccessException)
            {
                // Nhu tren
            }

            return;
        }
    }
}
