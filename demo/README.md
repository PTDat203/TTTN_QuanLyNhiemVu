# Demo phân hệ Giao nhiệm vụ + AI gợi ý

Bản mô phỏng chạy được của đặc tả [DAC-TA-PHAN-HE-GIAO-NHIEM-VU.md](../DAC-TA-PHAN-HE-GIAO-NHIEM-VU.md).
Mục đích: **làm rõ hai chức năng chính** — luồng nghiệp vụ giao việc trên hai trục trạng thái,
và AI gợi ý người thực hiện có giải thích được.

## Chạy

Mở thẳng `demo-giao-nhiem-vu.html` bằng trình duyệt. Không cần cài gì, không cần mạng
(trừ phần tải phông chữ). Toàn bộ dữ liệu sinh trong bộ nhớ, tải lại trang là về trạng thái đầu.

Bản trực tuyến: https://claude.ai/code/artifact/3a3b45a3-b42a-4f09-b9c6-161b23bc6eae

## Kịch bản trình chiếu 5 phút

| # | Thao tác | Điểm cần nói |
|---|---|---|
| 1 | Mở **Tổng quan** | 4 thẻ đếm tính thẳng từ 2 trục trạng thái, không cần job thống kê nền như hệ gốc |
| 2 | Bấm **▶ Kịch bản demo** (góc trên) | Hệ tự chạy trọn vòng 7 bước, **gồm cả nhánh "Chưa đạt → làm lại"**. Ruy-băng trên đầu và vai đăng nhập tự đổi theo từng bước |
| 3 | Vào **M05 Phân công nhiệm vụ** → bấm **🤖 AI gợi ý** | Màn hình trọng tâm. Top-5 ứng viên, điểm 0–100, 5 thanh thành phần, lý do tiếng Việt |
| 4 | Bấm **Xem cách tính điểm** ở ứng viên hạng 1 | Câu trả lời cho "vì sao lại là người này": từng công thức S1…S5 thay số thật, cộng lại đúng bằng điểm tổng |
| 5 | Vào **H4 Trọng số mô hình**, kéo `S1` xuống 0,05 rồi mở lại gợi ý | Thứ hạng đổi ngay — chứng minh mô hình là chấm điểm đa tiêu chí, không phải hộp đen |
| 6 | Vào **M13 Nhật ký gợi ý AI** → **Chạy so sánh** | Precision@1/@3, MRR, tỷ lệ chấp nhận so với 3 baseline (ngẫu nhiên / rảnh nhất / chuyên môn nhất) |

## Những chỗ nên chủ động nói trước khi bị hỏi

- **Đây là MCDM chấm điểm có trọng số, không phải mạng nơ-ron.** Chọn có chủ đích: dữ liệu
  ban đầu quá ít để huấn luyện, và môi trường hành chính bắt buộc giải thích được (§9.9).
- **Không có bảng khai báo năng lực.** Cả 4 yếu tố đề tài yêu cầu đều suy ra từ chính lịch sử
  nhiệm vụ, vì hệ gốc QLNV không lưu chuyên môn người dùng (§9.2, §10.1).
- **Bước "Tiếp nhận" là thiết kế mới**, hệ gốc không có nút / API / trạng thái nào (§10.3).
  Trong màn **§2 Máy trạng thái**, dòng T2 được gắn nhãn "thiết kế mới".
- **Ngưỡng bão hoà chuyên môn = 5.** Người làm 15 việc và người làm 5 việc cùng lĩnh vực đều
  được S1 = 1,00. Đây là đúng đặc tả §9.4, và chỉnh được ở màn H4.
- **Bắt buộc nhập lý do** khi từ chối / trả lại là **yêu cầu mới**, hệ gốc không bắt buộc (§10.7).

## Cấu trúc

Một tệp HTML, bên trong là 5 khối độc lập:

| Khối | Vai trò | Mục đặc tả |
|---|---|---|
| `SeedData` | 22 người dùng, 8 lĩnh vực, 350+ nhiệm vụ, 70+ bản ghi nhật ký AI | §4, §8 T9 |
| `Flow` | Máy trạng thái T1–T14, bảng quyền, lọc trạng thái theo hạn, job quá hạn | §1.2, §2, §6.2 |
| `AiEngine` | S1…S5, lọc cứng, cold start, sinh lý do, ghi nhật ký | §9.2–§9.6 |
| `Metrics` | Precision@1/@3, MRR, Gini, so sánh baseline, dashboard | §9.8, §5.10 |
| Lớp giao diện | 8 màn hình + 5 hộp thoại | §3 |

Chuyển sang bản thật (Angular 15 + ASP.NET Core theo §7.2): `AiEngine` và `Flow` là hai khối
port gần như 1–1 sang C#; `SeedData` thành script sinh dữ liệu; lớp giao diện viết lại bằng
Kendo + Nebular.

## Giới hạn của bản demo

- Không có backend, không đăng nhập thật, không upload tệp — đổi vai bằng ô chọn góc trên phải.
- Không kiểm quyền 2 lớp như §6.4 (chỉ có 1 lớp phía trình duyệt) vì không có máy chủ.
- Nhánh **phối hợp** chỉ ở mức xem, đúng như quyết định lược bỏ ở §6.3.
- Dữ liệu là mô phỏng có chủ đích để 8 chân dung người dùng cho ra thứ hạng khác nhau rõ rệt;
  các con số Precision **không phải** kết quả đo trên dữ liệu thật.
