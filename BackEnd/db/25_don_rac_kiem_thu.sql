-- ---------------------------------------------------------------------------
-- 25_don_rac_kiem_thu.sql — xoá nhiệm vụ do bộ kiểm thử tự động sinh ra
--
-- Vì sao cần: kiem_thu_phan_quyen.py tạo nhiệm vụ thật để kiểm quy tắc giao việc.
-- Những việc giao thành công thì ở lại DB, và vì chúng đang MỞ nên cộng vào khối
-- lượng việc đang gánh của người nhận. Khối lượng là một thành phần chấm điểm của
-- mô hình gợi ý, nên rác này làm lệch thứ hạng ứng viên và lệch cả số liệu đánh giá.
--
-- Chỉ xoá theo ĐÚNG tiêu đề mà bộ kiểm thử dùng, và chỉ những việc chưa từng có
-- tiến độ hay báo cáo — để không bao giờ chạm vào dữ liệu nghiệp vụ thật.
--
-- Cách chạy:
--   set NLS_LANG=.AL32UTF8
--   sqlplus -S TASK_APP/<mat_khau>@localhost:1521/XEPDB1 @25_don_rac_kiem_thu.sql
-- ---------------------------------------------------------------------------
SET SERVEROUTPUT ON
SET FEEDBACK OFF

DECLARE
  so_truoc   NUMBER;
  so_xoa     NUMBER;
  so_ky_nang NUMBER;
BEGIN
  SELECT COUNT(*) INTO so_truoc FROM TASKS;

  -- Xem trước: liệt kê đúng những gì sắp xoá.
  DBMS_OUTPUT.PUT_LINE('Nhiem vu se xoa:');
  FOR r IN (
    SELECT t.ID, t.TITLE, t.STATUS_CODE, t.ASSIGNEE_ID
    FROM   TASKS t
    WHERE  t.TITLE IN ('Kiểm thử phân quyền',
                       'Lập bảng lương và bảo hiểm tháng 10',
                       'Tối ưu truy vấn báo cáo doanh thu trên Oracle')
      AND  NOT EXISTS (SELECT 1 FROM TASK_PROGRESS p WHERE p.TASK_ID = t.ID)
      AND  NOT EXISTS (SELECT 1 FROM TASK_REPORTS  b WHERE b.TASK_ID = t.ID)
    ORDER BY t.ID
  ) LOOP
    DBMS_OUTPUT.PUT_LINE('  #' || r.ID || '  [' || r.STATUS_CODE || ']  ' || r.TITLE);
  END LOOP;

  DELETE FROM TASK_REQUIRED_SKILLS k
  WHERE  k.TASK_ID IN (
    SELECT t.ID FROM TASKS t
    WHERE  t.TITLE IN ('Kiểm thử phân quyền',
                       'Lập bảng lương và bảo hiểm tháng 10',
                       'Tối ưu truy vấn báo cáo doanh thu trên Oracle')
      AND  NOT EXISTS (SELECT 1 FROM TASK_PROGRESS p WHERE p.TASK_ID = t.ID)
      AND  NOT EXISTS (SELECT 1 FROM TASK_REPORTS  b WHERE b.TASK_ID = t.ID));
  so_ky_nang := SQL%ROWCOUNT;

  DELETE FROM TASKS t
  WHERE  t.TITLE IN ('Kiểm thử phân quyền',
                     'Lập bảng lương và bảo hiểm tháng 10',
                     'Tối ưu truy vấn báo cáo doanh thu trên Oracle')
    AND  NOT EXISTS (SELECT 1 FROM TASK_PROGRESS p WHERE p.TASK_ID = t.ID)
    AND  NOT EXISTS (SELECT 1 FROM TASK_REPORTS  b WHERE b.TASK_ID = t.ID);
  so_xoa := SQL%ROWCOUNT;

  COMMIT;
  DBMS_OUTPUT.PUT_LINE('Da xoa ' || so_xoa || ' nhiem vu va ' || so_ky_nang ||
                       ' dong ky nang yeu cau. Con lai ' || (so_truoc - so_xoa) || ' nhiem vu.');
END;
/
EXIT
