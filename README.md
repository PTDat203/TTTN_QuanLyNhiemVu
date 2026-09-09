# Mini App Quản lý nhiệm vụ

Đồ án thực tập tốt nghiệp — Phạm Tiến Đạt, MSV 0212966, lớp 66KSCS,
Khoa Công nghệ thông tin, Đại học Xây dựng Hà Nội.
GVHD: TS. Hoàng Nam Thắng. Đơn vị thực tập: Công ty TNHH Giải pháp Công nghệ B&T Việt Nam.

Ứng dụng web quản lý việc giao và theo dõi nhiệm vụ, tích hợp **AI gợi ý người thực hiện
phù hợp** dựa trên kỹ năng, lịch sử thực hiện, tỷ lệ đúng hạn và khối lượng đang xử lý.

---

## Cấu trúc kho mã

| Thư mục | Nội dung |
|---|---|
| `BackEnd/` | ASP.NET Core 8 + EF Core 8 + Oracle 21c XE |
| `FrontEnd/` | Angular 15 |
| `Tài liệu/` | Đặc tả, prompt, bản lưu trữ — **không commit lên git** |

---

## Luồng nghiệp vụ

```text
MANAGER tạo nhiệm vụ            STATUS_CODE = MOI_TAO
   ↓ chọn người thực hiện
Giao nhiệm vụ                   → DA_GIAO
   ↓
EMPLOYEE tiếp nhận              → DANG_THUC_HIEN
   ↓ cập nhật % tiến độ nhiều lần, ghi vào TASK_PROGRESS
Gửi báo cáo kết quả             → CHO_XAC_NHAN
   ↓
MANAGER kiểm tra  ─┬─ đạt      → HOAN_THANH
                   └─ chưa đạt → YEU_CAU_BO_SUNG → quay lại thực hiện
```

Hai vai trò: `MANAGER` giao việc, `EMPLOYEE` thực hiện.

---

## Lược đồ CSDL — 7 bảng

```text
USERS ──┬── 1:N ── USER_SKILLS
        ├── 1:N ── TASKS (CREATOR_ID)
        ├── 1:N ── TASKS (ASSIGNEE_ID)
        ├── 1:N ── TASK_PROGRESS
        ├── 1:N ── TASK_REPORTS (REPORTER_ID / REVIEWER_ID)
        └── 1:N ── TASK_ATTACHMENTS

TASK_STATUS_LOOKUP ── 1:N ── TASKS

TASKS ──┬── 1:N ── TASK_PROGRESS
        ├── 1:N ── TASK_REPORTS
        └── 1:N ── TASK_ATTACHMENTS

TASK_REPORTS ── 1:N ── TASK_ATTACHMENTS  (REPORT_ID nullable)
```

Trạng thái nhiệm vụ nằm ở `TASKS.STATUS_CODE` (FK tới `TASK_STATUS_LOOKUP`).
Trạng thái báo cáo nằm riêng ở `TASK_REPORTS.STATUS`. Hai cơ chế không trộn vào nhau.

Đặc tả đầy đủ: `Tài liệu/DATABASE_SOURCE_OF_TRUTH_TASK_MANAGEMENT.md`

---

## Chạy lần đầu

### Cần cài trước

| Công cụ | Phiên bản | Kiểm tra |
|---|---|---|
| Oracle Database XE | 21c | `lsnrctl status` |
| .NET SDK | 8.0 | `dotnet --version` |
| Node.js | 18 hoặc 20 LTS | `node --version` |

Oracle đã được cài sẵn trên máy phát triển. **`.NET SDK` và `Node.js` thì chưa** — tải tại
<https://dot.net/download> và <https://nodejs.org>.

### 1. Schema Oracle — đã dựng sẵn

**Không chạy script tạo bảng.** Schema `TASK_APP` đã được tạo thủ công và là nguồn chuẩn:
7 bảng, đủ khóa chính, khóa ngoại, ràng buộc CHECK, index và danh mục trạng thái.

