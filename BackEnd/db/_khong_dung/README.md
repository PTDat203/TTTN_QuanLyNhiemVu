# KHÔNG SỬ DỤNG CÁC SCRIPT TRONG THƯ MỤC NÀY

Các script trong thư mục này là phiên bản cũ hoặc script phá hủy dữ liệu.

**Không được sử dụng để tạo lại database hiện tại.**

## Database chính thức đang dùng

- Oracle Database 21c XE
- Service: `XEPDB1`
- Schema: `TASK_APP`

Schema chính thức gồm 7 bảng:

- `USERS`
- `USER_SKILLS`
- `TASK_STATUS_LOOKUP`
- `TASKS`
- `TASK_PROGRESS`
- `TASK_REPORTS`
- `TASK_ATTACHMENTS`

Schema này đã được tạo thủ công và là **nguồn chuẩn duy nhất**. Code phải sửa theo database,
không sửa database theo code.

Không chạy các script DROP hoặc CREATE schema cũ nếu chưa có yêu cầu rõ ràng.

## Vì sao từng tệp bị cách ly

| Tệp | Lý do |
|---|---|
| `00_tao_user.sql` | User `TASK_APP` đã tồn tại, đã có quota `UNLIMITED` trên tablespace `USERS`. Chạy lại sẽ báo `ORA-01920: user name conflicts`. |
| `01_tao_bang.sql` | 7 bảng đã tồn tại. Chạy lại báo `ORA-00955`. Nghiêm trọng hơn: script dùng tên cột **bản cũ** (`ROLE`, `STATUS`) trong khi schema thật là `USER_ROLE`, `USER_STATUS`, `REPORT_STATUS`. |
| `03_seed_du_lieu_mau.sql` | Dữ liệu mẫu, nhưng viết theo tên cột **bản cũ**: 10 chỗ dùng `ROLE`, 47 chỗ dùng `STATUS`. Chạy sẽ chết `ORA-00904: invalid identifier`. Chưa sửa vì `STATUS` ở đây ứng với **ba cột khác nhau** (`USER_STATUS`, `STATUS_CODE`, `REPORT_STATUS`) — thay thế hàng loạt sẽ sai. |
| `99_xoa_het.sql` | **DROP toàn bộ 7 bảng.** Mục 17 của `CLAUDE_GUIDE_ORACLE_TASK_APP.md` cấm DROP. |

## Giữ lại để làm gì

Không xóa hẳn, chỉ cách ly, để:

- còn đối chiếu được lịch sử thiết kế;
- `03_seed_du_lieu_mau.sql` còn tái sử dụng được sau khi sửa lại tên cột cho đúng schema thật.
