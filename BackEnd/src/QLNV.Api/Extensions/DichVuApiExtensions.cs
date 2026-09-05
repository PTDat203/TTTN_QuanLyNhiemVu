using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using QLNV.Api.Auth;

namespace QLNV.Api.Extensions;

/// <summary>
/// Gom cau hinh ha tang cua tang API: xac thuc JWT, CORS cho Angular, Swagger.
/// Tach khoi Program.cs de tep khoi dong doc duoc trong mot man hinh.
/// </summary>
public static class DichVuApiExtensions
{
    /// <summary>Ten chinh sach CORS danh cho SPA Angular chay o che do phat trien.</summary>
    public const string TenChinhSachCors = "QLNV_Angular";

    /// <summary>Nguon goc mac dinh cua Angular dev server (§7.2: Angular 15).</summary>
    public static readonly string[] NguonGocMacDinh = { "http://localhost:4200", "https://localhost:4200" };

    /// <summary>
    /// §7.2 — xac thuc bang JWT tu phat hanh (thay SSO WSO2/Keycloak cua he goc).
    /// Tham so kiem tra lay tu <see cref="ThamSoKiemTraToken"/> de khong lech voi
    /// <see cref="JwtService"/>.
    /// </summary>
    public static IServiceCollection ThemXacThucJwt(this IServiceCollection dichVu, CauHinhJwt cauHinh)
    {
        ArgumentNullException.ThrowIfNull(dichVu);
        ArgumentNullException.ThrowIfNull(cauHinh);

        dichVu
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(tuyChon =>
            {
                tuyChon.RequireHttpsMetadata = false; // moi truong noi bo / dev chay HTTP
                tuyChon.SaveToken = false;
                tuyChon.TokenValidationParameters = ThamSoKiemTraToken.Tao(cauHinh, kiemTraHan: true);
            });

        dichVu.AddAuthorization();
        return dichVu;
    }

    /// <summary>CORS cho Angular dev server. Danh sach nguon goc doc tu muc cau hinh <c>Cors:Origins</c>.</summary>
    public static IServiceCollection ThemCorsAngular(this IServiceCollection dichVu, IConfiguration cauHinh)
    {
        ArgumentNullException.ThrowIfNull(dichVu);
        ArgumentNullException.ThrowIfNull(cauHinh);

        var nguonGoc = cauHinh.GetSection("Cors:Origins").Get<string[]>();
        if (nguonGoc is null || nguonGoc.Length == 0)
        {
            nguonGoc = NguonGocMacDinh;
        }

        dichVu.AddCors(tuyChon =>
        {
            tuyChon.AddPolicy(TenChinhSachCors, xayDung =>
            {
                xayDung
                    .WithOrigins(nguonGoc)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return dichVu;
    }

    /// <summary>
    /// Swagger co nut Authorize (Bearer) va doc XML doc comment tieng Viet cua controller.
    /// </summary>
    public static IServiceCollection ThemSwaggerQlnv(this IServiceCollection dichVu)
    {
        ArgumentNullException.ThrowIfNull(dichVu);

        dichVu.AddEndpointsApiExplorer();
        dichVu.AddSwaggerGen(tuyChon =>
        {
            tuyChon.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "QLNV - Phân hệ Giao nhiệm vụ tích hợp AI gợi ý người thực hiện",
                Version = "v1",
                Description =
                    "API của app nhỏ theo đặc tả DAC-TA-PHAN-HE-GIAO-NHIEM-VU.md. " +
                    "Mọi đường dẫn dùng tiền tố /api/v1 (§5). Xác thực bằng JWT Bearer (§7.2)."
            });

            var luocDoBaoMat = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Dán access token lấy từ POST /api/v1/auth/login (không cần gõ chữ Bearer).",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            };

            tuyChon.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, luocDoBaoMat);
            tuyChon.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { luocDoBaoMat, Array.Empty<string>() }
            });

            // XML doc comment tieng Viet -> hien thi thang tren Swagger UI.
            var tenTepXml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var duongDanXml = Path.Combine(AppContext.BaseDirectory, tenTepXml);
            if (File.Exists(duongDanXml))
            {
                tuyChon.IncludeXmlComments(duongDanXml, includeControllerXmlComments: true);
            }

            // XML cua QLNV.Core de mo ta DTO (§5, §9.6) cung hien duoc.
            var duongDanXmlCore = Path.Combine(AppContext.BaseDirectory, "QLNV.Core.xml");
            if (File.Exists(duongDanXmlCore))
            {
                tuyChon.IncludeXmlComments(duongDanXmlCore);
            }
        });

        return dichVu;
    }
}
