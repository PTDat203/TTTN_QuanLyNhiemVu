# BackEnd — ASP.NET Core 8

API của phân hệ Giao nhiệm vụ. Đặc tả: [../DAC-TA-PHAN-HE-GIAO-NHIEM-VU.md](../DAC-TA-PHAN-HE-GIAO-NHIEM-VU.md)

## Chạy

```bash
dotnet restore
dotnet run --project src/QLNV.Api
```

Swagger: <http://localhost:5000/swagger>

Lần chạy đầu ở môi trường `Development` sẽ tạo CSDL SQLite và nạp dữ liệu mẫu. Muốn dựng lại
từ đầu: xoá tệp `.db` rồi chạy lại.

## Kiểm thử

```bash
dotnet test
```

Bộ test phủ:

| Tệp | Nội dung |
|---|---|
| `MayTrangThaiTests` | Mỗi chuyển T2–T14 của ma trận §2.4 có ít nhất một test hợp lệ và một test chặn |
| `TrangThaiHopLeTests` | Bộ lọc trạng thái theo hạn: còn hạn `{1,2,3}`, quá hạn `{5,7,3}` |
| `QuyenTests` | Bảng phân quyền §6.2 |
| `AiEngineTests` | Từng công thức S1–S5, và bất biến *tổng năm phần đóng góp = điểm tổng* |
| `SeedDataTests` | Dữ liệu mẫu đủ và nhất quán |

Đây là tiêu chí nghiệm thu của tuần 7 trong kế hoạch §8.

## Cấu trúc

```
QLNV.sln
src/QLNV.Core/            Không phụ thuộc project nào — test được không cần CSDL
  Constants/              Mã trạng thái §2, độ khẩn, vai trò
  Entities/               11 bảng theo §4
  Dtos/                   Hợp đồng JSON theo §5 và §9.6
  Abstractions/           Interface cho tầng dưới cài đặt
  Services/
    NhiemVuStateMachine   Máy trạng thái T1–T14 §2.4
    QuyenService          Bảng phân quyền §6.2
    RecommendationService Chấm điểm S1–S5 §9.4 — trọng tâm đề tài
    MetricsService        Precision@1/@3, MRR, Gini, so sánh baseline §9.8
    HanUtil               Lọc trạng thái theo hạn §1.2 bước 5
src/QLNV.Infrastructure/  EF Core, ánh xạ cột, seed, lưu tệp, job nền
src/QLNV.Api/             Controller §5, JWT, Swagger, kiểm quyền
tests/QLNV.Tests/         xUnit + FluentAssertions
```

Lớp `Core` cố ý không tham chiếu EF Core hay ASP.NET. Nhờ vậy máy trạng thái và bộ chấm điểm
là hàm thuần, test được mà không cần dựng CSDL — đó là lý do bộ test chạy trong vài giây.

## Cấu hình

Đọc từ `appsettings.json`, ghi đè bằng biến môi trường.

| Khoá | Mặc định | Ghi chú |
|---|---|---|
| `Database:Provider` | `Sqlite` | Đổi thành `SqlServer` để dùng SQL Server |
| `ConnectionStrings:Default` | tệp SQLite cục bộ | |
| `Jwt:Key` | *(không có)* | **Bắt buộc.** Ứng dụng dừng ngay khi khởi động nếu thiếu |
| `Jwt:Issuer`, `Jwt:Audience` | | |

Không có secret nào nằm trong mã nguồn. Đặc tả §10.11 ghi nhận hệ gốc commit thẳng secret SSO,
`SONAR_TOKEN` và license key vào kho mã — đây là thứ không lặp lại.

Đặt khoá JWT khi phát triển:

```bash
dotnet user-secrets --project src/QLNV.Api set "Jwt:Key" "<chuỗi ngẫu nhiên ít nhất 32 ký tự>"
```

## Chuyển sang SQL Server

```bash
dotnet ef migrations add InitialCreate --project src/QLNV.Infrastructure --startup-project src/QLNV.Api
dotnet ef database update --project src/QLNV.Infrastructure --startup-project src/QLNV.Api
```

Dự án hiện dùng `EnsureCreated()` cho SQLite khi phát triển nên **chưa có tệp migration nào**.
Máy dựng dự án không có .NET SDK, và viết tay migration mà không chạy được `dotnet ef` thì rủi ro
cao hơn nhiều so với việc để lệnh trên tự sinh. Lưu ý: `EnsureCreated()` và migration không dùng
lẫn nhau được — khi chuyển sang migration, xoá CSDL cũ trước.

## Điểm cần biết khi đọc mã

**Tên cột giữ nguyên như hệ gốc.** Thuộc tính C# đặt PascalCase (`TrangThaiDvXuly`) nhưng ánh xạ
xuống cột `trangthaiDvXuly` qua `HasColumnName`, theo ràng buộc §7.4 — để còn đối chiếu ngược
với hệ QLNV khi cần.

**Kiểm quyền hai lớp.** Mỗi endpoint kiểm vai trò rồi kiểm tiếp quyền theo dữ liệu và trạng thái,
trả 403 khi sai. Backend không tin cờ nào do frontend gửi lên. Đặc tả §6.4 nêu rõ lý do: hệ gốc
để backend tính sẵn các cờ `isxuly`, `istuchoi`… nhưng quy tắc tính không tồn tại trong frontend
nên không ai kiểm chứng được.

**Máy trạng thái không nằm trong controller.** Controller chỉ nhận request, gọi
`INhiemVuStateMachine`, trả kết quả. Mọi quy tắc chuyển trạng thái nằm một chỗ duy nhất.
