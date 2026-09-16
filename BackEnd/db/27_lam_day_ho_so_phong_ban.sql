-- ---------------------------------------------------------------------------
-- 27_lam_day_ho_so_phong_ban.sql — viết lại DEPARTMENTS.DESCRIPTION cho đủ việc
--
-- Vì sao cần
-- ----------
-- Mô tả phòng ban là đầu vào DUY NHẤT mang tính khai báo của tầng 1 (AI đoán nhiệm
-- vụ thuộc phòng nào). Bản cũ mỗi phòng một dòng, và chỉ nói phần chuyên môn lõi:
--   Hành chính - Kế toán: "Văn thư, tài sản, thu chi và báo cáo tài chính"
-- Không có chữ nào dính tới hoá đơn điện nước. Hậu quả đo được trên mô hình nhúng
-- (cosin thô, kiểu bất đối xứng; ngưỡng chuẩn hoá Thấp 0,8163 – Cao 0,861):
--
--   "Xử lý tiền điện tháng 8"        → cả ba phòng đều dưới ngưỡng, ra KHÔNG RÕ
--   "Cài lại Windows cho máy công ty" → Nhân sự 0,77 > Phát triển 0,71  (SAI hẳn)
--
-- Không sửa bằng cách hạ ngưỡng: hạ xuống thì ca "cài Windows" sẽ tự tin giao cho
-- Nhân sự, tệ hơn hiện tại. Gốc rễ là hồ sơ quá mỏng, nên mô hình không có gì để
-- khớp. Chữa đúng chỗ là làm dày hồ sơ.
--
-- Nguyên tắc viết
-- ---------------
-- Thêm phần việc NGOÀI LỀ mà phòng vẫn đảm nhận trên thực tế, không chỉ chuyên môn
-- lõi: công ty nhỏ, việc hành chính hằng ngày vẫn phải có người nhận. Dùng từ ngữ
-- người dùng thật sẽ gõ ("tiền điện", "cài Windows", "máy in", "wifi"), vì đó mới
-- là thứ đem so với nội dung nhiệm vụ.
--
-- Giữ mỗi mô tả trong khoảng 300–450 ký tự. Văn bản hồ sơ cuối cùng còn ghép thêm
-- tên nhóm, mô tả nhóm, kỹ năng tiêu biểu và chuyên ngành (VanBanHoSo.PhongBan),
-- mà multilingual-e5-small chỉ nhận 512 token — viết dài quá thì phần đuôi bị cắt.
--
-- Mô tả NHÓM (TEAMS.DESCRIPTION) đã đủ dày, script này không đụng tới.
--
-- Cách chạy:
--   set NLS_LANG=.AL32UTF8
--   sqlplus -S TASK_APP/<mat_khau>@localhost:1521/XEPDB1 @27_lam_day_ho_so_phong_ban.sql
--
-- Chạy lại được: chỉ UPDATE theo CODE, không đụng dữ liệu khác.
-- ---------------------------------------------------------------------------
SET SERVEROUTPUT ON
SET FEEDBACK OFF
SET DEFINE OFF

UPDATE DEPARTMENTS SET DESCRIPTION =
'Điều hành chung toàn công ty. Định hướng chiến lược, phê duyệt kế hoạch và ngân sách, '
|| 'quyết định đầu tư, đánh giá kết quả các phòng ban.'
WHERE CODE = 'BAN_GIAM_DOC';

UPDATE DEPARTMENTS SET DESCRIPTION =
'Phân tích, lập trình, kiểm thử và vận hành các sản phẩm phần mềm. '
|| 'Hỗ trợ kỹ thuật nội bộ: cài đặt và sửa máy tính, cài hệ điều hành Windows, cài phần mềm '
|| 'văn phòng, xử lý sự cố máy in, máy chiếu, mạng LAN và wifi, tài khoản email, sao lưu và '
|| 'phục hồi dữ liệu. Quản trị máy chủ, cơ sở dữ liệu, tên miền và an toàn thông tin.'
WHERE CODE = 'PHAT_TRIEN';

UPDATE DEPARTMENTS SET DESCRIPTION =
'Tuyển dụng, đào tạo, chế độ chính sách và quản lý hồ sơ nhân sự. '
|| 'Hợp đồng lao động, chấm công, nghỉ phép, bảo hiểm xã hội và bảo hiểm y tế. '
|| 'Đánh giá năng lực, khen thưởng, kỷ luật, xây dựng khung năng lực và lộ trình thăng tiến. '
|| 'Văn hoá công ty, gắn kết nhân viên, khảo sát mức độ hài lòng, tổ chức đào tạo hội nhập.'
WHERE CODE = 'NHAN_SU';

UPDATE DEPARTMENTS SET DESCRIPTION =
'Văn thư, tài sản, thu chi và báo cáo tài chính. '
|| 'Thanh toán hoá đơn tiền điện, tiền nước, internet, cước viễn thông và các dịch vụ thuê ngoài. '
|| 'Mua sắm, cấp phát và kiểm kê văn phòng phẩm, bàn ghế, thiết bị. Quản lý kho, hợp đồng nhà '
|| 'cung cấp, bảo hiểm tài sản. Tạm ứng, công tác phí, lương thưởng, hoá đơn và quyết toán thuế. '
|| 'Lễ tân, hậu cần, đặt vé, phòng họp và tổ chức sự kiện nội bộ.'
WHERE CODE = 'HANH_CHINH';

COMMIT;

DECLARE
  so_thieu NUMBER;
BEGIN
  SELECT COUNT(*) INTO so_thieu FROM DEPARTMENTS WHERE LENGTH(DESCRIPTION) < 100;
  IF so_thieu > 0 THEN
    DBMS_OUTPUT.PUT_LINE('CANH BAO: con ' || so_thieu ||
                         ' phong co mo ta ngan bat thuong — kiem tra lai CODE trong script.');
  END IF;

  FOR r IN (SELECT CODE, NAME, LENGTH(DESCRIPTION) AS n FROM DEPARTMENTS ORDER BY ID) LOOP
    DBMS_OUTPUT.PUT_LINE(RPAD(r.CODE, 6) || RPAD(r.NAME, 30) || r.n || ' ky tu');
  END LOOP;
  DBMS_OUTPUT.PUT_LINE('Da cap nhat mo ta phong ban. '
                    || 'Khoi dong lai backend de nap lai vec to nhung.');
END;
/
EXIT
