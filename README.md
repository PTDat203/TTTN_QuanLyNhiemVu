# Phân hệ Giao nhiệm vụ tích hợp AI gợi ý người thực hiện

Đồ án thực tập — Phạm Tiến Đạt, MSV 0212966, lớp 66KSCS, Khoa Công nghệ thông tin,
Đại học Xây dựng Hà Nội. GVHD: TS. Hoàng Nam Thắng. Đơn vị thực tập: Công ty TNHH Giải pháp
Công nghệ B&T Việt Nam.

Dựng lại nghiệp vụ giao nhiệm vụ của hệ thống **QLNV** thành một ứng dụng web nhỏ, bổ sung
chức năng **AI gợi ý người thực hiện phù hợp** dựa trên chuyên môn, lịch sử thực hiện,
hiệu quả công việc và khối lượng hiện tại.

Đặc tả đầy đủ: [DAC-TA-PHAN-HE-GIAO-NHIEM-VU.md](DAC-TA-PHAN-HE-GIAO-NHIEM-VU.md)

---

## Cấu trúc kho mã

| Thư mục | Nội dung |
|---|---|
| [`BackEnd/`](BackEnd/) | ASP.NET Core 8 + EF Core 8. Nghiệp vụ, API, CSDL, kiểm thử. |
| [`FrontEnd/`](FrontEnd/) | Angular 15.2.10 + Nebular 11. Giao diện 13 màn hình. |
| [`demo/`](demo/) | Bản mô phỏng một tệp HTML dựng ở giai đoạn phân tích. Không phải mã nguồn dự án — giữ lại để đối chiếu nghiệp vụ và trình bày nhanh. |
| `DAC-TA-PHAN-HE-GIAO-NHIEM-VU.md` | Đặc tả gốc, dựng từ việc đọc trực tiếp mã nguồn hệ QLNV. |

Hai phần chạy độc lập và giao tiếp qua HTTP. Backend không phục vụ tệp tĩnh của frontend;
khi phát triển, Angular gọi API qua proxy.

---

## Chạy lần đầu

### Cần cài trước

| Công cụ | Phiên bản | Kiểm tra |
|---|---|---|
| .NET SDK | 8.0 | `dotnet --version` |
| Node.js | 18 hoặc 20 LTS | `node --version` |

Máy dựng dự án này **chưa cài cả hai**. Tải tại <https://dot.net/download> và
<https://nodejs.org>. Không cần Docker, không cần SQL Server — mặc định dùng SQLite.

### Backend

```bash
cd BackEnd
dotnet restore
dotnet run --project src/QLNV.Api
```

Lần chạy đầu sẽ tạo CSDL SQLite và nạp dữ liệu mẫu (22 người dùng, 8 lĩnh vực,
320+ nhiệm vụ lịch sử). Swagger: <http://localhost:5000/swagger>

### Frontend

Mở cửa sổ dòng lệnh thứ hai:

```bash
cd FrontEnd
npm install
npm start
```

Giao diện: <http://localhost:4200>

### Tài khoản demo

Mật khẩu chung `123456`. Danh sách tài khoản hiện ngay trên màn đăng nhập.
Đổi vai để thấy bộ nút thay đổi theo bảng phân quyền §6.2 — một nhiệm vụ trông rất khác
giữa mắt người giao và mắt người thực hiện.

---

## Hai chức năng cần xem trước

**Vòng nghiệp vụ 7 bước.** Tạo văn bản chỉ đạo → giao nhiệm vụ → tiếp nhận → cập nhật tiến độ
→ gửi báo cáo → kiểm tra kết quả → hoàn thành, có cả nhánh *Chưa đạt → yêu cầu bổ sung → làm lại*.
Trạng thái chạy trên **hai trục song song** (`trangthai` và `trangthaiDvXuly`) đúng như hệ gốc,
không gộp lại.

**AI gợi ý người thực hiện** (`POST /api/v1/ai/goi-y-nguoi-thuc-hien`). Trả top-5 ứng viên kèm
điểm 0–100, năm điểm thành phần và lý do bằng tiếng Việt. Điểm tổng bằng đúng tổng năm phần
đóng góp, nên giải thích được từng con số — yêu cầu bắt buộc với hệ thống dùng trong môi trường
hành chính.

---

## Ba điểm khác hệ gốc, phải nói rõ khi báo cáo

Đặc tả §10 liệt kê những thứ **không sao chép được** từ hệ QLNV vì chúng không tồn tại.
Ba thứ dưới đây là phần **tự thiết kế**, không được trình bày như hiện trạng hệ gốc:

1. **Bước "Tiếp nhận"** — hệ gốc không có nút, không có API, không có trạng thái nào (§10.3).
2. **Bắt buộc nhập lý do** khi từ chối nhiệm vụ và khi trả lại kết quả — hệ gốc không bắt buộc (§10.7).
3. **Chuyên môn người dùng suy từ lịch sử.** Hệ gốc không lưu năng lực người dùng ở bất kỳ
   bảng hay màn hình nào (§10.1), nên thay vì dựng phân hệ hồ sơ năng lực, hệ đếm số nhiệm vụ
   đã nghiệm thu của từng người theo từng lĩnh vực. Hệ quả phải chấp nhận: người mới bị điểm
   chuyên môn thấp trong vài việc đầu — xử lý bằng cơ chế cold start §9.5.

Ngoài ra, `mucdoht` được validate 0–100 (hệ gốc để ô `type="text"`, không kiểm), và
route `/quan-tri/*` có guard ở cả hai phía (hệ gốc không có `canActivate` cho route admin nào).

---

## Tình trạng kiểm chứng

Mã nguồn được rà bằng đọc mã, **chưa từng chạy qua trình biên dịch** vì máy dựng dự án không có
.NET SDK lẫn Node. Lần đầu `dotnet build` và `ng build` nhiều khả năng còn lỗi vặt cần sửa.
Bộ test xUnit trong `BackEnd/tests` phủ ma trận chuyển trạng thái T1–T14 và các công thức AI —
chạy `dotnet test` là biết ngay phần nghiệp vụ có đúng không.
