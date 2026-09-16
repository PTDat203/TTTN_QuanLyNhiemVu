# Thư mục `kiem_thu/`

Công cụ kiểm thử phần AI gợi ý người thực hiện. Tất cả gọi **API thật** đang chạy, không mô
phỏng — vì phần lớn lỗi từng gặp nằm ở chỗ ghép nối (phạm vi tổ chức, dữ liệu trong Oracle,
dịch vụ nhúng Python), không nằm trong một hàm đơn lẻ.

Trước đây các script này nằm trong `Tài liệu/`, mà thư mục đó bị `.gitignore` bỏ qua — clone
repo về là không có bài kiểm thử nào. Chuyển vào đây để chúng đi cùng mã nguồn.

## Chuẩn bị

```bash
# 1. Dịch vụ nhúng
cd BackEnd/ai && .venv/Scripts/python -m uvicorn app:app --port 8000

# 2. Backend
cd BackEnd/src/TaskApp.Api && dotnet run --urls http://localhost:5099
```

Đổi địa chỉ hoặc mật khẩu bằng biến môi trường:

```bash
export KIEM_THU_GOC=http://localhost:5099
export KIEM_THU_MAT_KHAU=123456
```

## Các script

| Tệp | Làm gì | Ghi database? |
|---|---|---|
| `chung.py` | Tiện ích dùng chung: gọi API, đăng nhập, chờ backend | — |
| `chay_ca_vang.py` | Đối chiếu gợi ý với **đáp án do người đặt** trong `bo_ca_vang.json` | **Không** |
| `hieu_chinh.py` | Gọi ba endpoint đánh giá, lưu JSON thô | **Không** |
| `kiem_thu_phan_quyen.py` | 42 bài kiểm phân quyền và luồng nghiệp vụ | **CÓ** |
| `tao_nhiem_vu_thu.py` | Dựng 15 nhiệm vụ demo để thử tay trên giao diện | **CÓ** |

## Chạy cái gì, khi nào

**Sau mỗi lần sửa trọng số, ngưỡng, hoặc mô tả phòng ban:**

```bash
python chay_ca_vang.py      # nhanh, không ghi gì, bắt hồi quy ngay
python hieu_chinh.py chay   # chậm hơn, cho Top-1 / Top-3 / MRR
```

**Trước khi bảo vệ:** chạy cả hai, kèm `kiem_thu_phan_quyen.py`.

**Sau mỗi lần chạy script CÓ ghi database** — bắt buộc dọn:

```bash
export NLS_LANG=.AL32UTF8
sqlplus -S TASK_APP/<mat_khau>@localhost:1521/XEPDB1 @../db/25_don_rac_kiem_thu.sql   # sau kiem_thu_phan_quyen
sqlplus -S TASK_APP/<mat_khau>@localhost:1521/XEPDB1 @../db/26_don_du_lieu_thu.sql    # sau tao_nhiem_vu_thu
```

Không dọn thì nhiệm vụ rác nằm lại ở trạng thái **mở** và cộng vào khối lượng việc đang gánh
của người nhận — mà khối lượng là một thành phần chấm điểm. Chuyện này đã xảy ra thật: 21 việc
rác tích lại cộng khống 5 việc cho ba người, suýt làm chọn sai bộ tham số thâm niên.

## Bộ ca vàng

`bo_ca_vang.json` là **đáp án do người đặt**, không suy ra từ lịch sử. Đây là phần bổ khuyết
cho `hieu_chinh.py chay`: bộ đánh giá tự động chỉ phủ những việc đã từng xảy ra, còn bộ ca vàng
phủ được việc đời thường chưa có trong dữ liệu mẫu — đúng chỗ mô hình hay hỏng mà phép đo tự
động không thấy.

**Gặp một lần gợi ý sai thì thêm ngay một ca vào đây.** Đó là cách bộ này lớn lên, và là cách
duy nhất để lần sửa sau không làm hỏng lại chỗ vừa sửa.

Ba điều cần biết khi đọc kết quả:

1. **Kết quả phụ thuộc trạng thái database.** Tầng đoán phòng đọc lịch sử nhiệm vụ, nên thêm
   bớt dữ liệu là điểm đổi theo. Script ghi lại số nhiệm vụ trong database mỗi lần chạy và báo
   khi con số đó đổi, để phân biệt "hỏng do sửa mã" với "đổi do dữ liệu".

2. **Ca sát ngưỡng thì mong manh.** Ví dụ `mo-ho` được quyết định bởi cách biệt 0,147 so với
   ngưỡng 0,140 — hơn đúng 0,007, nên lật qua lật lại mỗi khi lịch sử nhúc nhích. Script in kèm
   cách biệt để nhận ra ngay loại ca này.

3. **`bietTruocLech: true`** đánh dấu ca đã biết là đang sai và chưa sửa. Script báo riêng,
   không tính là hồi quy, và reo lên khi ca đó tự nhiên đạt (tức vừa được sửa).

Mã thoát khác 0 khi có ca **lệch ngoài dự kiến** — ca đã biết lệch thì không làm hỏng build.

## Không được làm

**Đừng đánh dấu nhiệm vụ thử bằng tiền tố trong tiêu đề.** Đã thử `"[THỬ] "` và chính mấy ký tự
đó làm lệch kết quả AI: cùng một nội dung, có tiền tố thì "Rà soát và cải thiện quy trình nội
bộ" nhảy từ LƯỠNG LỰ sang CHẮC CHẮN, vì tiêu đề là đầu vào của mô hình nhúng. Nhận diện bằng
danh sách tiêu đề, đừng chèn gì vào văn bản.

**Đừng gọi gợi ý bằng `taskId` trong bộ ca vàng.** Dùng `title`/`description`. Lý do: kỹ năng
AI trích lúc tạo nhiệm vụ được lưu xuống `TASK_REQUIRED_SKILLS` với `Source = AI`, và lần gợi ý
sau theo `taskId` sẽ đọc lại chính nó thay vì trích lại — tức đo cái AI đã tự ghi cho mình, chứ
không đo năng lực mô hình.
