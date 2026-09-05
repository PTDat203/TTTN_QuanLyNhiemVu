# FrontEnd — Angular 15 + Nebular

Giao diện phân hệ Giao nhiệm vụ. Đặc tả: [../DAC-TA-PHAN-HE-GIAO-NHIEM-VU.md](../DAC-TA-PHAN-HE-GIAO-NHIEM-VU.md)

## Chạy

```bash
npm install
npm start
```

<http://localhost:4200> — gọi API qua `proxy.conf.json` sang `http://localhost:5000`,
nên phải chạy BackEnd trước.

```bash
npm run build     # bản phát hành vào dist/
npm test          # Karma + Jasmine
```

## Về việc không dùng Kendo UI

Đặc tả §7.2 chọn Kendo UI for Angular để bám đúng hệ gốc. Nhưng §7.2.1 ghi nhận
`kendo-ui-license.txt` trong kho mã gốc **đã hết hạn 26/12/2024**, và Kendo là sản phẩm thương mại.

Dự án này đi theo **phương án 3** của §7.2.1: giữ Angular 15.2.10 + Nebular 11 + Bootstrap 4.3.1
+ FontAwesome 6 — đều miễn phí và đều nằm trong stack hệ gốc — còn lưới dữ liệu thì tự viết.
Đổi lại: không vướng license, `npm install` là chạy được; lệch hệ gốc ở phần lưới.

Component `shared/data-table` là thứ thay chỗ của Kendo Grid. Nếu sau này xin được license của
B&T, chỉ cần thay ruột component này, các màn không phải sửa.

## Cấu trúc

```
src/app/
  core/
    models/        Interface khớp JSON backend trả về
    services/      Mỗi service ứng một nhóm endpoint §5
    auth/          Interceptor gắn token, guard đăng nhập, guard vai trò
    trang-thai/    Bảng mã → nhãn → màu (§2), và bảng phân quyền §6.2 bản frontend
  shared/
    data-table         Lưới tự viết: phân trang, sắp xếp, template ô
    trang-thai-chip    Chip kép hai trục
    han-badge          Badge hạn: đỏ "Hết hạn", vàng "Sắp hết hạn"
    tien-do-bar        Thanh phần trăm hoàn thành
    nguoi-dung-picker  Chọn người từ cây đơn vị
  layout/          nb-layout + nb-sidebar, menu ẩn hiện theo vai trò
  auth/            M01 Đăng nhập
  van-ban/         M03 danh sách, M04 tạo/sửa, M05 phân công, M06 popup AI, M07 kiểm tra
  nhiem-vu/        M02 tổng quan, M08 của tôi, M09 xử lý, M10 gia hạn
  quan-tri/        M11 người dùng, M12 danh mục, M13 nhật ký AI, H4 trọng số
```

## Hai component đáng chú ý

**`trang-thai-chip`** hiện đồng thời `trangthai` và `trangthaiDvXuly` kèm mã số. Hai trục trạng
thái song song là nguồn nhầm lẫn lớn nhất của hệ gốc — đặc tả §2 dành hẳn một mục cảnh báo về
việc đánh số trùng nhưng khác nghĩa. Nên ở đây chúng được cho hiện ra rõ thay vì giấu đi.

**Popup AI gợi ý (M06)** là điểm nhấn của đề tài. Mỗi ứng viên có điểm tổng, thanh đóng góp có
trọng số, năm thanh thành phần và tối đa bốn dòng lý do. Nút *Xem cách tính điểm* bung ra công
thức S1–S5 đã thay số thật — đó là câu trả lời cho câu hỏi "vì sao lại là người này".
Luôn có nút *Bỏ qua, chọn thủ công*: hệ thống hỗ trợ quyết định, không tự động phân công.

## Về phân quyền phía frontend

`core/trang-thai/quyen.util.ts` cài lại bảng §6.2, tính trực tiếp từ `trangthai`,
`trangthaiDvXuly`, `trangthaixulygiahan` và danh sách phân công.

Cố ý **không dùng** các cờ do backend tính sẵn kiểu `isxuly`, `istuchoi`. Đặc tả §6.4 nêu lý do:
ở hệ gốc những cờ này điều khiển toàn bộ nút bấm nhưng quy tắc tính nằm hoàn toàn ở backend,
không kiểm chứng được từ frontend. Riêng `istuchoi` thực ra còn không phải trường của model —
frontend gốc tự gán nó bằng `isxuly`.

Đây chỉ là lớp ẩn/hiện nút cho dễ dùng. Quyết định thật vẫn ở backend, và backend không tin
bất cứ thứ gì frontend gửi lên.
