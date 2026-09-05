# Đặc tả phân hệ Giao nhiệm vụ tích hợp AI gợi ý người thực hiện

> Tài liệu bàn giao cho đội phát triển. Dựng lại nghiệp vụ giao nhiệm vụ của hệ thống
> **QLNV (Quản lý nhiệm vụ)** thành một ứng dụng web nhỏ, có thêm chức năng **AI gợi ý
> người thực hiện phù hợp**.

---

## 0. Bối cảnh và cách đọc tài liệu

### 0.1. Đề tài

| | |
|---|---|
| **Tên đề tài** | Xây dựng phân hệ Giao nhiệm vụ tích hợp AI hỗ trợ gợi ý người thực hiện phù hợp cho các tổ chức |
| **Sinh viên** | Phạm Tiến Đạt — MSV 0212966 — Lớp 66KSCS, Khoa học máy tính |
| **Trường** | Đại học Xây dựng Hà Nội — Khoa Công nghệ thông tin |
| **GVHD** | TS. Hoàng Nam Thắng |
| **Đơn vị thực tập** | Công ty TNHH Giải pháp Công nghệ B&T Việt Nam |
| **Thời gian** | 12 tuần (01/8/2026 – 24/10/2026) |

Yêu cầu gốc của đề tài: AI gợi ý người thực hiện dựa trên **chuyên môn**, **lịch sử thực
hiện**, **hiệu quả công việc** và **khối lượng nhiệm vụ hiện tại**.

### 0.2. Luồng nghiệp vụ đích

```
Bắt đầu → Tạo nhiệm vụ → Giao nhiệm vụ → Tiếp nhận
        → Thực hiện và cập nhật tiến độ → Gửi báo cáo kết quả → Kiểm tra kết quả
            ├─ Đạt      → Hoàn thành
            └─ Chưa đạt → Yêu cầu bổ sung → quay lại Thực hiện
```

### 0.3. ⚠️ Cách đọc — ba loại nội dung, đừng nhầm lẫn

Tài liệu này được dựng bằng cách **đọc trực tiếp mã nguồn** hệ thống QLNV đang chạy
(Angular 15, ~1.500 tệp). Mỗi khẳng định đều kèm `tệp:dòng`. Nhưng **không phải phần nào
cũng lấy được từ mã nguồn**, nên nội dung chia làm ba loại:

| Ký hiệu | Nghĩa | Cách dùng |
|---|---|---|
| *(không đánh dấu)* | **Có thật trong mã nguồn**, đã trích dẫn `tệp:dòng` | Cài theo đúng như mô tả |
| **THIẾT KẾ MỚI** | Sơ đồ nghiệp vụ đòi hỏi nhưng hệ gốc **không có** | Đội mới tự cài, ghi rõ là phần bổ sung |
| **BỎ / LƯỢC** | Hệ gốc có nhưng cố ý không đưa vào app nhỏ | Kèm lý do ngay tại chỗ |

**Đọc mục 10 trước khi lập kế hoạch.** Mục đó liệt kê những thứ **không thể sao chép**
từ hệ gốc vì chúng không tồn tại — trong đó có hai thứ chặn thẳng vào chức năng AI:
dữ liệu **chuyên môn người dùng** và **đo hiệu quả công việc**.

### 0.4. Ba điểm khác biệt lớn nhất so với hệ gốc

1. **Bước "Tiếp nhận" không tồn tại trong hệ gốc.** Không nút, không API, không trạng
   thái. App mới phải tự thiết kế.
2. **Không có bảng mã→nhãn trạng thái trong mã nguồn.** Nhãn nạp lúc chạy từ danh mục
   phía máy chủ. Bảng trạng thái ở mục 2 được suy ra từ chú thích và các nhánh so sánh
   trong mã — app mới **phải tự seed** bảng danh mục này.
3. **Hệ gốc không có dữ liệu chuyên môn người dùng.** Đặc trưng có trọng số cao nhất của
   mô hình gợi ý **không có nguồn dữ liệu** — phải xây từ đầu.

---

## Mục lục

