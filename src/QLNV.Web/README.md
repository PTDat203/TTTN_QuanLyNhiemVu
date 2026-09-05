# QLNV.Web — Frontend Angular 15

Phân hệ **Giao nhiệm vụ tích hợp AI gợi ý người thực hiện**.
Tệp này là **hợp đồng** cho mọi tác tử viết màn hình sau: đừng đổi tên lớp,
selector, đường dẫn tệp hoặc tên trường JSON đã khai ở đây.

---

## 1. Chạy dự án

```bash
cd src/QLNV.Web
npm install
npm start          # ng serve --proxy-config proxy.conf.json  ->  http://localhost:4200
```

Backend `QLNV.Api` chạy ở **http://localhost:5080** (xem `Properties/launchSettings.json`).
`proxy.conf.json` chuyển tiếp `/api/*` sang cổng đó. Backend cũng đã bật CORS cho
`http://localhost:4200`, nên có thể bỏ proxy và đặt `environment.apiUrl = 'http://localhost:5080'`.

| Lệnh | Việc |
|---|---|
| `npm start` | Dev server + proxy |
| `npm run build` | Build production |
| `npm test` | Karma + Jasmine |
| `npm run lint` | ESLint (`@angular-eslint`) |

---

## 2. Quyết định công nghệ đã chốt

- Angular **15.2.10**, TypeScript **4.9.5**, `"strict": true` + `strictTemplates` (§7.2).
- Nebular **11.0.1** + `@nebular/eva-icons`, Bootstrap **4.3.1**, FontAwesome **6**.
- **KHÔNG có bất kỳ gói `@progress/*` nào** — §7.2.1 phương án 3: license Kendo hết hạn
  26/12/2024, lưới dữ liệu **tự viết** ở `shared/data-table`.
- CSS của Nebular/Bootstrap/FontAwesome nạp qua mảng `styles` của `angular.json`
  (không `@import` trong SCSS để khỏi phụ thuộc cách Sass phân giải `node_modules`).
- `environment.ts` **chỉ chứa `apiUrl`** — §10.11 cấm để secret ở FE.

---

## 3. Ba quy tắc bắt buộc khi viết màn hình

### 3.1. Tên trường JSON giữ nguyên (§7.4 mục 1)
Interface trong `core/models/` khớp **chính xác** JSON của backend. Vài khoá lệch chuẩn
camelCase, **không được "sửa cho đẹp"**:

| Khoá JSON | Ghi chú |
|---|---|
| `trangthai`, `trangthaiDvXuly`, `trangthaixulygiahan` | ba trục trạng thái |
| `solangiahan`, `mucdoht`, `hanxulyth`, `hanxulyph`, `noidung`, `phanhoi`, `dokhan`, `linhvuc` | giữ chữ thường của hệ gốc |
| `hsChatluong` | chữ `l` thường |
| `userIdGiaoViec` | `I` hoa, khác hẳn `useridcreate` |
| `aiGoiyId`, `goiyId` | chữ `y` thường |
| `boQuaTrungNoidung` | chữ `d` thường |
| `hanxulyth_cu` | có gạch dưới |
| `GiaHanDto.trangThai` | chữ `T` HOA, khác `NhiemVuDto.trangthai` |
| `GiaHanRequest.noiDung` | chữ `D` HOA, khác `noidung` của các DTO khác |
| `soLieu.K`, `hieuSuat.K` | chữ `K` HOA |

**Tham số query thì ngược lại**: ASP.NET Core ràng buộc theo **tên thuộc tính C#**, không
theo `[JsonPropertyName]`. Dùng `vaiTro`, `trangThai`, `trangThaiDvXuly`, `quaHan`,
`sapHetHan`, `idVb`, `linhVuc`, `doKhan`, `search`, `page`, `size` (không phân biệt hoa
thường). Hàm `thamSo()` trong `core/services/api.util.ts` đã lo việc lặp khoá cho mảng.

### 3.2. Ẩn/hiện nút bằng `quyen.util.ts`, KHÔNG bằng cờ của BE (§6.4)
```ts
import { tinhQuyen } from '../../core/trang-thai/quyen.util';

const q = tinhQuyen(nv, this.auth.nguoiDungHienTai, chiTiet.phanCong);
// q.tiepNhan, q.guiBaoCao, q.kiemTraKetQua, ...
```
`nv.quyen` do backend trả về **chỉ để đối chiếu khi gỡ lỗi**. Đặc tả §6.4 ghi rõ FE phải
tự tính từ `trangthai` / `trangthaiDvXuly` / `trangthaixulygiahan` + danh sách phân công,
vì hệ gốc để BE tính cờ `isxuly`/`istuchoi` mà quy tắc tính **không tồn tại trong FE**.
Backend vẫn kiểm quyền độc lập và trả `403` — hai lớp bổ sung cho nhau.

### 3.3. Hai trục trạng thái luôn hiển thị song song (§2)
Dùng `<qlnv-trang-thai-chip>`; không được gộp hai trục thành một nhãn.

---

## 4. Bản đồ thư mục