Ứng dụng **không** tạo bảng, **không** gọi `EnsureCreated()`, **không** dùng EF Migrations.

Kiểm tra nhanh:

```sql
SELECT TABLE_NAME FROM USER_TABLES ORDER BY TABLE_NAME;
SELECT * FROM TASK_STATUS_LOOKUP ORDER BY SORT_ORDER;
```

Chi tiết schema và các lệnh kiểm tra: [`BackEnd/db/README.md`](BackEnd/db/README.md).

> Các script trong `BackEnd/db/_khong_dung/` là bản cũ hoặc script DROP — **không chạy**.

### 2. Backend

```bash
cd BackEnd
dotnet user-secrets set "ConnectionStrings:OracleConnection" \
  "User Id=TASK_APP;Password=<mật_khẩu>;Data Source=127.0.0.1:1521/XEPDB1;" \
  --project src/TaskApp.Api
dotnet user-secrets set "Jwt:Key" "<chuỗi ngẫu nhiên ít nhất 32 ký tự>" \
  --project src/TaskApp.Api

dotnet restore
dotnet run --project src/TaskApp.Api
```

Swagger: <http://localhost:5000/swagger>

### 3. Frontend

```bash
cd FrontEnd
npm install
npm start
```

<http://localhost:4200>

---

## AI gợi ý người thực hiện

`POST /api/goi-y/nguoi-thuc-hien` trả danh sách ứng viên kèm điểm 0–1 và lý do tiếng Việt.

```text
DiemTong = 0.40·SkillSimilarity + 0.20·HistoryScore + 0.25·OnTimeScore + 0.15·WorkloadScore
```

| Thành phần | Tính từ |
|---|---|
| `SkillSimilarity` | TF-IDF + cosine giữa nội dung nhiệm vụ và `USER_SKILLS`, có trọng số theo `SKILL_LEVEL` |
| `HistoryScore` | Số nhiệm vụ `HOAN_THANH`, thang logarit để người làm nhiều không nuốt hết điểm |
| `OnTimeScore` | Tỷ lệ nghiệm thu trước `DUE_DATE`, làm mượt Laplace để người ít dữ liệu không bị điểm cực đoan |
| `WorkloadScore` | Số nhiệm vụ đang mở, có trọng số theo `PRIORITY` |

Trọng số đọc từ cấu hình, sửa được qua `PUT /api/goi-y/cau-hinh`.

Không dùng LLM, không gọi dịch vụ ngoài, không có bảng lưu kết quả AI — tính theo thời gian
thực. Mô hình là **chấm điểm đa tiêu chí có trọng số**, giải thích được từng con số: điểm tổng
bằng đúng tổng các thành phần đã nhân trọng số. Đây là lựa chọn có chủ đích, vì dữ liệu ban đầu
quá ít để huấn luyện mô hình học máy và môi trường sử dụng đòi hỏi giải thích được.

---

## Ghi chú kỹ thuật

Ba điểm dễ sai khi làm việc với Oracle + EF Core, đã xử lý sẵn trong mã:

**`VARCHAR2` đếm byte, không đếm ký tự.** Một ký tự tiếng Việt có dấu chiếm tới 3 byte trong
`AL32UTF8`, nên mọi cột chứa tiếng Việt đều khai `VARCHAR2(n CHAR)`. Thiếu `CHAR` sẽ gặp
`ORA-12899` ngay ở dữ liệu mẫu.

**EF Core đặt định danh trong nháy kép, Oracle thì viết hoa.** Mọi bảng và cột đều được map
tường minh bằng chữ hoa (`ToTable("USERS")`, `HasColumnName("FULL_NAME")`). Thiếu một cột là
cột đó sinh ra `"PropertyName"` và báo `ORA-00904`.

**Không dùng `Guid` và `DateOnly`.** Khoá chính là `NUMBER` tự tăng, ánh xạ sang `long`;
provider Oracle 8.23.x chưa hỗ trợ `DateOnly`.

Chi tiết: `Tài liệu/CLAUDE_CONTEXT_TASK_MANAGEMENT_DB.md`
