# FrontEnd — Angular 15

Giao diện mini app Quản lý nhiệm vụ.

> **Trạng thái: chưa dựng.** Thư mục này đang trống, chờ backend chốt xong hợp đồng API.
> Xem mục "Vì sao chưa có mã" bên dưới.

---

## Vì sao chưa có mã

Có một bản Angular dựng ở lượt trước, nhưng nó viết theo lược đồ cũ **11 bảng** của
`DAC-TA-PHAN-HE-GIAO-NHIEM-VU.md`: khoá `Guid`, hai trục trạng thái `trangthai` +
`trangthaiDvXuly`, nhiều người chủ trì trên một nhiệm vụ, gọi `/api/v1/nhiem-vu`.

Lược đồ đã chốt lại thành **7 bảng** trên Oracle: khoá `NUMBER`, một trục `STATUS_CODE`,
một người thực hiện qua `TASKS.ASSIGNEE_ID`. Toàn bộ model và service của bản cũ không còn
khớp — giữ lại sẽ gây nhầm lẫn hơn là tiết kiệm.

Bản cũ được lưu ở `Tài liệu/_luutru-frontend-cu/` để đối chiếu, không xoá.

---

## Sẽ dựng theo

| Lớp | Chọn | Ghi chú |
|---|---|---|
| Framework | Angular 15.x | |
| Giao diện | Nebular 11 + Bootstrap 4.3.1 + FontAwesome 6 | Miễn phí, không vướng license |
| Lưới dữ liệu | Tự viết | Xem ghi chú Kendo bên dưới |
| Gọi API | `HttpClient` + interceptor gắn JWT | |

### Không dùng Kendo UI

Kendo UI for Angular là sản phẩm thương mại và license trong kho mã hệ gốc đã hết hạn
26/12/2024. Nebular + Bootstrap phủ được phần lớn nhu cầu; riêng lưới dữ liệu thì tự viết
một component `data-table` dùng chung. Nếu sau này xin được license của B&T thì chỉ cần
thay ruột component đó, các màn không phải sửa.

---

## Các màn hình dự kiến

| # | Màn | Vai |
|---|---|---|
| 1 | Đăng nhập | chung |
| 2 | Danh sách nhiệm vụ (lọc theo trạng thái, người thực hiện, độ ưu tiên, hạn) | chung |
| 3 | Tạo / sửa nhiệm vụ | MANAGER |
| 4 | Giao nhiệm vụ — **có khu vực AI gợi ý người thực hiện** | MANAGER |
| 5 | Chi tiết nhiệm vụ + lịch sử tiến độ | chung |
| 6 | Cập nhật tiến độ | EMPLOYEE |
| 7 | Gửi báo cáo kết quả | EMPLOYEE |
| 8 | Xác nhận / từ chối báo cáo | MANAGER |
| 9 | Quản lý người dùng và kỹ năng (`USER_SKILLS`) | MANAGER |

Màn số 4 là điểm nhấn của đề tài: gọi `POST /api/goi-y/nguoi-thuc-hien`, hiển thị danh sách
ứng viên kèm điểm và lý do, nhưng **MANAGER vẫn là người quyết định cuối cùng** — AI chỉ hỗ trợ.

Màn số 9 không phải màn phụ: `USER_SKILLS` là nguồn dữ liệu bắt buộc cho phần AI. Không có
dữ liệu kỹ năng thì `SkillSimilarity` bằng 0 cho tất cả mọi người.

---

## Chạy (khi đã dựng xong)

```bash
npm install
npm start
```

<http://localhost:4200> — gọi API qua `proxy.conf.json`, nên phải chạy BackEnd trước.

Cần Node.js 18 hoặc 20 LTS. Máy phát triển hiện **chưa cài** — tải tại <https://nodejs.org>.