```
src/app/
  core/                      hợp đồng — không màn hình nào được sửa
    models/                  interface cho mọi DTO §5
    services/                mỗi nhóm endpoint một service
    auth/                    interceptor + 2 guard
    trang-thai/              trang-thai.const.ts, quyen.util.ts
    ngay.util.ts
  shared/                    component dùng chung (thay Kendo)
  layout/                    khung nb-layout + nb-sidebar + nb-menu
  features/
    auth/          (lazy)    M01
    dashboard/     (eager)   M02
    van-ban/       (lazy)    M03 M04 M05 M06
    nhiem-vu/      (lazy)    M07 M08 M09 M10
    quan-tri/      (lazy)    M11 M12 M13   ← vaiTroGuard
```

---

## 5. Bảng đường dẫn ↔ màn hình

| Màn | Đường dẫn / cách mở | Component |
|---|---|---|
| M01 Đăng nhập | `/login` | `DangNhapComponent` |
| M02 Tổng quan | `/` | `TongQuanComponent` |
| M03 Văn bản chỉ đạo | `/van-ban` | `VanBanDanhSachComponent` |
| M04 Tạo/sửa văn bản | dialog từ M03 | `VanBanFormComponent` |
| M05 Phân công nhiệm vụ | `/van-ban/:id/phan-cong` | `PhanCongComponent` |
| M06 Popup AI gợi ý | dialog từ M05 | `AiGoiYComponent` |
| M07 Kiểm tra kết quả | dialog từ M08 | `KiemTraKetQuaComponent` |
| M08 Nhiệm vụ của tôi | `/nhiem-vu` | `NhiemVuCuaToiComponent` |
| — Chi tiết nhiệm vụ | `/nhiem-vu/:id` | `NhiemVuChiTietComponent` |
| M09 Xử lý nhiệm vụ | dialog từ M08 | `XuLyNhiemVuComponent` |
| M10 Gia hạn nhiệm vụ | dialog từ M08 | `GiaHanComponent` |
| M11 Người dùng | `/quan-tri/nguoi-dung` | `NguoiDungComponent` |
| M12 Danh mục | `/quan-tri/danh-muc` | `DanhMucComponent` |
| M13 Nhật ký gợi ý AI | `/quan-tri/ai-log` | `AiLogComponent` |

Mọi component trên **đã tồn tại dưới dạng khung rỗng**. Tác tử phụ trách chỉ thay nội
dung lớp + template; **không sửa router**.

---

## 6. Ví dụ dùng lưới dữ liệu

```html
<qlnv-data-table
  [cot]="cot"
  [duLieu]="ds"
  [phanTrangMayChu]="true"
  [tongSo]="tongSo"
  [trang]="trang"
  [kichThuoc]="kichThuoc"
  [dangTai]="dangTai"
  (trangThayDoi)="doiTrang($event)"
>
  <ng-template qlnvCot="trangthai" let-dong>
    <qlnv-trang-thai-chip
      [trangthai]="dong.trangthai"
      [trangthaiDvXuly]="dong.trangthaiDvXuly"
      [nhanTrucA]="dong.tenTrangThai"
      [nhanTrucB]="dong.tenTrangThaiDvXuly"
    ></qlnv-trang-thai-chip>
  </ng-template>

  <ng-template qlnvCot="hanxulyth" let-dong>
    <qlnv-han-badge
      [han]="dong.hanxulyth"
      [soNgay]="dong.soNgayConLai"
      [quaHan]="dong.quaHan"
      [sapHetHan]="dong.sapHetHan"
    ></qlnv-han-badge>
  </ng-template>
</qlnv-data-table>
```

```ts
cot: CotBang[] = [
  { khoa: 'noidung', nhan: 'Nội dung', catDong: true },
  { khoa: 'nguoiGiaoTen', nhan: 'Người giao', rong: '160px' },
  { khoa: 'hanxulyth', nhan: 'Hạn xử lý', kieu: 'template', rong: '150px' },
  { khoa: 'mucdoht', nhan: 'Tiến độ', kieu: 'template', rong: '120px' },
  { khoa: 'trangthai', nhan: 'Trạng thái', kieu: 'template', rong: '240px' }
];
```

---

## 7. Luồng tệp đính kèm

Upload **trước** (G1) để lấy `id`, rồi gửi mảng `fileIds` kèm body của C1 / C5 / D2 / D3 /
D4 / F1. `FileService.taiXuong()` trả `Blob` vì endpoint cần header `Authorization` — không
dùng thẻ `<a href>` trực tiếp được.

---

## 8. Luồng AI gợi ý (M05 → M06)

1. M05 gọi `AiService.goiYNguoiThucHien({ noidung, linhvuc, dokhan, hanxulyth, ... })`.
2. M06 hiển thị `ungVien[]`, lưu `goiyId`.
3. Người giao chọn ai đó **hoặc bỏ qua** → **luôn** gọi
   `AiService.ghiKetQuaGoiY(goiyId, { useridDaChon })` (`null` khi bỏ qua).
   Không gọi thì §9.8 không tính được tỷ lệ chấp nhận.
4. Khi lưu C1, gán `aiGoiyId = goiyId` vào dòng nhiệm vụ tương ứng.