1. [Luồng nghiệp vụ](#1-luồng-nghiệp-vụ-đích-của-app-nhỏ)
2. [Máy trạng thái](#2-máy-trạng-thái)
3. [Danh sách màn hình](#3-danh-sách-màn-hình)
4. [Mô hình dữ liệu](#4-mô-hình-dữ-liệu)
5. [Danh sách API](#5-danh-sách-api)
6. [Phân quyền](#6-phân-quyền)
7. [Công nghệ](#7-công-nghệ)
8. [Phân rã công việc — 12 tuần](#8-phân-rã-công-việc--12-tuần)
9. [AI gợi ý người thực hiện phù hợp](#9-ai-gợi-ý-người-thực-hiện-phù-hợp-)
10. [CẢNH BÁO — những gì hệ thống gốc không có](#10-cảnh-báo--những-gì-hệ-thống-gốc-không-có)

---


## 1. Luồng nghiệp vụ đích của app nhỏ

```
Bắt đầu
  └─> (1) Tạo nhiệm vụ
        └─> (2) Giao nhiệm vụ (chọn người/đơn vị thực hiện — CÓ AI GỢI Ý)
              └─> (3) Tiếp nhận
                    └─> (4) Thực hiện & cập nhật tiến độ  <──────────┐
                          └─> (5) Gửi báo cáo kết quả               │
                                └─> (6) Kiểm tra kết quả           │
                                      ├─ Đạt      -> (7) Hoàn thành │
                                      └─ Chưa đạt -> Yêu cầu bổ sung┘
```

### 1.1. Đối chiếu với hệ thống gốc (QLNV_FE)

| Bước sơ đồ | Hiện trạng hệ gốc | Quyết định cho app nhỏ |
|---|---|---|
| Tạo nhiệm vụ | 2 tầng: **Văn bản chỉ đạo** (`DmNhiemvuModel`, API `POST /qlgiaonv/api/DmNhiemvu/InsertOrUpdateDmNhiemvu`) → **Nhiệm vụ chi tiết** (`InsertMultipleNhiemvuChitietV2Model`). Có 3 lối vào: `VANBAN` / `BUTPHE` / `CHIDAO` — nhưng `typeNV` **không đi vào payload**, BUTPHE và CHIDAO sinh dữ liệu giống hệt nhau | **GIỮ** 2 tầng Văn bản → Nhiệm vụ (1–n). **LƯỢC** 3 lối vào còn 1 lối "Tạo nhiệm vụ từ văn bản chỉ đạo" vì 2/3 lối trùng nhau về dữ liệu |
| Giao nhiệm vụ | **Không có API "Giao" riêng.** Giao = lưu nhiệm vụ kèm `listUserTh[]` (chủ trì) / `listUserPh[]` (phối hợp). Mỗi dòng nhiệm vụ **bắt buộc ≥ 1 chủ trì**, phối hợp có thể rỗng | **GIỮ** mô hình chủ trì/phối hợp. **THÊM MỚI**: nút "Gợi ý người thực hiện" ngay tại ô chọn chủ trì |
| Tiếp nhận | **KHÔNG TỒN TẠI** trong mã gốc: không nút, không API, không trạng thái. Thứ gần nhất là cờ `isview` tự bật khi mở dialog chi tiết (`XemNhiemvu()` → `POST .../UpdateChitietNV`). Việc chuyển "Chưa triển khai (3)" → "Đang triển khai (2)" ở hệ gốc chỉ xảy ra khi người thực hiện **tự chọn** trạng thái trong combobox "Kết quả xử lý" | **THIẾT KẾ MỚI**: bổ sung hành động **Tiếp nhận** tường minh: `trangthai 3 → 2`, ghi `ngaytiepnhan`. Đây là điểm khác biệt so với hệ gốc, phải ghi rõ trong tài liệu bàn giao |
| Thực hiện & cập nhật tiến độ | Không có màn riêng. Dùng chung dialog "Xử lý nhiệm vụ" (`ThongTinXuLyNhiemVuComponent`), nhập `mucdoht` (%) — ô `type="text"`, `maxlength=3`, **không validate 0–100** | **GIỮ**, nhưng **tách** hành động "Cập nhật tiến độ" (không đổi trạng thái phản hồi) khỏi "Gửi báo cáo kết quả". Thêm validate 0–100 |
| Gửi báo cáo kết quả | `POST /qlgiaonv/api/DmNhiemvuV2/Xulynhiemvu`. FE tự ép `trangthaiXuly = 10` và `trangthaiDvXuly = 10` (trừ việc phối hợp `VIECPH`); `trangthai` = giá trị người dùng chọn trong combobox đã bị lọc theo hạn | **GIỮ** nguyên cơ chế: gửi báo cáo ⇒ `trangthaiDvXuly = 10` (Chờ xác nhận) |
| Kiểm tra kết quả | Dialog "Phản hồi nhiệm vụ" (`PhanHoiNhiemvuComponent`) → `POST .../UpdateChitietNV`. **Không có 3 nút Đạt/Chưa đạt/Bổ sung**, chỉ có 1 dropdown "Trạng thái phản hồi" + 1 nút nhãn động Lưu/Duyệt | **ĐƠN GIẢN HOÁ**: thay dropdown bằng **2 nút tường minh "Đạt" / "Chưa đạt (yêu cầu bổ sung)"** — đúng sơ đồ đích, dễ hiểu hơn |
| Chưa đạt → quay lại | Chọn phản hồi = **12**; nếu `trangthai ∈ {1,5}` thì hệ thống **đẩy ngược** `trangthai := 2` (còn hạn) hoặc `:= 7` (quá hạn) → nút "Xử lý nhiệm vụ" mở lại | **GIỮ NGUYÊN** — đây chính là mũi tên "Yêu cầu bổ sung → Thực hiện" của sơ đồ |
| Hoàn thành | Điểm cuối = `trangthai ∈ {1,5}` **VÀ** `trangthaiDvXuly = 11`. Ở cặp này toàn bộ 9 guard hành động đều false, chỉ còn "Chi tiết" | **GIỮ NGUYÊN** |

### 1.2. Mô tả chi tiết từng bước (đặc tả thi hành)

**Bước 1 — Tạo nhiệm vụ** (vai: Người giao)
1. Tạo/chọn **Văn bản chỉ đạo**: số ký hiệu, trích yếu, ngày ban hành, cơ quan ban hành, độ khẩn, độ mật, tệp đính kèm.
2. Trong màn Phân công, nhập **danh sách nhiệm vụ** (lưới nhiều dòng, 1 văn bản → n nhiệm vụ). Mỗi dòng: nội dung (bắt buộc, ≤2000 ký tự), lĩnh vực, mức độ ưu tiên, thời hạn hoàn thành.
3. Ràng buộc bật nút Lưu (bám gốc, rút gọn còn 4 điều kiện): có đơn vị giao, có lĩnh vực, mọi dòng có nội dung, mọi dòng có ≥1 người/đơn vị chủ trì.
4. (Tuỳ chọn, giữ từ gốc) **Dò trùng nội dung** trước khi lưu — fail-open: lỗi hoặc không trùng thì lưu tiếp; trùng thì cảnh báo, người dùng xác nhận bỏ qua mới lưu.

**Bước 2 — Giao nhiệm vụ** (vai: Người giao)
1. Ở cột "Người/đơn vị chủ trì", bấm **"AI gợi ý người thực hiện"**.
2. Hệ thống trả về top-5 ứng viên kèm **điểm** và **lý do** (xem mục `aiGoiY`).
3. Người giao chọn 1 hoặc nhiều người; có thể bỏ qua gợi ý và chọn tay.
4. Lưu ⇒ sinh bản ghi phân công. **Trạng thái khởi tạo do server đặt = 3 (Chưa triển khai)**, `trangthaiDvXuly = null`.
   > Lưu ý bàn giao: hệ gốc **không** gán trạng thái khởi tạo ở FE (đã grep 7 màn tạo/giao, không dòng nào gán `trangthai`); backend quyết định. App mới **phải quy định rõ = 3**.
5. Ghi log gợi ý (đề xuất top-5, ai được chọn) vào `AI_GOIY_LOG` để đo tỷ lệ chấp nhận.

**Bước 3 — Tiếp nhận** (vai: Người thực hiện)
- Nhiệm vụ ở `trangthai = 3`, `trangthaiDvXuly = null`. Người thực hiện bấm **Tiếp nhận** ⇒ `trangthai := 2 (Đang triển khai)`, ghi `ngaytiepnhan`.
- Ngoài Tiếp nhận, người thực hiện còn có thể **Từ chối nhiệm vụ** ⇒ `trangthai := 6`, `trangthaiDvXuly := 10` (chờ người giao xử lý). Bám gốc: `CATE_FORM.REFUSE = 6` ép `trangthai = 6`, `trangthaiXuly = 10`.
  > Ở gốc **lý do từ chối KHÔNG bắt buộc** (ô "Nội dung xử lý" chỉ `required` khi trạng thái là '1' hoặc '5'). App mới **bắt buộc nhập lý do** — đây là yêu cầu mới, không phải hiện trạng.

**Bước 4 — Thực hiện & cập nhật tiến độ** (vai: Người thực hiện)
- Nhập `mucdoht` (0–100), nội dung công việc đã làm, đính kèm tệp kết quả.
- Hành động này **không** đổi `trangthaiDvXuly` (vẫn `null`), chỉ ghi thêm 1 dòng vào lịch sử xử lý.
- Có thể thực hiện nhiều lần.

**Bước 5 — Gửi báo cáo kết quả** (vai: Người thực hiện)
- Chọn kết quả xử lý. **Danh sách kết quả bị lọc theo hạn** (bám gốc):
  - Còn hạn (số ngày còn lại ≥ 0): loại bỏ `5, 7, 8, 6` ⇒ chọn được **1 (Hoàn thành)**, 2, 3.
  - Quá hạn (< 0): loại bỏ `1, 2, 4, 6, 8` ⇒ chọn được **5 (Hoàn thành - Sau hạn)**, 7, 3.
  - Giá trị `13 (Gia hạn)` **luôn bị loại** khỏi combobox này.
- Nội dung xử lý **bắt buộc** khi chọn trạng thái `1` hoặc `5`.
- Lưu ⇒ `trangthai := giá trị đã chọn`, `trangthaiDvXuly := 10 (Chờ xác nhận)`.
- Người thực hiện có thể **Thu hồi báo cáo** khi còn đang chờ xác nhận ⇒ `trangthai := 2` (còn hạn / không có hạn) hoặc `:= 7` (quá hạn), `trangthaiDvXuly := null`.

**Bước 6 — Kiểm tra kết quả** (vai: Người giao)
- Điều kiện thấy nút "Kiểm tra": `trangthaiDvXuly = 10` và người đăng nhập là người giao (hoặc người tạo khi chưa chỉ định cán bộ) — bám `coTheXacNhan()`.
- **Đạt** ⇒ `trangthaiDvXuly := 11`, `trangthai` giữ nguyên (1 hoặc 5) ⇒ **kết thúc**.
- **Chưa đạt (Yêu cầu bổ sung)** ⇒ `trangthaiDvXuly := 12` **và** nếu `trangthai ∈ {1,5}` thì `trangthai := 2` (còn hạn) / `:= 7` (quá hạn) ⇒ **quay lại bước 4**. Nội dung phản hồi bắt buộc ở app mới (gốc không bắt buộc).

**Bước 7 — Hoàn thành**
- Cặp `(trangthai ∈ {1,5}, trangthaiDvXuly = 11)`. Mọi hành động tắt, chỉ xem chi tiết và lịch sử.

### 1.3. Nhánh phụ GIỮ LẠI

| Nhánh | Mô tả (bám gốc) |
|---|---|
| **Gia hạn** | Người thực hiện xin gia hạn khi `trangthai ∈ {2,3,7}`, chưa có yêu cầu treo (`trangthaixulygiahan ≠ 10`), `solangiahan < 2`. Request đặt `trangThai = 13`, bản ghi gia hạn `trangthaixulygiahan = 10`. Người giao (`userIdGiaoViec`) Duyệt ⇒ `11` (+1 lần), Từ chối ⇒ `12`. |
| **Thu hồi nhiệm vụ** | Người giao rút cả nhiệm vụ ⇒ `trangthai := 97`, `trangthaiDvXuly := null`. Điểm cuối, không có đường quay lại trong FE gốc. |
| **Nhắc việc** | Người giao gửi nhắc tới người thực hiện. Ở gốc: **không đổi trạng thái**, không đính kèm file, nội dung ≤2000 ký tự. Giữ ở dạng tối giản. |

### 1.4. Nhánh LƯỢC BỎ có chủ đích

| Bỏ | Lý do |
|---|---|
| **Trình cấp trên** (`trangthaiDvXuly = 14`, `POST /DeXuatNhiemVu/trinh-xu-ly-nv`) | Là nhánh duyệt nhiều cấp, không nằm trong sơ đồ đích; ở gốc nút riêng đã bị comment, chỉ còn kích hoạt gián tiếp bằng cách chọn giá trị '14' trong dropdown — khó hiểu, dễ sai |
| **Luồng văn bản đề xuất** (Duyệt / Trình / Trả lại, trục trạng thái riêng 1/2/3/4) | Là trục trạng thái THỨ BA, đánh số trùng nhưng khác nghĩa hoàn toàn với trạng thái nhiệm vụ ⇒ nguồn gây nhầm lẫn lớn nhất của hệ gốc |
| **Nhiệm vụ phối hợp (`VIECPH`) có luồng riêng** | Ở gốc, việc phối hợp khi lưu **không** set `trangthaiDvXuly = 10` (tự cập nhật, không qua kiểm tra). App nhỏ: giữ vai phối hợp ở mức **chỉ đọc + bình luận**, không có luồng trạng thái riêng |
| **Nhiệm vụ định kỳ** (`DKNGAY/DKTUAN/DKTHANG/DKNAM`) | Chỉ giữ `KHONGDK`. Định kỳ kéo theo `gioxuly`, `thuxuly`, `ngayketthucdk`, `isketthucdk` và job nền sinh kỳ — quá nặng cho app mô phỏng |
| **Mã hoá nội dung + mật khẩu file, ký số** (`encryptData`, service localhost:6543) | Đặc thù an ninh của hệ gốc, không phục vụ mục tiêu mô phỏng nghiệp vụ + AI |
| **Trục trạng thái thống kê** (`TRANGTHAITHONGKE`, job nền `SP_THONGKE_NHIEMVU`) | Đánh số lại hoàn toàn khác trục nghiệp vụ (1=Hoàn thành, 2=Đang triển khai, 3=Quá hạn, 4=Trả lại). App mới tính thống kê trực tiếp từ trạng thái nghiệp vụ + hạn, không dựng job nền |
| **Bản `-embed`** của mọi màn | Hệ gốc có 2 bản song sinh gần như y hệt cho mỗi màn ⇒ 2 nguồn sự thật. App mới chỉ 1 bản |


---


## 2. Máy trạng thái

App nhỏ dùng **2 trục trạng thái song song** — đúng như hệ gốc, không gộp:

- Trục A — `trangthai`: trạng thái nhiệm vụ (gốc: danh mục `DM_TUDIEN` type `TRANGTHAINV`).
- Trục B — `trangthaiDvXuly`: trạng thái phản hồi/kiểm tra kết quả (gốc: type `TRANGTHAIPH`).
- Trục phụ C — `trangthaixulygiahan`: chỉ dùng cho nhánh gia hạn.

> **Quan trọng khi bàn giao**: hệ gốc **không có bảng mã→nhãn** cho `TRANGTHAINV`/`TRANGTHAIPH` trong mã nguồn FE dưới bất kỳ dạng nào (enum, const, JSON, SQL seed). Nhãn nạp runtime từ `POST /Qtht/api/DmTudien/GetDmTudienByMa` với body `["TRANGTHAINV","TRANGTHAIPH"]`. Bảng dưới đây được **suy ra từ comment mã nguồn + các nhánh gán/so sánh** và đã đối chiếu chéo. App mới **phải seed bảng danh mục này**.

### 2.1. Trục A — `trangthai` (trạng thái nhiệm vụ)

| Mã | Nhãn | Ý nghĩa | Dùng trong app nhỏ | Bằng chứng gốc |
|---|---|---|---|---|
| 1 | Hoàn thành | Đã báo cáo xong, còn trong hạn | ✅ | `TRANGTHAI_DA_HOAN_THANH = [1, 5]` (xu-ly-cv-phan-cong.component.ts:1601-1602) |
| 2 | Đang triển khai | Đã tiếp nhận, đang làm, còn hạn | ✅ | comment `// 2: đang triển khai` (:2019) |
| 3 | Chưa triển khai | Đã giao nhưng chưa tiếp nhận | ✅ **(trạng thái khởi tạo)** | comment `// 3:Chưa triển khai` (:2020) |
| 4 | *(không tìm thấy nhãn)* | Chỉ xuất hiện trong mảng lọc bỏ khi quá hạn | ❌ **BỎ** | thong-tin-xu-ly-nhiem-vu.component.ts:1273 |
| 5 | Hoàn thành - Sau hạn | Đã báo cáo xong nhưng trễ hạn | ✅ | ts:1601 ; ticker:33 |
| 6 | Từ chối nhiệm vụ | Người nhận từ chối (giao nhầm / không đúng chức năng) | ✅ | thong-tin-xu-ly-nhiem-vu.component.ts:227-231 |
| 7 | Đang triển khai - Đã hết hạn | Đang làm nhưng đã quá hạn | ✅ | comment `// 7:Đang triển khai - Đã hết hạn` (:2021) |
| 8 | Chỉnh sửa bổ sung | Người thực hiện đang sửa báo cáo đã gửi | ⚠️ **BỎ** (xem ghi chú) | ts:214-217 (`CATE_FORM.ADDITIONAL` ép `trangthai = 8`) |
| 13 | Gia hạn | Đang trong quy trình gia hạn | ✅ | :2022 ; gia-han-nv.component.ts:54 |
| 97 | Đã thu hồi | Người giao thu hồi cả nhiệm vụ | ✅ **(điểm cuối)** | dm-index-nhiemvu-chitiet.component.ts:282 |
| 100 | *(không tìm thấy nhãn)* | Chỉ có icon, không nơi nào gán | ❌ **BỎ** | xu-ly-cv-phan-cong.component.ts:213 |

> **Ghi chú mã 8**: ở gốc, mã 8 bị loại khỏi combobox "Kết quả xử lý" ở **cả hai** nhánh lọc (còn hạn loại `['5','7','8','6']`, quá hạn loại `['1','2','4','6','8']`) nên người dùng không bao giờ chọn được; nó chỉ được **ép** khi mở form ở chế độ `ADDITIONAL`. App nhỏ bỏ hành động "Chỉnh sửa bổ sung" (thay bằng "Thu hồi báo cáo" rồi báo cáo lại) ⇒ bỏ luôn mã 8.

### 2.2. Trục B — `trangthaiDvXuly` (trạng thái kiểm tra kết quả)

| Mã | Nhãn | Ý nghĩa | Màu | Dùng trong app nhỏ |
|---|---|---|---|---|
| `null` | *(chưa gửi báo cáo)* | Chưa có báo cáo nào ⇒ được phép Xử lý | — | ✅ |
| 10 | Chờ xác nhận | Đã gửi báo cáo, chờ người giao kiểm tra | vàng | ✅ |
| 11 | Đã xác nhận | Kết quả **ĐẠT** | xanh lá | ✅ |
| 12 | Từ chối | Kết quả **CHƯA ĐẠT** ⇒ yêu cầu bổ sung | đỏ | ✅ |
| 14 | Đã trình cấp trên | Đẩy lên cấp trên duyệt | xanh lá | ❌ **BỎ** (lược bỏ nhánh trình cấp trên) |

### 2.3. Trục C — `trangthaixulygiahan` (duyệt gia hạn)

| Mã | Nhãn | `solangiahan` sau đó |
|---|---|---|
| `null` | Chưa xin gia hạn | giữ nguyên |
| 10 | Chờ duyệt gia hạn | giữ nguyên |
| 11 | Đã duyệt gia hạn | **+1** |
| 12 | Từ chối gia hạn | giữ nguyên |

Ràng buộc: `solangiahan < 2` (tối đa 2 lần) — `coTheGiaHan()` (xu-ly-cv-phan-cong.component.ts:1751-1757).

> ⚠️ Ràng buộc "chỉ gia hạn khi còn dưới 3 ngày hoặc đã quá hạn" **KHÔNG có hiệu lực** ở hệ gốc: nó nằm trong hàm chết `canShowGiaHan()` (không có call site, và luôn trả `false` do so sánh chuỗi `'"VIECDVGIAO"'` có nháy kép thừa). Guard đang chạy là `coTheGiaHan()`, **không kiểm tra ngày**. App mới tự quyết định có áp ràng buộc này hay không.

### 2.4. Ma trận chuyển trạng thái (đặc tả cho app nhỏ)

| # | Từ `(trangthai, trangthaiDvXuly)` | Hành động | Vai | Đến |
|---|---|---|---|---|
| T1 | — | **Tạo & Giao nhiệm vụ** | Người giao | `(3, null)` |
| T2 | `(3, null)` | **Tiếp nhận** *(mới, gốc không có)* | Người thực hiện | `(2, null)` + `ngaytiepnhan` |
| T3 | `(3, null)` hoặc `(2, null)` | **Từ chối nhiệm vụ** (bắt buộc lý do) | Người thực hiện | `(6, 10)` |
| T4 | `(6, 10)` | **Người giao xử lý từ chối: Chấp nhận** | Người giao | `(97, null)` (thu hồi) |
| T5 | `(6, 10)` | **Người giao xử lý từ chối: Bác bỏ** | Người giao | `(2, 12)` — người thực hiện làm lại |
| T6 | `(2 \| 7, null \| 12)` | **Cập nhật tiến độ** (chỉ ghi `mucdoht` + lịch sử) | Người thực hiện | *không đổi trạng thái* |
| T7 | `(2 \| 3 \| 7, null \| 12)` | **Gửi báo cáo kết quả** (chọn trong danh sách đã lọc theo hạn) | Người thực hiện | `(giá trị đã chọn, 10)` |
| T8 | `(1 \| 5, 10)` | **Thu hồi báo cáo** | Người thực hiện | `(2 nếu còn hạn / 7 nếu quá hạn, null)` |
| T9 | `(1 \| 5, 10)` | **Kiểm tra: ĐẠT** | Người giao | `(giữ nguyên, 11)` → **ĐIỂM CUỐI** |
| T10 | `(1 \| 5, 10)` | **Kiểm tra: CHƯA ĐẠT** (bắt buộc nội dung phản hồi) | Người giao | `(2 nếu còn hạn / 7 nếu quá hạn, 12)` → quay lại T6/T7 |
| T11 | `(2 \| 3 \| 7, *)`, `giahan ≠ 10`, `solangiahan < 2` | **Xin gia hạn** | Người thực hiện | `trangthai := 13`, `trangthaixulygiahan := 10` |
| T12 | `trangthaixulygiahan = 10` | **Duyệt gia hạn** | Người giao | `trangthaixulygiahan := 11`, `solangiahan += 1`, cập nhật `hanxulyth` |
| T13 | `trangthaixulygiahan = 10` | **Từ chối gia hạn** | Người giao | `trangthaixulygiahan := 12` |
| T14 | `trangthai ≠ 97` và chưa hoàn thành | **Thu hồi nhiệm vụ** | Người giao | `(97, null)` → **ĐIỂM CUỐI** |

### 2.5. Quy tắc tự động theo hạn

| Quy tắc | Áp dụng khi |
|---|---|
| `2 → 7` (quá hạn) | Job nền chạy hằng ngày, khi `hanxulyth < hôm nay` và `trangthai = 2` |
| `3 → 7` | Tương tự với `trangthai = 3` (đã giao nhưng chưa tiếp nhận, quá hạn) |
| Chọn `1` hay `5` khi báo cáo | Do **bộ lọc combobox theo hạn** quyết định, không phải người dùng tự do chọn |
| `7 → 2` | Khi duyệt gia hạn và hạn mới > hôm nay |

> Ở gốc, FE chỉ tính `2 ↔ 7` ở đúng 2 chỗ (thu hồi báo cáo và từ chối kết quả); phần còn lại do job nền backend (`SP_THONGKE_NHIEMVU`) — quy tắc chấm **không có trong FE**. App mới phải tự viết job này.

### 2.6. Điểm cuối

| Điểm cuối | Điều kiện |
|---|---|
| **Hoàn thành – đã nghiệm thu** | `trangthai ∈ {1,5}` **VÀ** `trangthaiDvXuly = 11` |
| **Đã thu hồi** | `trangthai = 97` |

**KHÔNG phải điểm cuối** (bám gốc, dễ nhầm):
- `(1|5, 10)` — vẫn thu hồi báo cáo được.
- `(1|5, 12)` — chính là vòng "Yêu cầu bổ sung", vẫn xử lý lại được.
- `(6, 12)` — nhiệm vụ bị từ chối nhưng người giao bác bỏ, vẫn xử lý được.


---


## 3. Danh sách màn hình

App nhỏ gồm **10 màn** (gốc có hơn 40 màn + bản `-embed` song sinh). Mỗi màn chỉ 1 bản, không có bản nhúng.

### 3.1. Nhóm dùng chung

| # | Màn | Route | Chức năng chính |
|---|---|---|---|
| M01 | **Đăng nhập** | `/login` | Form username/password, JWT. **Lược bỏ SSO WSO2/Keycloak** của hệ gốc |
| M02 | **Bảng điều khiển (Dashboard)** | `/` | 4 thẻ đếm: Chưa triển khai / Đang triển khai / Chờ xác nhận / Quá hạn. Biểu đồ tròn theo trạng thái, biểu đồ cột theo đơn vị. Danh sách "Việc của tôi sắp đến hạn (≤3 ngày)" |

### 3.2. Nhóm người giao

| # | Màn | Route | Chức năng chính |
|---|---|---|---|
| M03 | **Danh sách văn bản chỉ đạo** | `/van-ban` | Lưới phân trang, lọc theo số ký hiệu / trích yếu / ngày ban hành / lĩnh vực. Cột "Số nhiệm vụ" (`tongSoNhiemVu`). Nút Thêm mới / Sửa / Xoá (xoá chỉ khi chưa có nhiệm vụ nào) |
| M04 | **Tạo / sửa văn bản chỉ đạo** | dialog | 8 trường: số ký hiệu, ngày ban hành, loại văn bản, cơ quan ban hành, trích yếu **(bắt buộc)**, độ khẩn **(bắt buộc)**, lĩnh vực, tệp đính kèm. Bám gốc `dm-nhiemvu-crud` nhưng bỏ độ mật/mã hoá |
| M05 | **Phân công nhiệm vụ** ⭐ | `/van-ban/:id/phan-cong` | **Màn quan trọng nhất.** Header: đơn vị giao, lãnh đạo phụ trách, lĩnh vực, thời gian chỉ đạo. Lưới n dòng nhiệm vụ, mỗi dòng: STT, nội dung (bắt buộc, ≤2000), mức độ ưu tiên, **người chủ trì (bắt buộc ≥1) + nút "🤖 AI gợi ý"**, người phối hợp (tuỳ chọn), thời hạn hoàn thành. Nút Thêm dòng / Xoá dòng / Lưu. Dò trùng nội dung trước khi lưu |
| M06 | **Popup AI gợi ý người thực hiện** ⭐ | dialog | Danh sách top-5 ứng viên: ảnh/tên, đơn vị, **điểm tổng (0–100)** dạng thanh, **4 thanh thành phần** (chuyên môn / kinh nghiệm / hiệu quả / tải hiện tại), 1–2 câu lý do bằng tiếng Việt, nhãn "Dữ liệu còn ít" khi cold-start. Nút "Chọn" từng người, nút "Xem thêm 5 người", ô lọc theo đơn vị |
| M07 | **Kiểm tra kết quả** | dialog | Hiển thị: nội dung nhiệm vụ, hạn, báo cáo mới nhất (nội dung + `mucdoht` + tệp), lịch sử báo cáo. Ô "Nội dung phản hồi" **(bắt buộc)**. **2 nút: "✅ Đạt" và "↩️ Chưa đạt – yêu cầu bổ sung"** |

### 3.3. Nhóm người thực hiện

| # | Màn | Route | Chức năng chính |
|---|---|---|---|
| M08 | **Nhiệm vụ của tôi** | `/nhiem-vu-cua-toi` | Lưới nhiệm vụ được giao. Bộ lọc nhanh dạng chip: Tất cả / Chưa tiếp nhận / Đang làm / Chờ xác nhận / Bị trả lại / Quá hạn / Hoàn thành. Cột: nội dung, người giao, hạn, **số ngày còn lại** (badge đỏ "Hết hạn" / vàng "Sắp hết hạn <3 ngày"), tiến độ `mucdoht` (%), trạng thái, trạng thái phản hồi. Menu hành động theo trạng thái |
| M09 | **Xử lý nhiệm vụ** | dialog | 3 chế độ dùng chung 1 form (bám gốc `ThongTinXuLyNhiemVuComponent`):<br>• **Cập nhật tiến độ**: `mucdoht` + nội dung + tệp<br>• **Gửi báo cáo kết quả**: thêm combobox "Kết quả xử lý" (đã lọc theo hạn), nội dung bắt buộc khi chọn 1/5<br>• **Từ chối nhiệm vụ**: chỉ có ô lý do (bắt buộc)<br>Tab bên phải: lịch sử xử lý + lịch sử phản hồi |
| M10 | **Gia hạn nhiệm vụ** | dialog | Chế độ **Đề xuất** (người thực hiện): hạn hiện tại (chỉ đọc), thời gian đề xuất gia hạn **(bắt buộc, `min` = hạn hiện tại)**, lý do, tệp. Chế độ **Duyệt** (người giao): nội dung phản hồi + 2 nút Duyệt / Từ chối. Bên dưới: lịch sử gia hạn (tối đa 2 lần) |

### 3.4. Màn quản trị tối thiểu

| # | Màn | Route | Chức năng |
|---|---|---|---|
| M11 | **Người dùng & Hồ sơ năng lực** ⭐ | `/quan-tri/nguoi-dung` | **Màn MỚI hoàn toàn** — hệ gốc không có. Quản lý người dùng + **hồ sơ chuyên môn** (danh sách lĩnh vực + mức thành thạo 1–5) + số nhiệm vụ tối đa cùng lúc. Đây là **nguồn dữ liệu bắt buộc** cho AI gợi ý |
| M12 | **Danh mục** | `/quan-tri/danh-muc` | Lĩnh vực/nghiệp vụ, trạng thái nhiệm vụ (`TRANGTHAINV`), trạng thái phản hồi (`TRANGTHAIPH`), độ khẩn. Seed sẵn theo bảng ở mục `trangThai` |
| M13 | **Nhật ký gợi ý AI** ⭐ | `/quan-tri/ai-log` | Xem log: nhiệm vụ nào, đề xuất ai, điểm bao nhiêu, người giao chọn ai, **tỷ lệ chấp nhận top-1 / top-3**. Dùng để hiệu chỉnh trọng số và làm số liệu báo cáo thực tập |

### 3.5. Màn LƯỢC BỎ so với gốc (và lý do)

| Màn gốc | Lý do bỏ |
|---|---|
| Xử lý CV phân công (`xu-ly-cv-phancong`) với 3 tab `VIECGIAODV`/`VIECDVGIAO`/`VIECPH` | Gộp vào M08 bằng 1 bộ lọc "Vai trò: Tôi giao / Tôi làm". Gốc có **2 bộ menu hành động song song** (chuột phải + bánh răng) với điều kiện khác nhau — nguồn lỗi |
| Toàn bộ nhóm `giao-nhan-nhiem-vu` (văn bản đề xuất, duyệt/trình/trả lại) | Trục trạng thái thứ ba, ngoài sơ đồ đích |
| Dashboard trạng thái + ticker thông báo | Phụ thuộc job nền thống kê `SP_THONGKE_TRANGTHAI_BCAV2` |
| Import Excel hàng loạt, AI tách nhiệm vụ từ văn bản (SSE), OCR | Ngoài trọng tâm; AI của đề tài là **gợi ý người thực hiện**, không phải OCR |
| Nhắc việc hàng loạt, guided tour, kho dữ liệu, soạn thảo docx, báo cáo Telerik | Ngoài phạm vi app mô phỏng |
| Mọi bản `-embed` | 2 nguồn sự thật, chi phí bảo trì gấp đôi |


---


## 4. Mô hình dữ liệu

Đặt tên bảng theo quy ước hệ gốc (`DM_`, `_CHITIET`) để đội mới đối chiếu được. Khoá chính: `UUID`.

### 4.1. `DM_VANBAN` — Văn bản chỉ đạo (gốc: `DmNhiemvuModel`)

| Trường | Kiểu | Bắt buộc | Ghi chú |
|---|---|---|---|
| `id` | uuid | ✅ | PK |
| `sokyhieu` | varchar(100) | | Số ký hiệu |
| `trichyeu` | varchar(2000) | ✅ | Tóm tắt nội dung chỉ đạo |
| `loaivb` | varchar(50) | | FK → `DM_TUDIEN` (type `LOAIVB`) |
| `ngaybanhanh` | date | | |
| `coquanbanhanh` | varchar(500) | | |
| `dokhan` | varchar(50) | ✅ | `TRONGTAM` / `THUONGXUYEN` / `DOTXUAT` |
| `linhvuc` | varchar(50) | | FK → `DM_LINHVUC` |
| `thoigianchidao` | datetime | | Mặc định = ngày tạo |
| `nguonnv` | varchar(200) | | Cơ quan/đơn vị giao nhiệm vụ |
| `nguoitheodoi` | varchar(500) | | CSV `userid` lãnh đạo phụ trách |
| `unitcode` | varchar(50) | ✅ | Đơn vị sở hữu bản ghi |
| `useridcreate` | uuid | ✅ | |
| `createdate` | datetime | ✅ | |

**LƯỢC BỎ so với gốc**: `domat`, `isencrypt`, `password`, `idvbden`, `isbutphechidao`, `isvbdexuat`, `capduyetfinal`, `usertiepnhan`, `vanbanuutien`, `appid`, `idnvchitietgoc`, `dmNhiemvu2[]`.

### 4.2. `DM_NHIEMVU_CHITIET` — Nhiệm vụ (gốc: `DmNhiemvuChitietV2Model`, 91 trường → còn 26)

| Trường | Kiểu | Bắt buộc | Ghi chú |
|---|---|---|---|
| `id` | uuid | ✅ | PK |
| `idvb` | uuid | ✅ | FK → `DM_VANBAN.id` *(gốc dùng cả `idnv` và `idvb`; BE map `idnv → Idvb`. App mới **chỉ dùng 1 trường** `idvb`)* |
| `noidung` | varchar(2000) | ✅ | Nội dung nhiệm vụ |
| `linhvuc` | varchar(50) | ✅ | FK → `DM_LINHVUC` — **đầu vào AI** |
| `dokhan` | varchar(50) | ✅ | Mức độ ưu tiên — **đầu vào AI (trọng số tải)** |
| `hanxulyth` | date | | Thời hạn hoàn thành (chủ trì) |
| `songayhxlth` | int | | Số ngày thực hiện |
| `hanxulyph` | date | | Thời hạn phối hợp |
| `ngaygiao` | datetime | ✅ | |
| `ngaytiepnhan` | datetime | | **MỚI** — hệ gốc không có |
| `ngayhoanthanhthucte` | datetime | | |
| **`trangthai`** | int | ✅ | Trục A — mặc định `3` |
| **`trangthaiDvXuly`** | int | | Trục B — mặc định `null` |
| `trangthaixulygiahan` | int | | Trục C |
| `solangiahan` | int | ✅ | Mặc định `0`, tối đa `2` |
| `mucdoht` | int | | % hoàn thành, **0–100** (gốc không validate) |
| `phanhoi` | varchar(2000) | | Nội dung phản hồi khi kiểm tra kết quả |
| `hsChatluong` | int | | Điểm chất lượng 1–6 khi nghiệm thu — **đầu vào AI** |
| `userIdGiaoViec` | uuid | ✅ | Người giao — dùng cho quyền duyệt gia hạn |
| `useridcreate` | uuid | ✅ | |
| `unitcode` | varchar(50) | ✅ | |
| `createdate` / `updatedate` | datetime | ✅ | |
| `ai_goiy_id` | uuid | | FK → `AI_GOIY_LOG.id` — người này có phải do AI gợi ý không |

**LƯỢC BỎ**: `idnvgroup`, `parentIdCt`, `idnvchutri`, `idnvchitietgoc`, `nhiemvugiao`, `loainv` (định kỳ), `gioxuly`, `thuxuly`, `ngayketthucdk`, `isketthucdk`, `domat`, `isencrypt`, `password`, `captrinh`, `loaiSpcvId`, `hsTiendo`, `trangthaigiahan` *(gốc khai nhưng 0 nơi dùng)*, `tonghopTrangthai`, và toàn bộ 8 cờ do BE tính (`isxuly`, `ischuyentiep`, `isthuhoiphancong`, `istuchoi`, `isview`, `isnhomnv`, `isphoihop`, `istrinhdexuat`) — app mới **tính quyền trực tiếp từ trạng thái + bảng phân công**.

### 4.3. `NHIEMVU_PHANCONG` — Bảng nối người được giao (gốc: `UserThPhModel`)

| Trường | Kiểu | Bắt buộc | Ghi chú |
|---|---|---|---|
| `id` | uuid | ✅ | |
| `idnvchitiet` | uuid | ✅ | FK → `DM_NHIEMVU_CHITIET.id` |
| `userid` | uuid | ✅ | FK → `SYS_USER.id` |
| `unitcode` | varchar(50) | ✅ | |
| **`vaitro`** | varchar(10) | ✅ | `CHUTRI` \| `PHOIHOP` — **cải tiến** so với gốc (gốc phân biệt bằng việc nằm ở mảng `listUserTh` hay `listUserPh`, không có cột vai trò) |
| `useridCreate` | uuid | ✅ | Ai tạo phân công này |
| `trangthai` | int | ✅ | `1` = còn hiệu lực, `0` = đã thu hồi phân công |
| `createdate` | datetime | ✅ | |

Ràng buộc: mỗi `idnvchitiet` phải có **≥ 1** bản ghi `vaitro = 'CHUTRI'` còn hiệu lực. Một `userid` không được vừa `CHUTRI` vừa `PHOIHOP` trên cùng 1 nhiệm vụ (bám ràng buộc loại trừ của gốc).

### 4.4. `XULY_NHIEMVU` — Lịch sử xử lý / báo cáo (gốc: `XulyNhiemvuModel`)

| Trường | Kiểu | Bắt buộc | Ghi chú |
|---|---|---|---|
| `id` | uuid | ✅ | |
| `idCtnv` | uuid | ✅ | FK → `DM_NHIEMVU_CHITIET.id` |
| `loai` | varchar(20) | ✅ | `TIENDO` \| `BAOCAO` \| `TUCHOI` \| `THUHOI_BC` — **cải tiến**, gốc không phân loại |
| `noidung` | varchar(2000) | | Bắt buộc khi `loai = BAOCAO` và `trangthai ∈ {1,5}`; bắt buộc khi `loai = TUCHOI` |
| `mucdoht` | int | | Ảnh chụp % tại thời điểm báo cáo |
| `trangthai` | int | | Trạng thái nhiệm vụ chọn khi báo cáo |
| `trangthaiXuly` | int | | `= 10` khi gửi báo cáo |
| `useridXuly` | uuid | ✅ | |
| `ngayxuly` | datetime | ✅ | |

> **Cải tiến quan trọng**: hệ gốc **không lưu vết `mucdoht` theo thời gian** (tab Lịch sử không hiển thị `mucdoht` từng lần). App mới lưu ảnh chụp % ⇒ vẽ được đường tiến độ, và là **đầu vào cho đặc trưng "tốc độ triển khai"** của AI.

### 4.5. `GIAHAN_NHIEMVU` (gốc: `GiaHanNhiemVuRequest/Response`)

| Trường | Kiểu | Ghi chú |
|---|---|---|
| `id`, `idCtnv` | uuid | |
| `noiDung` | varchar(2000) | Lý do gia hạn |
| `hanxulydexuat` | date | ✅ Bắt buộc, `>= hanxulyth` hiện tại |
| `hanxulyth_cu` | date | Ảnh chụp hạn trước khi gia hạn |
| `trangThai` | int | `10` chờ / `11` duyệt / `12` từ chối |
| `phanhoi` | varchar(2000) | Ý kiến người duyệt |
| `useridDexuat`, `useridDuyet` | uuid | |
| `createDate`, `ngayduyet` | datetime | |

### 4.6. `NHIEMVU_FILE` — Tệp đính kèm (gốc: `ResponseModel`, rút gọn)

`id`, `recordid` (id bản ghi nghiệp vụ), `loai_ban_ghi` (`VANBAN` / `NHIEMVU` / `XULY` / `GIAHAN`), `fileName`, `filePath`, `fileSize`, `contentType`, `createBy`, `createDate`.

### 4.7. `SYS_USER` / `SYS_UNIT` (gốc: `IUserDataToken`, `SysUnitModel`)

**`SYS_UNIT`**: `unitcode` (PK), `tendonvi`, `macha` (self FK), `capdonvi` (int), `trangthai`.
**`SYS_USER`**: `id`, `username`, `password_hash`, `fullname`, `email`, `unitcode` (FK), `chucvu`, `vaitro` (`NGUOI_GIAO` / `NGUOI_THUC_HIEN` / `QUAN_TRI`), `trangthai`, **`max_concurrent_tasks`** *(mới — ngưỡng tải cho AI, mặc định 8)*.

> Hệ gốc dùng 3 trục "vai trò" chồng chéo (`Level` số, `Chucvu` chuỗi, `roles` JWT) và **không có enum nào định nghĩa `Level`** (chỉ thấy 3/4/6 rải rác). App mới dùng **1 trục `vaitro`** duy nhất.

### 4.8. Bảng phục vụ AI — chỉ 1 bảng tổng hợp + 1 view + 1 nhật ký

> **Không có bảng dữ liệu gốc nào được thêm.** Cả ba thứ dưới đây đều **sinh tự động** từ
> `DM_NHIEMVU_CHITIET`, `NHIEMVU_PHANCONG`, `XULY_NHIEMVU`. Không ai phải nhập tay.

**`USER_HIEUSUAT`** — bảng tổng hợp, do job nền ghi lại hằng đêm (cache cho nhanh)

| Trường | Kiểu | Tính từ |
|---|---|---|
| `userid` | uuid | PK |
| `linhvuc` | varchar(50) | PK — thống kê theo từng lĩnh vực |
| `so_nv_hoanthanh` | int | Đếm `trangthai ∈ {1,5}` **và** `trangthaiDvXuly = 11` |
| `so_nv_dunghan` | int | Đếm riêng `trangthai = 1` |
| `so_nv_bi_tralai` | int | Đếm `XULY_NHIEMVU.trangthaiDvXuly = 12` |
| `so_lan_giahan` | int | Tổng `solangiahan` |
| `updatedate` | datetime | |

> Đây **thuần tuý là bộ nhớ đệm**. Xoá sạch bảng rồi chạy lại job là dựng lại được y nguyên.
> Nếu dữ liệu ít (< 5.000 nhiệm vụ), có thể **bỏ hẳn bảng này** và tính thẳng bằng truy vấn
> lúc gọi API gợi ý — đơn giản hơn, chấp nhận chậm hơn vài chục mili giây.

**`USER_TAI_HIENTAI`** — view, tính tức thời (khối lượng đang gánh)

```sql
SELECT p.userid,
       COUNT(*) AS so_nv_dang_mo,
       SUM(CASE nv.dokhan WHEN 'DOTXUAT'  THEN 2.0
                          WHEN 'TRONGTAM' THEN 1.5
                          ELSE 1.0 END)                        AS tai_trong_so,
       SUM(CASE WHEN nv.trangthai = 7 THEN 1 ELSE 0 END)       AS so_nv_qua_han
FROM NHIEMVU_PHANCONG p
JOIN DM_NHIEMVU_CHITIET nv ON nv.id = p.idnvchitiet
WHERE p.vaitro = 'CHUTRI'
  AND nv.trangthai NOT IN (1, 5, 97)
GROUP BY p.userid;
```

**`AI_GOIY_LOG`** — nhật ký gợi ý, để đo hiệu quả AI cho báo cáo thực tập

| Trường | Kiểu | Ghi chú |
|---|---|---|
| `id` | uuid | |
| `idnvchitiet` | uuid | Nhiệm vụ được gợi ý |
| `linhvuc` | varchar(50) | Lĩnh vực lúc gợi ý |
| `ket_qua_json` | nvarchar(max) | Top-5 ứng viên + điểm + điểm thành phần |
| `userid_da_chon` | uuid | Người giao thực tế đã chọn ai (null nếu bỏ qua gợi ý) |
| `co_trong_goi_y` | bit | Người được chọn có nằm trong top-5 không |
| `phien_ban_trongso` | int | Để so sánh khi đổi trọng số |
| `createdate` | datetime | |

> **Đây là bảng quan trọng nhất cho phần bảo vệ**: từ nó tính ra **tỷ lệ chấp nhận gợi ý**
> — số liệu chứng minh AI có ích. Không có bảng này thì không có gì để báo cáo.

---
### 4.9. Sơ đồ quan hệ

```
DM_VANBAN (1) ──< (n) DM_NHIEMVU_CHITIET
                          │
                          ├──< (n) NHIEMVU_PHANCONG >── (1) SYS_USER
                          ├──< (n) XULY_NHIEMVU
                          ├──< (n) GIAHAN_NHIEMVU
                          └──< (n) NHIEMVU_FILE

SYS_USER (1) ──< (n) USER_HIEUSUAT  >── (1) DM_LINHVUC   (bảng tổng hợp, job nền sinh)
SYS_USER (1) ──< (n) USER_HIEUSUAT  >── (1) DM_LINHVUC
SYS_UNIT (1) ──< (n) SYS_USER ;  SYS_UNIT.macha ──> SYS_UNIT.unitcode (đệ quy)
DM_NHIEMVU_CHITIET (1) ──< (n) AI_GOIY_LOG
```


---


## 5. Danh sách API

Quy ước app mới: REST, prefix `/api/v1`, JSON, JWT Bearer. Cột "Gốc" ghi endpoint tương ứng của hệ QLNV để đội mới đối chiếu.

### 5.1. Xác thực & danh mục

| # | Method | Đường dẫn | Mô tả | Gốc |
|---|---|---|---|---|
| A1 | POST | `/api/v1/auth/login` | Đăng nhập, trả `accessToken`, `refreshToken`, thông tin user | `POST /Qtht/api/Auth/Authenticate?isLoginSSO=false` |
| A2 | POST | `/api/v1/auth/refresh` | Làm mới token | `POST /Qtht/api/Auth/Refresh-token` |
| A3 | POST | `/api/v1/auth/logout` | | `GET /Qtht/api/Auth/Logout` |
| A4 | GET | `/api/v1/danh-muc?type=TRANGTHAINV,TRANGTHAIPH,LOAIVB,DOKHAN` | Nạp danh mục theo mã type | `POST /Qtht/api/DmTudien/GetDmTudienByMa` |
| A5 | GET | `/api/v1/linh-vuc` | Cây lĩnh vực/nghiệp vụ | `qthtApiDmTudienGetDmTudienTreeByTypeAndUnit` |
| A6 | GET | `/api/v1/don-vi/cay` | Cây đơn vị + người dùng | `GET /Qtht/api/SysUnit/GetAllDonViAndUserBuildTree` |

### 5.2. Văn bản chỉ đạo

| # | Method | Đường dẫn | Mô tả | Gốc |
|---|---|---|---|---|
| B1 | GET | `/api/v1/van-ban?page=&size=&search=&linhvuc=&tuNgay=&denNgay=` | Danh sách phân trang | `POST /qlgiaonv/api/DmNhiemvuV2/Paging` |
| B2 | GET | `/api/v1/van-ban/{id}` | Chi tiết + tệp đính kèm | `GET /qlgiaonv/api/DmNhiemvu/SelectOneNhiemVu/{id}` |
| B3 | POST | `/api/v1/van-ban` | Tạo mới, trả `id` | `POST /qlgiaonv/api/DmNhiemvu/InsertOrUpdateDmNhiemvu` |
| B4 | PUT | `/api/v1/van-ban/{id}` | Cập nhật | (dùng chung B3 ở gốc) |
| B5 | DELETE | `/api/v1/van-ban/{id}` | Chỉ khi chưa có nhiệm vụ con | — |

### 5.3. Nhiệm vụ — tạo & giao

| # | Method | Đường dẫn | Mô tả | Gốc |
|---|---|---|---|---|
| C1 | POST | `/api/v1/van-ban/{idvb}/nhiem-vu` | **Tạo + giao nhiều nhiệm vụ 1 lần.** Body: `{ boQuaTrungNoidung, items: [{ noidung, linhvuc, dokhan, hanxulyth, songayhxlth, chuTri: [userid], phoiHop: [userid], aiGoiyId }] }`. Server đặt `trangthai = 3`, `trangthaiDvXuly = null` | `POST /qlgiaonv/api/DmNhiemvuV2/InsertMultipleNhiemVuItem` |
| C2 | POST | `/api/v1/nhiem-vu/kiem-tra-trung` | Dò trùng nội dung trước khi lưu. Body `{ items:[{tt, noiDung, dsUserChuTri[]}] }`. **Fail-open** | `POST /qlgiaonv/api/DmNhiemvuV2/CheckTrungNhiemVu` |
| C3 | GET | `/api/v1/nhiem-vu?vaiTro=TOI_GIAO\|TOI_LAM&trangThai=&quaHan=&page=&size=` | Danh sách nhiệm vụ theo vai | `POST .../PagingNhiemvuXuly` |
| C4 | GET | `/api/v1/nhiem-vu/{id}` | Chi tiết + danh sách phân công + lịch sử | `GET .../SelectOneNhiemvuChitiet` |
| C5 | PUT | `/api/v1/nhiem-vu/{id}` | Sửa nội dung/hạn (chỉ khi `trangthai = 3`) | `POST .../UpdateChitietNV` |
| C6 | POST | `/api/v1/nhiem-vu/{id}/thu-hoi` | Người giao thu hồi ⇒ `trangthai = 97`. Body `{ lyDo }` | `POST .../ThuhoiNV` |
| C7 | POST | `/api/v1/nhiem-vu/{id}/thu-hoi-phan-cong` | Rút phân công của 1 số người. Body `{ userIds:[] }` | `POST .../ThuhoiPhancongNhiemVu` |

### 5.4. Nhiệm vụ — thực hiện

| # | Method | Đường dẫn | Mô tả | Gốc |
|---|---|---|---|---|
| D1 | POST | `/api/v1/nhiem-vu/{id}/tiep-nhan` | `3 → 2`, ghi `ngaytiepnhan` | **KHÔNG CÓ Ở GỐC** — thiết kế mới |
| D2 | POST | `/api/v1/nhiem-vu/{id}/tu-choi` | Body `{ lyDo }` (bắt buộc) ⇒ `trangthai = 6`, `trangthaiDvXuly = 10` | `POST .../Xulynhiemvu` với `cateForm = REFUSE` |
| D3 | POST | `/api/v1/nhiem-vu/{id}/tien-do` | Body `{ mucdoht (0-100), noidung, fileIds[] }`. **Không đổi trạng thái** | `POST .../Xulynhiemvu` (gộp chung ở gốc) |
| D4 | POST | `/api/v1/nhiem-vu/{id}/bao-cao` | Body `{ trangthai, noidung, mucdoht, fileIds[] }` ⇒ đặt `trangthaiDvXuly = 10`. Server **validate lại** `trangthai` có nằm trong danh sách hợp lệ theo hạn không | `POST .../Xulynhiemvu` |
| D5 | GET | `/api/v1/nhiem-vu/{id}/trang-thai-hop-le` | Trả danh sách trạng thái được chọn khi báo cáo (đã lọc theo hạn) | (gốc lọc ở FE: `thong-tin-xu-ly-nhiem-vu.component.ts:1268-1278`) |
| D6 | POST | `/api/v1/nhiem-vu/{id}/thu-hoi-bao-cao` | ⇒ `trangthai = 2\|7`, `trangthaiDvXuly = null` | `POST .../UpdateChitietNV` |
| D7 | GET | `/api/v1/nhiem-vu/{id}/lich-su` | Lịch sử xử lý + phản hồi + gia hạn | `GET .../SelectOneXulyNhiemvu?id=&idnvgroup=` |

### 5.5. Kiểm tra kết quả

| # | Method | Đường dẫn | Mô tả | Gốc |
|---|---|---|---|---|
| E1 | POST | `/api/v1/nhiem-vu/{id}/nghiem-thu` | Body `{ ketQua: "DAT" \| "CHUA_DAT", phanHoi (bắt buộc), hsChatluong (1-6, chỉ khi ĐẠT) }`.<br>• `DAT` ⇒ `trangthaiDvXuly = 11`<br>• `CHUA_DAT` ⇒ `trangthaiDvXuly = 12` và nếu `trangthai ∈ {1,5}` thì `trangthai = 2` (còn hạn) / `7` (quá hạn) | `POST /qlgiaonv/api/DmNhiemvuV2/UpdateChitietNV` |

### 5.6. Gia hạn

| # | Method | Đường dẫn | Mô tả | Gốc |
|---|---|---|---|---|
| F1 | POST | `/api/v1/nhiem-vu/{id}/gia-han` | Đề xuất. Body `{ hanxulydexuat, noiDung, fileIds[] }` ⇒ `trangthai = 13`, `trangthaixulygiahan = 10`. Chặn khi `solangiahan >= 2` hoặc đã có yêu cầu treo | `POST .../gia-han-nv` |
| F2 | POST | `/api/v1/gia-han/{idGiaHan}/duyet` | Body `{ ketQua: "DUYET"\|"TU_CHOI", phanHoi }`. DUYET ⇒ `11`, `solangiahan += 1`, cập nhật `hanxulyth = hanxulydexuat`, `trangthai` về `2` nếu hạn mới còn hiệu lực. TU_CHOI ⇒ `12` | `POST .../duyet-gia-han-nv` |
| F3 | GET | `/api/v1/nhiem-vu/{id}/gia-han` | Lịch sử gia hạn | `GET .../lich-su-gia-han?idctnv=` |

> **Cảnh báo bàn giao**: hệ gốc FE **không có dòng nào gán lại `hanxulyth`** sau khi duyệt gia hạn — logic nằm hoàn toàn ở backend, không kiểm chứng được. App mới **phải tự đặc tả** hành vi này (đề xuất: F2 cập nhật `hanxulyth` như trên).

### 5.7. Tệp đính kèm

| # | Method | Đường dẫn | Mô tả |
|---|---|---|---|
| G1 | POST | `/api/v1/files` | Upload multipart, trả `[{id, fileName, filePath, size}]`. Whitelist: pdf, doc(x), xls(x), png, jpg. Tối đa 20MB/tệp |
| G2 | GET | `/api/v1/files/{id}` | Tải xuống |
| G3 | DELETE | `/api/v1/files/{id}` | Xoá (chỉ người upload, khi bản ghi chưa khoá) |

### 5.8. AI gợi ý người thực hiện ⭐

| # | Method | Đường dẫn | Mô tả |
|---|---|---|---|
| **H1** | **POST** | **`/api/v1/ai/goi-y-nguoi-thuc-hien`** | **Endpoint trọng tâm của đề tài.** Xem đặc tả body/response chi tiết ở mục `aiGoiY` |
| H2 | POST | `/api/v1/ai/goi-y/{goiyId}/ket-qua` | Ghi nhận người giao đã chọn ai (cập nhật `userid_da_chon`, `thu_hang_da_chon`) |
| H3 | GET | `/api/v1/ai/thong-ke` | Tỷ lệ chấp nhận top-1 / top-3, số lần gợi ý, phân bố điểm |
| H4 | GET | `/api/v1/ai/cau-hinh` / PUT | Xem/sửa bộ trọng số `w1..w5`, ngưỡng tải, hằng số làm mượt |

### 5.9. Quản trị & hồ sơ năng lực

| # | Method | Đường dẫn | Mô tả |
|---|---|---|---|
| I1 | GET/POST/PUT | `/api/v1/nguoi-dung` | CRUD người dùng |
| I2 | GET | `/api/v1/nguoi-dung/{id}/nang-luc` | *(chỉ để xem)* Thống kê số nhiệm vụ đã hoàn thành theo lĩnh vực — **suy từ lịch sử, không nhập tay** |
| I3 | GET | `/api/v1/nguoi-dung/{id}/hieu-suat` | Chỉ số hiệu quả theo lĩnh vực |
| I4 | POST | `/api/v1/jobs/cap-nhat-hieu-suat` | Kích hoạt thủ công job tổng hợp `USER_HIEUSUAT` (thường chạy cron 01:00) |
| I5 | POST | `/api/v1/jobs/cap-nhat-qua-han` | Job hằng ngày: `trangthai 2→7`, `3→7` khi `hanxulyth < hôm nay` |

### 5.10. Thống kê

| # | Method | Đường dẫn | Mô tả |
|---|---|---|---|
| J1 | GET | `/api/v1/dashboard/tong-quan` | Đếm theo trạng thái, quá hạn, sắp hết hạn (<3 ngày) |
| J2 | GET | `/api/v1/dashboard/theo-don-vi` | Số nhiệm vụ + tỷ lệ hoàn thành theo đơn vị |

### 5.11. API GỐC KHÔNG BÊ SANG (và lý do)

| Gốc | Lý do |
|---|---|
| `/DeXuatNhiemVu/{trinh-xu-ly-nv, duyet-dexuat-nv, trinh-dexuat-nv, tra-lai-vb-de-xuat}` | Lược bỏ nhánh trình cấp trên & văn bản đề xuất |
| `/ThongkeNhiemvu/UpdateStatusNvThongKe` | Ở gốc luôn gọi với **body rỗng** (`new BaoCaoModel()`) — không rõ ngữ nghĩa. Thay bằng job I5 |
| `/VanBanLienQuan/{Ocr, OcrHeader, ExtractNhiemVuStream}` | AI của đề tài là gợi ý người thực hiện, không phải OCR |
| `/AiCanhbaoNhiemvu/*` (9 endpoint) | Module cảnh báo chậm muộn, ngoài phạm vi |
| `/DmNhiemvuV2/{PhancongChiTietNhiemVuByDoi, ByCanhan, Th, Ph}` | 4 endpoint phân công khác nhau theo cấp — gộp còn C1 |
| `/Signature/{GetCert, SignPdf}` (localhost:6543) | Ký số đặc thù |
| `/DmNhiemvu/SelectStatus` | Ở gốc đã bị comment tại dashboard |


---


## 6. Phân quyền

### 6.1. Nguyên tắc khác biệt so với hệ gốc

Hệ gốc dùng **3 trục "vai trò" chồng chéo** và không trục nào là danh mục vai trò chuẩn:
- `Level` (số) — **không có enum nào định nghĩa**, chỉ thấy 3/4/6 rải rác trong `if`;
- `Chucvu` (chuỗi `CAN_BO`, `TRUONG_PHONG`… — **không có hằng số tập trung**);
- `roles` trong JWT — **mã chết**, hàm `roleMatch()` không có call site nào;
- Cộng thêm `RoleValue` 6 cờ (`isInsert/isView/isUpdate/isDelete/isSend/isApprove`) từ `lstMenu[].function`, **nhưng ở màn xử lý công việc quan trọng nhất thì `RoleValue` hoàn toàn không điều khiển gì** (tham chiếu duy nhất trong template đã bị comment).
- Ngoài ra có **2 bảng mã mâu thuẫn** cho cùng chuỗi `function`: `AUTH_ROLE` (3=Update, 4=Delete, 5=Send, 6=Approve) vs `RoleMenuData` (3=Xoá, 4=Cập nhật, 5=Import, 6=Export).

👉 **App nhỏ dùng 1 trục duy nhất**: `SYS_USER.vaitro ∈ {QUAN_TRI, NGUOI_GIAO, NGUOI_THUC_HIEN}`, kết hợp với **quyền theo dữ liệu** (người dùng có nằm trong bảng `NHIEMVU_PHANCONG` của nhiệm vụ đó không, có phải `userIdGiaoViec` không) — chính là cách hệ gốc **thực sự** kiểm quyền ở luồng nghiệp vụ.

### 6.2. Bảng vai trò × hành động

Ký hiệu: ✅ = được phép · ❌ = không · 🔸 = có điều kiện dữ liệu (ghi ở cột cuối)

| # | Hành động | QUAN_TRI | NGUOI_GIAO | NGUOI_THUC_HIEN | Điều kiện dữ liệu + trạng thái |
|---|---|:---:|:---:|:---:|---|
| 1 | Xem danh sách văn bản chỉ đạo | ✅ | ✅ | 🔸 | Người thực hiện chỉ thấy văn bản có nhiệm vụ giao cho mình |
| 2 | Tạo / sửa văn bản chỉ đạo | ✅ | ✅ | ❌ | Sửa: chỉ người tạo, và văn bản chưa có nhiệm vụ nào ở trạng thái ≠ 3 |
| 3 | Xoá văn bản chỉ đạo | ✅ | 🔸 | ❌ | Chỉ người tạo, và chưa có nhiệm vụ con |
| 4 | **Tạo & giao nhiệm vụ** | ✅ | ✅ | ❌ | — |
| 5 | **Dùng AI gợi ý người thực hiện** | ✅ | ✅ | ❌ | Chỉ trong màn phân công |
| 6 | Sửa nhiệm vụ đã giao | ✅ | 🔸 | ❌ | `userIdGiaoViec = tôi` **và** `trangthai = 3` |
| 7 | **Thu hồi nhiệm vụ** (→ 97) | ✅ | 🔸 | ❌ | `userIdGiaoViec = tôi` **và** `trangthai ∉ {1,5,97}` |
| 8 | **Thu hồi phân công** (rút 1 người) | ✅ | 🔸 | ❌ | `userIdGiaoViec = tôi` **và** `trangthai ∉ {1,5}` *(bám `canThuhoiPhancong`)* |
| 9 | **Tiếp nhận nhiệm vụ** | ❌ | ❌ | 🔸 | Tôi là `CHUTRI` **và** `trangthai = 3` |
| 10 | **Từ chối nhiệm vụ** | ❌ | ❌ | 🔸 | Tôi là `CHUTRI` **và** `trangthai ∈ {2,3}` **và** `solangiahan = 0` *(bám `coTheTuChoi`)* |
| 11 | **Cập nhật tiến độ** | ❌ | ❌ | 🔸 | Tôi là `CHUTRI` **và** `trangthai ∈ {2,3,7}` **và** `trangthaiDvXuly ∈ {null,12}` |
| 12 | **Gửi báo cáo kết quả** | ❌ | ❌ | 🔸 | Như #11 *(bám `coTheXuLy`: `((tt∈{1,5} && dv=12) \|\| tt∉{1,5,6} \|\| (tt=6 && dv=12)) && isxuly`)* |
| 13 | **Thu hồi báo cáo** | ❌ | ❌ | 🔸 | Tôi là `CHUTRI` **và** `trangthai ∈ {1,5}` **và** `trangthaiDvXuly = 10` |
| 14 | **Kiểm tra kết quả (Đạt / Chưa đạt)** | ✅ | 🔸 | ❌ | `trangthaiDvXuly = 10` **và** (`userIdGiaoViec = tôi` hoặc tôi là người tạo khi chưa chỉ định cán bộ) *(bám `coTheXacNhan`)* |
| 15 | **Xin gia hạn** | ❌ | ❌ | 🔸 | Tôi là `CHUTRI` **và** `trangthai ∈ {2,3,7}` **và** `trangthaixulygiahan ≠ 10` **và** `solangiahan < 2` *(bám `coTheGiaHan`)* |
| 16 | **Duyệt / từ chối gia hạn** | ✅ | 🔸 | ❌ | `trangthaixulygiahan = 10` **và** `userIdGiaoViec = tôi` *(bám `coTheDuyetGiaHan`)* |
| 17 | Nhắc việc | ✅ | 🔸 | ❌ | `userIdGiaoViec = tôi` **và** `trangthai ∉ {1,5,97}` |
| 18 | Xem chi tiết + lịch sử nhiệm vụ | ✅ | 🔸 | 🔸 | Là người giao, hoặc có tên trong `NHIEMVU_PHANCONG` (cả `CHUTRI` lẫn `PHOIHOP`) |
| 19 | Tải tệp đính kèm | ✅ | 🔸 | 🔸 | Như #18 |
| 20 | Xem Dashboard toàn hệ thống | ✅ | 🔸 | ❌ | Người giao chỉ thấy phạm vi đơn vị mình + đơn vị con |
| 21 | Quản lý người dùng | ✅ | ❌ | ❌ | — |
| 22 | **Sửa hồ sơ chuyên môn** | ✅ | 🔸 | 🔸 | Người giao sửa được của cấp dưới trong đơn vị; người thực hiện chỉ sửa của chính mình |
| 23 | Quản lý danh mục | ✅ | ❌ | ❌ | — |
| 24 | **Xem nhật ký & tinh chỉnh trọng số AI** | ✅ | ❌ | ❌ | Chỉ quản trị |

> ⚠️ Lưu ý riêng: `coTheDuyetGiaHan()` ở gốc có vế `(d.trangthai !== 1 || d.trangthai !== 5)` **luôn true** (lỗi logic, không lọc gì). Bảng trên đã bỏ vế lỗi này.

### 6.3. Vai PHỐI HỢP

Ở hệ gốc, việc phối hợp (`VIECPH`) khi lưu **không** đặt `trangthaiDvXuly = 10` ⇒ đơn vị phối hợp tự cập nhật trạng thái, không đi qua kiểm tra kết quả. App nhỏ **đơn giản hoá**: người `PHOIHOP` chỉ được **xem chi tiết, xem lịch sử, tải tệp và thêm ghi chú** — không có luồng trạng thái riêng. Lý do: giữ đúng 1 máy trạng thái duy nhất, tránh nhánh song song khó kiểm chứng.

### 6.4. Thực thi phân quyền

| Tầng | Cách làm |
|---|---|
| **Backend (bắt buộc)** | Mỗi endpoint kiểm 2 lớp: (1) `vaitro` cho phép gọi endpoint; (2) **kiểm quyền theo dữ liệu + trạng thái** đúng bảng 6.2, trả `403` nếu sai. Không tin bất cứ cờ nào do FE gửi lên |
| **Frontend** | Ẩn/hiện nút bằng đúng biểu thức ở bảng 6.2, tính từ `trangthai`, `trangthaiDvXuly`, `trangthaixulygiahan` và danh sách phân công — **không** dùng cờ do BE tính sẵn kiểu `isxuly`, `istuchoi` (gốc để BE tính nhưng **quy tắc tính không tồn tại trong FE** ⇒ không kiểm chứng được) |
| **Route guard** | Chặn cả `/quan-tri/*`. Hệ gốc **không có `canActivate` cho bất kỳ route `/admin/...` nào** (chỉ `/login` và các route `embed`) — đây là lỗ hổng cần khắc phục |


---


## 7. Công nghệ

### 7.1. Stack hệ gốc (để đối chiếu — KHÔNG bê nguyên)

| Lớp | Hệ gốc QLNV_FE |
|---|---|
| Framework | **Angular 15.2.10**, TypeScript ~4.9.5 (không bật `strict`), **RxJS 6.6.2** (+ `rxjs-compat`), zone.js 0.11.x, core-js 2.5.1 |
| UI | **Kendo UI for Angular 11.6.0** — 26 gói `@progress/*`, 21 module đăng ký toàn cục. **License trong repo (`kendo-ui-license.txt`) đã hết hạn 26/12/2024** |
| Theme/Layout | **Nebular 11.0.1** (theme, auth, security, eva-icons) — nền template `ngx-admin` v11 |
| CSS | Bootstrap 4.3.1 + FontAwesome 6 |
| Tài liệu/xuất file | docx-preview, exceljs, file-saver, jspdf + html2canvas, pdf-lib, jszip, Syncfusion DocumentEditor (license hard-code trong `main.ts`), Telerik Report Viewer |
| Realtime | `@microsoft/signalr` 8 → `{APP_QLGIAONV_URL}/hub` |
| Sinh API client | **NSwag 13.20** → 3 file: `app-qlgiaonv.service.ts` (**71.669 dòng / 483 endpoint**), `app-qtht.service.ts` (46.383 dòng / 371 endpoint), `app-guinhannv.service.ts` (5.102 dòng / 26 endpoint). **Không có file cấu hình `nswag.json` trong repo** |
| Kiến trúc BE | Microservice sau API gateway (`http://api.bca.local`), phân tách bằng path prefix: `/qlgiaonv`, `/Qtht`, `/GuinhanNv`, `/Kpi`, `/Chat` |
| Xác thực | SSO WSO2 IS hoặc Keycloak, chọn theo `SYSAPP.TRANGTHAISSO` (0/1/2) |
| Triển khai | Docker `nginx:alpine-openssl`, GitLab CI, Node 20 |

**Vấn đề của stack gốc** (nêu để đội mới không lặp lại):
1. Kendo license hết hạn — chi phí thương mại cao, không hợp cho app thực tập.
2. `strict: false` + 500+ class sinh tự động ⇒ nhiều lỗi kiểu chỉ lộ lúc chạy (ví dụ `chitietNhiemvu` khai V1 nhưng runtime là V2).
3. Nhiều mã chết đã xác nhận: `mammoth`, `tinymce`, `@angular/material`, `kendo-excel-export`, `echarts` (nạp mà không dùng), `roleMatch()`, `routingMach()`, `canShowGiaHan()`, 9 method NSwag `AiCanhbao*`…
4. Bí mật nằm trong repo: secret SSO trong `environment.ts`, mật khẩu registry + `SONAR_TOKEN` trong `.gitlab-ci.yml`.
5. Mỗi màn có bản `-embed` song sinh ⇒ 2 nguồn sự thật.

### 7.2. Stack cho app nhỏ — DÙNG ĐÚNG CÔNG NGHỆ HỆ GỐC

> **Quyết định:** app nhỏ dùng **cùng bộ công nghệ với QLNV**, không đổi sang stack khác.
> Mục tiêu của đề tài là làm ra sản phẩm *tương tự* hệ thống đang chạy, nên giữ nguyên
> công nghệ giúp: đối chiếu trực tiếp với hệ gốc, tái dùng được mẫu mã nguồn, và người
> hướng dẫn tại doanh nghiệp review được ngay.

| Lớp | Dùng gì | Ghi chú thi hành |
|---|---|---|
| **Framework FE** | **Angular 15.2.10**, TypeScript 4.9.5 | Đúng phiên bản hệ gốc. **Khuyến nghị bật `strict: true`** cho dự án mới — gốc để `false` nên nhiều lỗi kiểu chỉ lộ lúc chạy |
| **UI** | **Kendo UI for Angular 11.6.0** | Chỉ cần **7 module** thay vì 26: `Grid`, `DropDowns`, `DateInputs`, `Dialog`, `Inputs`, `Buttons`, `TreeView`. Xem 7.2.1 về license |
| **Theme/Layout** | **Nebular 11.0.1** + template `ngx-admin` | Dùng `nb-layout`, `nb-card`, `nb-menu` — dựng khung app rất nhanh |
| **CSS** | Bootstrap 4.3.1 + FontAwesome 6 | Đúng gốc |
| **Backend** | **ASP.NET Core (C#)** | Xác nhận từ NSwag v13.20 + NJsonSchema + Newtonsoft.Json trong tệp sinh tự động — đây là chuỗi công cụ .NET |
| **CSDL** | **SQL Server** | Suy từ .NET + quy ước đặt tên `SP_THONGKE_NHIEMVU`. *Chưa xác minh trực tiếp được vì chỉ đọc được frontend* |
| **Sinh API client** | **NSwag 13.20** | Giữ nguyên cách làm: BE phát OpenAPI → NSwag sinh service TypeScript. **Nhớ commit tệp `nswag.json`** — hệ gốc thiếu, không ai tái tạo được lệnh sinh |
| **Kiến trúc BE** | Một service duy nhất | Gốc là microservice sau API gateway (`/qlgiaonv`, `/Qtht`, `/GuinhanNv`…). App nhỏ **gộp về một service**, giữ path prefix `/qlgiaonv/api/...` để URL giống gốc |
| **Xác thực** | **JWT** (access + refresh) | Gốc dùng SSO WSO2/Keycloak — quá nặng cho app thực tập và **secret đang nằm trong `environment.ts`**. Dùng JWT tự phát hành, secret đọc từ biến môi trường |
| **Tệp đính kèm** | **MinIO** | Đúng gốc |
| **Realtime** | **SignalR** *(tuỳ chọn)* | Gốc có `@microsoft/signalr` → `{APP_QLGIAONV_URL}/hub`. Chỉ làm nếu còn thời gian, dùng cho thông báo giao việc |
| **Job nền** | **Hangfire** hoặc `BackgroundService` | Thay stored procedure `SP_THONGKE_NHIEMVU`. Hai job: cập nhật quá hạn `2→7`, tổng hợp `USER_HIEUSUAT` |
| **AI gợi ý** | **Service C# trong chính BE** | Thuật toán chấm điểm có trọng số, thuần C#, ~300 dòng, chạy < 50ms. **Giải thích được** — quan trọng khi bảo vệ đồ án |
| **Test** | Jasmine + Karma (FE), xUnit (BE) | Đúng gốc. *Không dùng Protractor — đã ngừng phát triển* |
| **Triển khai** | Docker: `nginx:alpine` + `aspnet` + `mssql` + `minio` | Gốc dùng `nginx:alpine-openssl`, GitLab CI, Node 20 |

#### 7.2.1. ⚠️ Vấn đề license Kendo — phải xử lý trước Tuần 4

Tệp `kendo-ui-license.txt` trong repo gốc **đã hết hạn 26/12/2024**. Kendo UI for Angular là
sản phẩm thương mại. Ba lựa chọn, chọn xong ghi vào báo cáo:

| Cách | Ưu | Nhược |
|---|---|---|
| **Xin license của doanh nghiệp** *(khuyến nghị)* | Đúng gốc 100%, không phải sửa code | Phụ thuộc B&T cấp |
| **Dùng bản dùng thử 30 ngày** | Đủ cho giai đoạn xây dựng | Hết hạn giữa chừng, watermark |
| **Thay riêng phần Kendo bằng Nebular + bảng tự viết** | Miễn phí hoàn toàn | Mất ~1 tuần, lệch gốc ở phần bảng dữ liệu |

Nebular, Bootstrap, FontAwesome đều miễn phí — chỉ **Kendo** là vướng.

### 7.3. Những gì KHÔNG bê từ gốc sang (và lý do)

| Thứ | Vì sao bỏ |
|---|---|
| `rxjs-compat`, `core-js 2.5.1` | Gói tương thích ngược cho code cũ. Dự án mới không cần |
| 19 trong 26 gói Kendo | App nhỏ không có PDF viewer, scheduler, gauge, chart… |
| Syncfusion DocumentEditor, Telerik Report Viewer | License riêng, chỉ phục vụ soạn thảo/báo cáo — ngoài phạm vi |
| `mammoth`, `tinymce`, `@angular/material`, `echarts` | **Mã chết trong gốc** — nạp mà không dùng |
| Kiến trúc microservice + API gateway | Một service là đủ; giữ prefix URL cho giống |
| SSO WSO2 / Keycloak | Nặng, và gốc đang để secret trong mã nguồn |
| Bản `-embed` của mỗi màn | Gốc mỗi màn có 2 bản ⇒ 2 nguồn sự thật, đã gây lỗi thật (mất điểm neo khi merge) |
| 3 tệp NSwag 123.000 dòng | App nhỏ chỉ cần ~40 endpoint |

---
### 7.4. Ràng buộc bắt buộc giữ lại từ gốc

1. **Tên trường phải giữ nguyên**: `trangthai`, `trangthaiDvXuly`, `trangthaixulygiahan`, `solangiahan`, `mucdoht`, `hanxulyth`, `hanxulyph`, `noidung`, `phanhoi`, `dokhan`, `linhvuc`, `hsChatluong` — để đối chiếu ngược với hệ gốc khi cần.
2. **Giá trị mã trạng thái giữ nguyên**: 1/2/3/5/6/7/13/97 và 10/11/12 (xem mục 2).
3. **Danh mục trạng thái vẫn để trong bảng `DM_TUDIEN`** với 2 mã type `TRANGTHAINV` / `TRANGTHAIPH` — giữ khả năng đổi nhãn không cần build lại.
4. **Danh mục độ khẩn**: `TRONGTAM` / `THUONGXUYEN` / `DOTXUAT` (đúng hằng `MUCDOUUTIEN` của gốc).


---


## 8. Phân rã công việc — 12 tuần

Giả định: **1–2 người**, 5 buổi/tuần. Mỗi tuần có **đầu ra kiểm chứng được** (demo hoặc test pass).

### Giai đoạn 1 — Phân tích & nền tảng (Tuần 1–2)

| Tuần | Công việc | Đầu ra |
|---|---|---|
| **T1** | • Đọc đặc tả này + soát lại mã gốc 5 màn lõi (`dm-nhiemvu-multi-insert`, `xu-ly-cv-phan-cong`, `thong-tin-xu-ly-nhiem-vu`, `phan-hoi-nhiemvu`, `gia-han-nv`)<br>• Chốt phạm vi: xác nhận danh sách GIỮ / LƯỢC BỎ ở mục 1.4<br>• **Xin dump bảng `DM_TUDIEN` (`type IN ('TRANGTHAINV','TRANGTHAIPH')`) từ DB hệ gốc** để có nhãn chính xác | Tài liệu phạm vi 3 trang + file `seed_trangthai.sql` |
| **T2** | • Vẽ sơ đồ máy trạng thái (mục 2.4) và duyệt với người hướng dẫn<br>• Thiết kế ERD (mục 4), viết migration EF Core<br>• Khởi tạo repo: FE Angular 15 + Kendo + Nebular, BE ASP.NET Core, `docker-compose.yml` (mssql + minio)<br>• Migration + seed: đơn vị, người dùng mẫu, lĩnh vực, danh mục trạng thái | `docker compose up` chạy được, DB có dữ liệu mẫu; ERD được duyệt |

### Giai đoạn 2 — Khung ứng dụng (Tuần 3–4)

| Tuần | Công việc | Đầu ra |
|---|---|---|
| **T3** | • BE: module Auth (JWT access + refresh, bcrypt), Guard vai trò, Interceptor lỗi chuẩn hoá<br>• BE: CRUD `SYS_USER`, `SYS_UNIT`, `DM_LINHVUC`, `DM_TUDIEN` (A1–A6, I1)<br>• FE: `nb-layout`, router, `AuthService`, `HttpInterceptor` gắn token + bắt lỗi 401 | Đăng nhập được, gọi được API danh mục; test unit cho Auth |
| **T4** | • Màn M01 Đăng nhập, M12 Danh mục, M11 Người dùng &amp; đơn vị<br>• **Seed dữ liệu nhiệm vụ lịch sử** (≥100 nhiệm vụ đã nghiệm thu, rải đều lĩnh vực và người) — đây mới là tiền đề của AI<br>• Thiết lập ESLint/Prettier/Karma, CI chạy lint + test | Đăng nhập, quản trị danh mục chạy được; DB có đủ lịch sử để AI chấm điểm |

### Giai đoạn 3 — Nghiệp vụ tạo & giao (Tuần 5–6)

| Tuần | Công việc | Đầu ra |
|---|---|---|
| **T5** | • BE: B1–B5 (văn bản chỉ đạo), G1–G3 (tệp, MinIO)<br>• FE: M03 danh sách văn bản, M04 form tạo/sửa, upload tệp | Tạo/sửa/xoá văn bản + đính kèm chạy được |
| **T6** | • BE: **C1 tạo & giao nhiều nhiệm vụ**, C2 dò trùng, C3–C5<br>• FE: **M05 màn phân công** — lưới nhiều dòng, chọn chủ trì/phối hợp từ cây đơn vị, validate 4 điều kiện, ràng buộc loại trừ chủ trì ⇄ phối hợp theo từng dòng<br>• Ràng buộc dùng chung: FE `Validators` + BE `FluentValidation` | **Demo mốc 1**: tạo 1 văn bản → giao 3 nhiệm vụ cho 3 người, trạng thái = 3 |

### Giai đoạn 4 — Nghiệp vụ thực hiện & nghiệm thu (Tuần 7–8)

| Tuần | Công việc | Đầu ra |
|---|---|---|
| **T7** | • BE: D1 tiếp nhận, D2 từ chối, D3 tiến độ, D4 báo cáo, D5 lọc trạng thái theo hạn, D6 thu hồi báo cáo, D7 lịch sử<br>• **Viết test máy trạng thái**: mỗi ô trong ma trận T1–T14 phải có ≥1 test (hợp lệ) + 1 test chặn (bất hợp lệ trả 403/400) | ≥ 25 test máy trạng thái pass |
| **T8** | • FE: M08 nhiệm vụ của tôi (chip lọc, badge hạn), M09 xử lý nhiệm vụ (3 chế độ)<br>• BE+FE: **E1 nghiệm thu** — 2 nút Đạt / Chưa đạt; M07<br>• Job I5 cập nhật quá hạn | **Demo mốc 2**: chạy trọn vòng Tạo → Giao → Tiếp nhận → Tiến độ → Báo cáo → **Chưa đạt** → làm lại → **Đạt** |

### Giai đoạn 5 — AI GỢI Ý (Tuần 9–10) ⭐ trọng tâm

| Tuần | Công việc | Đầu ra |
|---|---|---|
| **T9** | • Job I4: tổng hợp `USER_HIEUSUAT` từ dữ liệu lịch sử<br>• **Sinh dữ liệu mô phỏng**: script tạo ≥300 nhiệm vụ lịch sử đã kết thúc, phân bổ theo 5–8 lĩnh vực và 15–20 người dùng, có đúng hạn/trễ hạn/bị trả lại/điểm chất lượng<br>• Cài `RecommendationService`: 5 đặc trưng, chuẩn hoá, công thức tính điểm, làm mượt Laplace, xử lý cold-start<br>• Unit test từng đặc trưng + test tính bất biến (đổi thứ tự đầu vào không đổi kết quả) | **H1 trả JSON đúng đặc tả**, có `diem_thanhphan` và `ly_do` |
| **T10** | • FE: **M06 popup gợi ý** — thẻ ứng viên, thanh điểm, 4 thanh thành phần, lý do tiếng Việt, nhãn cold-start<br>• Nối vào M05: bấm "AI gợi ý" → chọn → tự điền chủ trì<br>• H2 ghi log kết quả chọn; H4 màn cấu hình trọng số<br>• **Hiệu chỉnh trọng số**: chạy trên tập mô phỏng, đo Precision@3 | **Demo mốc 3**: giao nhiệm vụ có AI gợi ý, giải thích được vì sao gợi ý người đó |

### Giai đoạn 6 — Hoàn thiện & bàn giao (Tuần 11–12)

| Tuần | Công việc | Đầu ra |
|---|---|---|
| **T11** | • Nhánh phụ: F1–F3 gia hạn (M10), C6/C7 thu hồi, nhắc việc<br>• M02 Dashboard + M13 nhật ký AI (H3: tỷ lệ chấp nhận top-1/top-3)<br>• E2E Playwright: 3 kịch bản trọn vòng (Đạt / Chưa đạt / Gia hạn)<br>• Rà bảo mật: kiểm quyền 2 lớp ở BE cho **mọi** endpoint, bí mật ra biến môi trường | E2E xanh; checklist bảo mật hoàn tất |
| **T12** | • Sửa lỗi tồn, tinh chỉnh UI, tối ưu truy vấn (index cho `idvb`, `trangthai`, `hanxulyth`, `userid`)<br>• Viết tài liệu: README cài đặt, tài liệu API (Swagger), sổ tay người dùng, **báo cáo thực tập** (đối chiếu app mới ↔ hệ gốc, số liệu hiệu quả AI)<br>• Chuẩn bị demo + slide bảo vệ | **Bàn giao**: mã nguồn + tài liệu + bản demo chạy bằng `docker compose up` |

### 8.1. Cột mốc kiểm soát

| Mốc | Tuần | Tiêu chí "đạt" |
|---|---|---|
| M1 — Nền tảng | T4 | Đăng nhập + nhập hồ sơ chuyên môn cho ≥15 user |
| M2 — Tạo & giao | T6 | Tạo văn bản → giao ≥3 nhiệm vụ, trạng thái đúng = 3 |
| M3 — Trọn vòng nghiệp vụ | T8 | Đi hết sơ đồ, **có cả nhánh Chưa đạt quay lại** |
| M4 — AI gợi ý | T10 | Popup gợi ý top-5 có điểm + lý do; Precision@3 ≥ 0,6 trên tập mô phỏng |
| M5 — Bàn giao | T12 | E2E xanh, tài liệu đủ, demo 1 lệnh |

### 8.2. Rủi ro & phương án dự phòng

| Rủi ro | Ảnh hưởng | Xử lý |
|---|---|---|
| Không xin được dump `DM_TUDIEN` từ hệ gốc | Nhãn trạng thái sai | Dùng bảng suy luận ở mục 2, ghi rõ "suy ra từ mã nguồn" trong báo cáo |
| Không có dữ liệu lịch sử thật → AI không có gì để học | **Chặn mốc M4** | Bắt buộc làm script sinh dữ liệu mô phỏng ở **T9** (không lùi) |
| Ôm đồm nhánh phụ (định kỳ, trình cấp trên, phối hợp) | Trễ 2–3 tuần | Đã lược bỏ từ đầu; nếu dư thời gian thì bổ sung ở T11 |
| AI chỉ là "công thức cộng có trọng số", bị hỏi vặn khi bảo vệ | Điểm số | Chuẩn bị: (1) so sánh với baseline chọn ngẫu nhiên / chọn người rảnh nhất; (2) số liệu Precision@3 và tỷ lệ chấp nhận từ `AI_GOIY_LOG`; (3) nêu lộ trình v2 dùng embedding |


---


## 9. AI gợi ý người thực hiện phù hợp ⭐

> Đây là điểm nhấn của đề tài. Đặc tả dưới đây đủ chi tiết để cài đặt trực tiếp.

### 9.1. Bài toán

**Đầu vào**: một nhiệm vụ chưa có người thực hiện (nội dung, lĩnh vực, độ khẩn, thời hạn) + phạm vi đơn vị được phép giao.
**Đầu ra**: danh sách top-N ứng viên, sắp xếp giảm dần theo điểm 0–100, **kèm lý do bằng tiếng Việt**.
**Tính chất bắt buộc**: *giải thích được* (explainable). Người giao phải hiểu vì sao và có toàn quyền bỏ qua gợi ý — hệ thống **hỗ trợ quyết định**, không tự động phân công.

### 9.2. Bốn nhóm đặc trưng — TÍNH TỪ DỮ LIỆU SẴN CÓ

> **Nguyên tắc thiết kế:** cả bốn yếu tố của đề tài đều được **suy ra từ chính lịch sử
> nhiệm vụ** đã có trong hệ thống. **Không có bảng khai báo năng lực, không có màn nhập
> tay, không cần ai đi điền hồ sơ.** Cắm vào là chạy — miễn là đã có dữ liệu nhiệm vụ.
>
> Đây là điểm khác bản thiết kế đầu: hệ gốc QLNV không lưu chuyên môn người dùng, nên
> thay vì bắt xây một phân hệ hồ sơ năng lực mới, ta **đọc chuyên môn ra từ việc người đó
> đã làm**. Cách này đúng tinh thần "giống phần mềm đang chạy", và bỏ được toàn bộ chi phí
> vận hành của việc khai báo, duyệt, cập nhật hồ sơ.

| Ký hiệu | Đặc trưng (theo đề tài) | Suy từ đâu | Trọng số |
|---|---|---|---|
| `S1` | **Chuyên môn** | Số nhiệm vụ **đã hoàn thành** theo từng `linhvuc` | **0,30** |
| `S2` | **Lịch sử thực hiện** | Tổng số nhiệm vụ đã hoàn thành | **0,20** |
| `S3` | **Hiệu quả công việc** | Tỷ lệ đúng hạn (`trangthai=1` so với `=5`) + số lần bị trả lại (`trangthaiDvXuly=12`) | **0,25** |
| `S4` | **Khối lượng hiện tại** | Số nhiệm vụ đang giữ (`trangthai ∈ {2,3,7}`), có trọng số theo độ khẩn | **0,20** |
| `S5` | Tính sẵn sàng *(bổ trợ)* | Số nhiệm vụ đang quá hạn (`trangthai = 7`) | **0,05** |

**Toàn bộ đọc từ 3 bảng đã có**: `DM_NHIEMVU_CHITIET`, `NHIEMVU_PHANCONG`, `XULY_NHIEMVU`.
Không thêm bảng dữ liệu gốc nào.

`w1 + … + w5 = 1,00`. Trọng số để trong bảng cấu hình, sửa được qua API H4.

---
### 9.3. Lọc cứng (chạy TRƯỚC khi chấm điểm)

Loại khỏi danh sách ứng viên nếu:
1. `SYS_USER.trangthai ≠ 1` (tài khoản khoá).
2. `vaitro` không chứa `NGUOI_THUC_HIEN`.
3. `unitcode` không thuộc phạm vi đơn vị được người giao chọn.
4. Đã có tên trong `NHIEMVU_PHANCONG` của chính nhiệm vụ này (tránh gợi ý trùng).
5. Đã từng **từ chối** nhiệm vụ này (`XULY_NHIEMVU.loai = 'TUCHOI'`).
6. `tai_trong_so >= max_concurrent_tasks × 1,5` (**quá tải nghiêm trọng**) — vẫn hiển thị nhưng gắn nhãn đỏ "Quá tải", đẩy xuống cuối. *Không loại hẳn để người giao vẫn thấy toàn cảnh.*

### 9.4. Công thức tính từng đặc trưng

Ký hiệu: `L` = lĩnh vực của nhiệm vụ; `u` = ứng viên. Mọi `Sᵢ ∈ [0, 1]`.

---
**S1 — Chuyên môn** (0,30) — suy từ lịch sử, KHÔNG cần khai báo

```
L = lĩnh vực của nhiệm vụ đang giao
n = số nhiệm vụ u ĐÃ HOÀN THÀNH thuộc lĩnh vực L
    (trangthai ∈ {1,5} VÀ trangthaiDvXuly = 11 VÀ vaitro = 'CHUTRI')

S1_chinh = min(1, n / 5)          // n=0→0,00  1→0,20  2→0,40  3→0,60  5→1,00

// Chưa từng làm lĩnh vực L -> xét lĩnh vực CÙNG NHÓM CHA (nếu danh mục có phân cấp)
Nếu n = 0:
    n' = số nhiệm vụ đã hoàn thành ở các lĩnh vực cùng nhóm cha với L
    S1 = 0,5 × min(1, n' / 5)     // chiết khấu 50% vì chỉ gần đúng
Ngược lại:
    S1 = S1_chinh
```

> **Vì sao ngưỡng 5?** Đủ nhỏ để người mới có cơ hội lọt vào gợi ý sau vài việc, đủ lớn để
> phân biệt người làm quen tay. Chỉnh được qua cấu hình `nguong_chuyen_mon`.
>
> **Vì sao chỉ đếm việc ĐÃ NGHIỆM THU** (`trangthaiDvXuly = 11`)? Nếu đếm cả việc đang làm
> thì người nhận nhiều mà chưa xong vẫn được điểm chuyên môn cao — sai bản chất.

---
**S2 — Lịch sử thực hiện** (0,20) — dùng log để người làm nhiều không nuốt hết điểm

```
N = TỔNG số nhiệm vụ đã hoàn thành của u (mọi lĩnh vực)
S2 = ln(1 + N) / ln(1 + 10),  cắt ngưỡng tại 1
// N=0→0,00  1→0,29  3→0,58  5→0,75  10→1,00  30→1,00
```

---
**S3 — Hiệu quả công việc** (0,25) — suy từ kết quả nghiệm thu

```
// (a) Tỷ lệ đúng hạn. Hệ gốc đã LỌC combobox theo hạn nên 2 mã này phản ánh đúng thực tế:
//     trangthai = 1 -> hoàn thành TRONG hạn ; trangthai = 5 -> hoàn thành SAU hạn
so_dunghan   = đếm nhiệm vụ của u có trangthai = 1 và trangthaiDvXuly = 11
so_hoanthanh = đếm nhiệm vụ của u có trangthai ∈ {1,5} và trangthaiDvXuly = 11

// Làm mượt Laplace: người mới ít việc không bị điểm cực đoan 0 hoặc 1
p0 = 0,70                       // tiên nghiệm toàn hệ thống
α  = 5                          // số "quan sát ảo"
r_dunghan = (so_dunghan + α × p0) / (so_hoanthanh + α)

// (b) Phạt bị trả lại — đếm số lần người giao chấm CHƯA ĐẠT
so_tralai   = đếm bản ghi XULY_NHIEMVU của u có trangthaiDvXuly = 12
tyle_tralai = so_tralai / max(1, so_hoanthanh)

// (c) Phạt gia hạn
tyle_giahan = Σ solangiahan / max(1, so_hoanthanh)

S3 = clamp(0, 1, r_dunghan − 0,3 × tyle_tralai − 0,15 × tyle_giahan)
```

> **Bỏ `hsChatluong`** khỏi công thức. Trường này có trong model gốc nhưng chỉ được ghi khi
> nhiệm vụ gắn với KPI (`loaiSpcvId`), nên đa số bản ghi rỗng — đưa vào chỉ thêm nhiễu.
> Đội nào có dữ liệu KPI đầy đủ thì cộng thêm thành phần `(hsChatluong − 1) / 5` với trọng
> số nhỏ.

---
---
**S4 — Khối lượng hiện tại** (0,20) — càng rảnh điểm càng cao

```
// Tải có trọng số theo độ khẩn (bám hằng MUCDOUUTIEN của hệ gốc)
w(dokhan): DOTXUAT = 2,0 | TRONGTAM = 1,5 | THUONGXUYEN = 1,0

tai = Σ w(dokhan) của các nhiệm vụ u đang giữ vai CHUTRI
      và trangthai ∉ {1, 5, 97}
K   = SYS_USER.max_concurrent_tasks (mặc định 8)

S4 = clamp(0, 1, 1 − tai / K)
// tai=0 →1,00 ; tai=4 →0,50 ; tai=8 →0,00 ; tai>8 →0,00 + nhãn "Quá tải"
```

---
**S5 — Tính sẵn sàng** (0,05)

```
q = số nhiệm vụ của u đang ở trangthai = 7 (đang triển khai, đã quá hạn)
S5 = 1 − min(1, q / 3)      // q=0 →1,00 ; q=1 →0,67 ; q≥3 →0,00
```

---
**Điểm tổng**

```
DIEM = 100 × (0,30·S1 + 0,20·S2 + 0,25·S3 + 0,20·S4 + 0,05·S5)
```

**Hệ số tin cậy** (không nhân vào điểm, chỉ hiển thị):
```
do_tin_cay = clamp(0, 1, (so_nv_hoanthanh_tong / 10) × 0,6 + (co_ho_so_chuyen_mon ? 0,4 : 0))
// < 0,4  ⇒ hiện nhãn "Dữ liệu còn ít"
```

### 9.5. Xử lý dữ liệu thiếu (cold start) — bắt buộc cài

| Tình huống | Xử lý | Hiển thị |
|---|---|---|
| Người dùng mới, chưa có nhiệm vụ nào | `S2 = 0`; `S3` = giá trị tiên nghiệm (`r_dunghan = p0 = 0,70`, `r_chatluong = 0,60`, `phat = 0`) ⇒ `S3 ≈ 0,655`; `S4 = 1` (rảnh hoàn toàn) | Nhãn xám **"Người mới – chưa có dữ liệu lịch sử"** |
| Chưa khai hồ sơ chuyên môn | Dùng `S1_suyluan`; nếu vẫn = 0 thì `S1 = 0` | Nhãn **"Chưa khai báo chuyên môn"** + link tới M11 |
| Nhiệm vụ không có lĩnh vực | Bỏ ràng buộc lĩnh vực: `S1` tính trên TỔNG số nhiệm vụ đã hoàn thành (mọi lĩnh vực) thay vì theo lĩnh vực; `S2` giữ nguyên | Cảnh báo trên popup: *"Nhiệm vụ chưa gán lĩnh vực — độ chính xác gợi ý giảm"* |
| Chưa từng nghiệm thu (`hsChatluong` toàn null) | `r_chatluong = 0,60` | Không hiển thị thanh chất lượng |
| **Toàn hệ thống chưa có dữ liệu** (mới triển khai) | Chuyển sang **chế độ dự phòng**: chỉ xếp theo `S1` và `S4` với `w = 0,6 / 0,4` | Banner: *"Chế độ khởi tạo: gợi ý dựa trên chuyên môn và khối lượng hiện tại"* |

**Nguyên tắc**: không bao giờ trả danh sách rỗng. Nếu mọi ứng viên đều 0 điểm thì xếp theo `S4` (ai rảnh nhất) và nói rõ lý do.

### 9.6. Hợp đồng API H1

**Request** `POST /api/v1/ai/goi-y-nguoi-thuc-hien`
```json
{
  "noidung": "Rà soát và báo cáo tình hình triển khai hệ thống một cửa điện tử quý III",
  "linhvuc": "CNTT",
  "dokhan": "TRONGTAM",
  "hanxulyth": "2026-10-15",
  "phamViUnitCode": ["P01", "P02"],
  "loaiTru": ["uuid-user-da-chon"],
  "soLuong": 5
}
```

**Response** `200`
```json
{
  "goiyId": "b1f2...-uuid",
  "phienBanTrongSo": "v1.0",
  "cheDo": "DAY_DU",
  "canhBao": [],
  "ungVien": [
    {
      "userid": "u-001",
      "fullname": "Nguyễn Văn A",
      "chucvu": "Chuyên viên",
      "unitname": "Phòng CNTT",
      "diemTong": 87.4,
      "doTinCay": 0.92,
      "nhan": [],
      "diemThanhPhan": {
        "chuyenMon":   { "diem": 1.00, "trongSo": 0.30, "gopPhan": 30.0 },
        "lichSu":      { "diem": 0.86, "trongSo": 0.20, "gopPhan": 17.2 },
        "hieuQua":     { "diem": 0.88, "trongSo": 0.25, "gopPhan": 22.0 },
        "khoiLuong":   { "diem": 0.75, "trongSo": 0.20, "gopPhan": 15.0 },
        "sanSang":     { "diem": 0.65, "trongSo": 0.05, "gopPhan":  3.2 }
      },
      "lyDo": [
        "Chuyên môn CNTT ở mức Chuyên gia (5/5)",
        "Đã hoàn thành 7 nhiệm vụ cùng lĩnh vực",
        "Tỷ lệ đúng hạn 86% (6/7), điểm chất lượng trung bình 5,2/6",
        "Đang giữ 2 nhiệm vụ (tải 2,0/8 — còn nhiều dư địa)"
      ],
      "soLieu": {
        "mucThanhThao": 5, "soNvHoanThanh": 7, "soNvDungHan": 6,
        "diemChatLuongTb": 5.2, "soNvDangMo": 2, "taiTrongSo": 2.0, "soNvQuaHan": 1
      }
    }
  ]
}
```

**Sinh câu lý do** (mẫu cố định, không dùng LLM ⇒ ổn định, không tốn chi phí):

| Thành phần | Điều kiện | Mẫu câu |
|---|---|---|
| Chuyên môn | có `m` | `"Chuyên môn {tenLinhVuc} ở mức {nhãn m} ({m}/5)"` — nhãn: 1 Mới biết · 2 Cơ bản · 3 Thành thạo · 4 Giỏi · 5 Chuyên gia |
| Chuyên môn | suy luận | `"Chưa khai báo chuyên môn, nhưng đã làm {n} nhiệm vụ thuộc lĩnh vực này"` |
| Lịch sử | `n > 0` | `"Đã hoàn thành {n} nhiệm vụ cùng lĩnh vực"` |
| Lịch sử | `n = 0` | `"Chưa từng thực hiện nhiệm vụ thuộc lĩnh vực này"` |
| Hiệu quả | có dữ liệu | `"Tỷ lệ đúng hạn {x}% ({a}/{b}), điểm chất lượng trung bình {y}/6"` |
| Hiệu quả | bị trả lại nhiều | `"Lưu ý: {k} nhiệm vụ từng bị trả lại yêu cầu bổ sung"` |
| Khối lượng | `S4 ≥ 0,5` | `"Đang giữ {n} nhiệm vụ (tải {t}/{K} — còn nhiều dư địa)"` |
| Khối lượng | `S4 < 0,25` | `"⚠ Đang khá bận: {n} nhiệm vụ (tải {t}/{K})"` |
| Sẵn sàng | `q > 0` | `"⚠ Đang có {q} nhiệm vụ quá hạn"` |

### 9.7. Hiển thị trên giao diện (M06)

```
┌─────────────────────────────────────────────────────────────┐
│  🤖 Gợi ý người thực hiện        Lĩnh vực: CNTT · Hạn 15/10 │
│  ─────────────────────────────────────────────────────────  │
│  ①  Nguyễn Văn A · Chuyên viên · Phòng CNTT      87,4 điểm  │
│      ████████████████████░░░                                │
│      Chuyên môn  ██████████ 1,00   Kinh nghiệm ████████ 0,86│
│      Hiệu quả    ████████▉  0,88   Khối lượng  ███████  0,75│
│      • Chuyên môn CNTT ở mức Chuyên gia (5/5)               │
│      • Đã hoàn thành 7 nhiệm vụ cùng lĩnh vực               │
│      • Đúng hạn 86% (6/7), chất lượng TB 5,2/6              │
│      • Đang giữ 2 nhiệm vụ (tải 2,0/8)          [  Chọn  ]  │
│  ─────────────────────────────────────────────────────────  │
│  ②  Trần Thị B · Phòng CNTT     [Dữ liệu còn ít]  71,2 điểm │
│  ③  Lê Văn C   · Phòng CNTT     [⚠ Quá tải]       48,6 điểm │
│  ─────────────────────────────────────────────────────────  │
│  [ Xem thêm 5 người ]              [ Bỏ qua, chọn thủ công ]│
└─────────────────────────────────────────────────────────────┘
```

Quy tắc trình bày:
- Thanh điểm tổng dùng **1 màu trung tính**, không dùng đỏ/xanh gây hiểu nhầm "tốt/xấu".
- 4 thanh thành phần luôn hiển thị đủ (kể cả bằng 0) để so sánh ngang hàng.
- Tối đa **4 dòng lý do**, ưu tiên: chuyên môn → kinh nghiệm → hiệu quả → khối lượng.
- Luôn có nút **"Bỏ qua, chọn thủ công"** — nhấn mạnh tính hỗ trợ quyết định.
- Cảnh báo tuân thủ: chân popup ghi *"Gợi ý dựa trên dữ liệu lịch sử trên hệ thống. Quyết định phân công thuộc về người giao nhiệm vụ."*

### 9.8. Đo hiệu quả (số liệu cho báo cáo thực tập)

| Chỉ số | Công thức | Ngưỡng mục tiêu |
|---|---|---|
| **Precision@1** | số lần người giao chọn đúng ứng viên hạng 1 / tổng số lần gợi ý | ≥ 0,35 |
| **Precision@3** | số lần người được chọn nằm trong top-3 / tổng | ≥ 0,60 |
| **Tỷ lệ chấp nhận** | số lần chọn từ danh sách gợi ý / tổng số lần mở popup | ≥ 0,70 |
| **MRR** | trung bình `1 / thu_hang_da_chon` | ≥ 0,50 |
| **Độ lệch tải** (Gini) | Hệ số Gini của phân bố `so_nv_dang_mo` — trước và sau khi dùng AI | **giảm** |
| **Tỷ lệ đúng hạn** | so sánh nhóm nhiệm vụ giao theo gợi ý vs giao thủ công | **cao hơn** |

Tất cả tính từ `AI_GOIY_LOG`, hiển thị ở màn M13.

**So sánh baseline bắt buộc có trong báo cáo**: chấm điểm có trọng số **vs** (a) chọn ngẫu nhiên, (b) chọn người rảnh nhất, (c) chọn người có chuyên môn cao nhất. Kết quả kỳ vọng: mô hình đầy đủ tốt hơn cả 3 baseline ở Precision@3 và độ đồng đều tải.

### 9.9. Lộ trình mở rộng (giai đoạn 2, KHÔNG bắt buộc để nghiệm thu)

1. **So khớp ngữ nghĩa**: nhúng `noidung` nhiệm vụ và mô tả năng lực bằng mô hình đa ngữ (`paraphrase-multilingual-MiniLM`), lưu vào `pgvector`, thêm đặc trưng `S6 = cosine(v_nhiemvu, v_hoso)` với trọng số nhỏ (0,10) trừ vào `w1`. Ưu điểm: bắt được nhiệm vụ chưa gán đúng lĩnh vực.
2. **Học trọng số từ phản hồi**: khi `AI_GOIY_LOG` có ≥ 500 bản ghi, huấn luyện Logistic Regression / LambdaMART với nhãn = "được chọn hay không", đầu vào là 5 điểm thành phần ⇒ tự học `w`. Vẫn giải thích được vì đặc trưng không đổi.
3. **Gợi ý cả nhóm**: khi nhiệm vụ cần nhiều người, tối ưu tổ hợp (phủ đủ chuyên môn + cân tải) thay vì chọn top-k độc lập.

> **Trung thực về thuật ngữ khi bảo vệ**: lõi v1 là **hệ thống chấm điểm đa tiêu chí có trọng số (MCDM/rule-based)**, không phải mạng nơ-ron. Đây là lựa chọn *có chủ đích*: dữ liệu ban đầu quá ít để huấn luyện mô hình học máy, và yêu cầu giải thích được trong môi trường hành chính là bắt buộc. Lộ trình học từ phản hồi ở mục 9.9.2 là bước tiến tự nhiên khi đủ dữ liệu.


---


## 10. CẢNH BÁO — Những gì hệ thống gốc KHÔNG có

Phần này liệt kê những chỗ **không thể sao chép từ mã nguồn QLNV** vì chúng không tồn tại. Đội mới **phải tự thiết kế** và ghi rõ trong báo cáo rằng đây là phần bổ sung.

### 10.1. ⚠️ Hệ gốc KHÔNG lưu chuyên môn người dùng — đã có cách vòng

Hệ gốc **không có bảng, trường hay màn hình nào** lưu năng lực/kỹ năng của người dùng:

- `IUserDataToken` (auth.service.ts:20-41) chỉ có `Id, FullName, UnitCode, Level, Chucvu…`
- `ListUserModel`, `TreeListUserAndUnit`, `UserThPhModel` chỉ có `unitcode`, `chucvu`, `capdonvi`
- `linhvuc` là thuộc tính của **nhiệm vụ**, không phải của **người**

**Cách xử lý đã chọn (mục 9.2):** *không* xây phân hệ hồ sơ năng lực. Thay vào đó **suy
chuyên môn từ lịch sử** — đếm số nhiệm vụ đã nghiệm thu của mỗi người theo từng `linhvuc`.

| | Xây bảng khai báo | Suy từ lịch sử *(đã chọn)* |
|---|---|---|
| Công sức | Bảng + màn nhập + quy trình duyệt (~1 tuần) | Một truy vấn gộp |
| Vận hành | Phải có người khai và cập nhật liên tục | Tự cập nhật theo việc thực tế |
| Độ chính xác | Chủ quan, dễ khai vống | Phản ánh việc đã làm thật |
| Nhược điểm | — | **Người mới chưa có lịch sử ⇒ điểm 0** |

**Hệ quả phải chấp nhận:** người mới vào bị điểm chuyên môn thấp trong vài việc đầu.
Xử lý ở mục 9.5 (cold start): khi cả đơn vị đều ít dữ liệu, hệ thống **hạ trọng số `S1`**
và đôn `S4` (ai rảnh hơn) lên, kèm nhãn *"Dữ liệu còn ít"* để người giao biết mà cân nhắc.

### 10.2. ⚠️ Hệ gốc không đo hiệu quả công việc — dùng kết quả nghiệm thu thay thế

- `hsChatluong` (thang 1–6) chỉ ghi khi nhiệm vụ gắn KPI (`loaiSpcvId`) ⇒ **đa số rỗng**
- `hsTiendo` có trong model nhưng **không nơi nào ghi** trong frontend
- `mucdoht` (%) **nhập tay**, `maxlength=3`, **không validate 0–100**, không lưu vết theo thời gian
- Quy tắc chấm trạng thái nằm trong stored procedure `SP_THONGKE_NHIEMVU` — không đọc được

**Cách xử lý đã chọn:** đo hiệu quả bằng **kết quả nghiệm thu**, thứ hệ gốc đã ghi đầy đủ:

| Đo cái gì | Lấy từ |
|---|---|
| Đúng hạn hay trễ | `trangthai = 1` (trong hạn) so với `= 5` (sau hạn) |
| Bị trả về làm lại | `trangthaiDvXuly = 12` |
| Phải xin thêm thời gian | `solangiahan` |

Ba chỉ số này **đã có sẵn** trong mọi bản ghi nhiệm vụ, không cần thêm gì. Công thức ở 9.4.

`hsChatluong` **bị loại khỏi công thức** vì thưa dữ liệu — đội nào có KPI đầy đủ thì cộng
thêm với trọng số nhỏ.

> **Việc bắt buộc phải làm:** validate `mucdoht` trong khoảng 0–100. Hệ gốc **không có**
> ràng buộc này, ô nhập là `type="text"` — dữ liệu bẩn sẽ làm sai mọi thống kê phía sau.

---
### 10.3. ⛔ KHÔNG CÓ BƯỚC "TIẾP NHẬN"

Sơ đồ nghiệp vụ đích có bước "Tiếp nhận" nhưng hệ gốc **không có nút, không có API, không có trạng thái "đã tiếp nhận"** trên nhiệm vụ (đã grep toàn `src/`).
- Thứ gần nhất: cờ `isview` tự bật khi **mở dialog chi tiết** (`XemNhiemvu()`) — chỉ là "đã xem".
- API duy nhất có chữ `tiep-nhan` là `/qlgiaonv/api/AiCanhbaoNhiemvu/tiep-nhan/{id}` — thuộc **module cảnh báo AI**, không phải tiếp nhận nhiệm vụ, và là **mã chết** (0 call site).
- Việc chuyển `3 (Chưa triển khai) → 2 (Đang triển khai)` ở gốc chỉ xảy ra khi người thực hiện **tự chọn trạng thái** trong combobox lúc báo cáo.

**Phải tự làm**: hành động Tiếp nhận (D1), trường `ngaytiepnhan`, và quyết định có SLA tiếp nhận hay không (gốc **không có SLA nào**).

### 10.4. ⛔ KHÔNG CÓ TRẠNG THÁI KHỞI TẠO xác định ở FE

Đã grep 7 màn tạo/giao (`dm-nhiemvu-crud`, `dm-nhiemvu-chitiet`, `dm-nhiemvu-multi-insert`, `dm-nhiemvu-giaodoi`, `dm-nhiemvu-giaocanbo`, `dm-nhiemvu-forward`, `phan-cong-dv-pb`): **không dòng nào gán `trangthai` hay `trangthaiDvXuly`** cho payload tạo mới. Backend quyết định, quy tắc không đọc được.
Khi tra không ra nhãn, UI hiển thị mặc định `"Chưa triển khai"` (fallback) hoặc `"-"`.

**Phải tự làm**: quy định `trangthai = 3`, `trangthaiDvXuly = null` (đã đặc tả ở mục 2).

### 10.5. ⛔ KHÔNG CÓ BẢNG MÃ→NHÃN CHÍNH THỨC của `TRANGTHAINV` / `TRANGTHAIPH`

Không tồn tại trong FE dưới **bất kỳ dạng nào**: enum, const, JSON, mock, SQL seed. Nhãn nạp runtime từ `POST /Qtht/api/DmTudien/GetDmTudienByMa`.
Bảng ở mục 2 là **suy luận** từ comment mã nguồn + nhánh gán/so sánh. Cụ thể chưa xác định được:
- **Nhãn của mã 4**: chỉ xuất hiện đúng 1 lần trong mảng lọc bỏ, không nơi nào gán/hiển thị.
- **Nhãn của mã 97**: FE chỉ gán rồi thôi; chuỗi "Đã thu hồi" **không tồn tại** trong repo.
- **Nhãn của mã 100**: chỉ có icon, không có nơi gán.
- **Nhãn 'Chưa triển khai - quá hạn'** mà nghiệp vụ hay nhắc: **không tìm thấy** ở bất kỳ file nào — có thể là cách gọi khác của mã 7.

**Phải tự làm**: xin dump `SELECT * FROM DM_TUDIEN WHERE type IN ('TRANGTHAINV','TRANGTHAIPH')`, hoặc chấp nhận bảng suy luận và ghi rõ trong báo cáo.

### 10.6. ⛔ KHÔNG CÓ QUY TẮC TÍNH các cờ quyền

Các cờ điều khiển toàn bộ nút bấm — `isxuly`, `ischuyentiep`, `isthuhoiphancong`, `istuchoi`, `isview`, `isnhomnv`, `istrinhdexuat`, `canNhacViec` — **do backend tính, quy tắc không có trong FE**.
Riêng `istuchoi` thực ra **không phải trường của model**, mà là bí danh FE tự gán `= isxuly` (xu-ly-cv-phan-cong.component.ts:1049).

**Phải tự làm**: đây là phần **quyền hạn cốt lõi**. Đặc tả đã thay bằng bảng 6.2 tính trực tiếp từ trạng thái + bảng phân công — **không dùng lại cơ chế cờ**.

### 10.7. ⛔ KHÔNG CÓ ràng buộc bắt buộc nhập lý do

Ở hệ gốc, **không** bắt buộc lý do ở: Từ chối nhiệm vụ (ô "Nội dung xử lý" chỉ `required` khi trạng thái là '1'/'5'), Thu hồi báo cáo (dialog không có ô), Thu hồi phân công (không có ô), Kiểm tra kết quả (nội dung phản hồi không bắt buộc), Trả lại đề xuất.
Chỉ tài liệu hướng dẫn (guided tour) mới ghi "nhập lý do từ chối" — nhưng đó **không phải validator**.

**Nếu app mới bắt buộc lý do (khuyến nghị: CÓ) thì đó là yêu cầu MỚI**, phải ghi rõ, không được trình bày như hiện trạng.

### 10.8. ⛔ KHÔNG CÓ hành vi hệ thống sau khi Nhắc việc / bị Từ chối / bị Thu hồi

FE chỉ POST rồi hiện toast và đóng dialog. **Không tìm thấy**: gửi thông báo/email cho người liên quan, đếm số lần bị nhắc, badge "đã bị nhắc", hay bất kỳ thay đổi trạng thái nào. Cũng **không có** cơ chế nhắc việc tự động (cron).

**Phải tự làm** nếu muốn có thông báo.

### 10.9. ⛔ KHÔNG XÁC ĐỊNH được nhiều quy tắc backend

| Không có trong FE | Hệ quả |
|---|---|
| Sau khi **duyệt gia hạn**, `hanxulyth` có tự cập nhật thành `hanxulydexuat` không | Phải tự đặc tả (đề xuất ở F2) |
| Cơ chế sinh `idnvgroup` | App mới đã bỏ khái niệm nhóm nhiệm vụ |
| Quy tắc chấm `TRANGTHAITHONGKE` (job nền) | Phải tự viết job I5 |
| Schema response `CheckTrungNhiemVu` | FE dùng `any`, chỉ đọc `res.data.groups` |
| Ngưỡng/thuật toán dò trùng | Nằm hoàn toàn ở BE |
| Body của `UpdateStatusNvThongKe` | FE luôn gửi **object rỗng** — không rõ ngữ nghĩa |
| Cơ chế kiểm quyền phía server | Không kiểm chứng được từ FE; **không có route guard cho `/admin/*`** |

### 10.10. ⚠️ MÃ CHẾT / LỖI trong hệ gốc — TUYỆT ĐỐI KHÔNG SAO CHÉP

| Vị trí | Vấn đề |
|---|---|
| `canShowGiaHan()` (xu-ly-cv-phan-cong.component.ts:2014) | **0 call site**, VÀ luôn `false` do so sánh `'"VIECDVGIAO"'` (nháy kép lồng). Comment giải nghĩa mã trạng thái tốt nhất của FE lại nằm trong hàm chết này. Ràng buộc "gia hạn khi còn <3 ngày" **không có hiệu lực** |
| `coTheDuyetGiaHan()` (:1737) | Vế `(d.trangthai !== 1 \|\| d.trangthai !== 5)` **luôn true** — không lọc gì |
| Template lưới đọc `dataItem.trangthaixuly` | Model `DmNhiemvuChitietV2Model` **không có trường này** ⇒ điều kiện `!== 11` luôn đúng ⇒ badge "Hết hạn" hiện cả khi đã nghiệm thu |
| `isCapCuoi: createby == 'TRUONG_PHONG'` (phan-hoi-nhiemvu:158) | So **username** với **mã chức vụ** ⇒ luôn false |
| `save()` vs `saveSign()` xử lý REFUSE khác nhau | `save()` vẫn ép `trangthaiXuly = 10` khi từ chối; `saveSign()` thì loại trừ ⇒ bấm "Lưu" để từ chối lại đẩy nhiệm vụ về "chờ xác nhận" |
| `TRANGTHAI_NHIEMVU_FILTER` (constants.ts:261) | Khai `'Phân công' = 0` trong khi lưới hiển thị `'Phân công' = 1` — mâu thuẫn; và là **mã chết** (khối HTML dùng nó đã bị comment ở cả 2 màn) |
| `STATUS_REPORT`, `STATUS`, `Status`, `STATUS_REPORT_NT`, `TRANGTHAIBAOCAO` | **0 nơi import**. Riêng `TRANGTHAIBAOCAO` chứa `'Chờ xác nhận' = 2` gây nhầm với `trangthaiDvXuly = 10` |
| `dialogRef.close(dialogRef.close({success:true}))` | Gọi `close()` hai lần lồng nhau |
| `[disabled]="frm.invalid \|\| textError"` | `textError` **không tồn tại** trong component |
| `NhacViecComponent` không có thuộc tính `result` | Mọi caller kiểm `instance.result` đều không vào nhánh reload |
| `WebSocketService` | Gọi `.start()` **2 lần**; `registerOnServerEvents()` **rỗng** ⇒ `getMessages()` không bao giờ phát dữ liệu |
| `mammoth`, `tinymce`, `@angular/material`, `kendo-excel-export`, `echarts` | Cài/nạp nhưng **0 nơi dùng** |
| Mọi bản `*-embed` | Bản sao gần như y hệt, lệch ~10 dòng ⇒ 2 nguồn sự thật |

### 10.11. ⚠️ Rủi ro bảo mật của hệ gốc (nêu để KHÔNG lặp lại)

| Vấn đề | Vị trí |
|---|---|
| Mật khẩu registry + `SONAR_TOKEN` commit thẳng vào repo | `.gitlab-ci.yml:10-14` |
| `secretKey` SSO nằm trong `environment.ts` / `environment.prod.ts` | env.ts:34, prod.ts:28 |
| License key Syncfusion hard-code trong `main.ts` | main.ts:13 |
| License Kendo (JWT) để trong repo, **đã hết hạn 26/12/2024** | `kendo-ui-license.txt` |
| **Không có `canActivate` cho bất kỳ route `/admin/...`** nào | `admin-routing.module.ts:44-83` |
| Interceptor gắn header theo `if (token)` chứ không `if (selectedToken)` | `auth.interceptor.ts:88` |
| `APP_SIGNATURE_URL` = `http://localhost:6543` **kể cả ở production** | `environment.prod.ts:21` |
| Dịch vụ AI gọi ra domain public bên ngoài `https://aiocr.vconnect247.xyz` | env.ts:26, prod.ts:20 |

**Bắt buộc với app mới**: mọi bí mật đọc từ biến môi trường; có `.env.example`; guard cho `/quan-tri/*`; kiểm quyền 2 lớp ở backend cho **mọi** endpoint.

### 10.12. 📋 Danh sách câu hỏi phải hỏi bên nghiệp vụ / backend

1. Dump `DM_TUDIEN` cho `TRANGTHAINV` và `TRANGTHAIPH` — nhãn chính thức là gì?
2. Nhãn của mã `4`, `97`, `100` trong `TRANGTHAINV`?
3. Trạng thái khởi tạo khi tạo nhiệm vụ do BE đặt là gì?
4. Sau khi duyệt gia hạn, `hanxulyth` có tự cập nhật không? `trangthai` từ 13 quay về gì?
5. Quy tắc backend tính `isxuly`, `ischuyentiep`, `isthuhoiphancong`, `canNhacViec`?
6. Quy tắc chấm của `SP_THONGKE_NHIEMVU` — khi nào `2 → 7`?
7. Nghiệp vụ có yêu cầu bắt buộc nhập lý do khi từ chối / trả lại không?
8. Có SLA cho việc tiếp nhận nhiệm vụ không?
9. Ngưỡng "sắp hết hạn" chính thức là bao nhiêu ngày? (FE gốc có 2 con số khác nhau, đều là 3 nhưng 1 chỗ kèm điều kiện `loainv === 'KHONGDK'`)
10. **Có sẵn dữ liệu chuyên môn của cán bộ ở hệ thống nhân sự nào không?** — nếu có thì tiết kiệm được cả tuần 4.


---

## Phụ lục — Nguồn gốc tài liệu

Tài liệu dựng từ mã nguồn `QLNV_FE` bằng cách soát song song 8 hướng: máy trạng thái,
tạo & giao nhiệm vụ, tiếp nhận, cập nhật tiến độ, báo cáo & duyệt kết quả, phân quyền,
mô hình dữ liệu, công nghệ. Mọi kết luận dạng "chức năng X đã có" đều được **kiểm chứng
ngược** — mở lại tệp, truy đường gọi từ thao tác người dùng tới lời gọi API — để loại bỏ
những phần chỉ tồn tại trên giấy.

Một số mã chết đã phát hiện trong quá trình soát và **không** đưa vào đặc tả:

| Chỗ | Vì sao không dùng được |
|---|---|
| `canShowGiaHan()` | Không nơi nào gọi; lại luôn trả `false` do so sánh chuỗi `'"VIECDVGIAO"'` thừa cặp nháy kép |
| `AiCanhbaoNhiemvu/tiep-nhan/{id}` | Khai trong service nhưng không component nào gọi |
| Công tắc "Bật AI tìm nhiệm vụ trùng" | `*ngIf` so `cateForm` với giá trị không bao giờ xảy ra |
| Ô "Tìm theo nội dung file" | Nằm trong khối HTML đã bị chú thích |

**Giới hạn cần biết:** tài liệu chỉ đọc được **frontend**. Quy tắc chấm trạng thái theo
hạn nằm trong stored procedure phía máy chủ (`SP_THONGKE_NHIEMVU`) — không đọc được, nên
mục 2.5 là **đề xuất thiết kế**, không phải mô tả hiện trạng.
