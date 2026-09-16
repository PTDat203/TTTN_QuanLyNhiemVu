-- ---------------------------------------------------------------------------
-- 26_don_du_lieu_thu.sql — xoá bộ nhiệm vụ do tao_nhiem_vu_thu.py dựng
--
-- Khác 25 ở chỗ nào: 25 chỉ xoá nhiệm vụ CHƯA có tiến độ hay báo cáo, vì nó dọn
-- rác của bộ kiểm thử phân quyền và phải tuyệt đối không chạm dữ liệu thật.
-- Bộ dựng để thử tay thì cố ý đẩy nhiệm vụ đi hết vòng đời — có tiến độ, báo cáo,
-- điểm duyệt — nên 25 không với tới. Script này xoá cả dữ liệu con, và bù lại phải
-- chặt hơn: chỉ khớp ĐÚNG TỪNG TIÊU ĐỀ trong danh sách cố định dưới đây.
--
-- Danh sách này phải trùng với BO_THU trong Tài liệu/cong_cu_kiem_thu/tao_nhiem_vu_thu.py.
-- Sửa một bên thì sửa cả bên kia.
--
-- Vì sao phải dọn: nhiệm vụ còn mở cộng vào khối lượng việc đang gánh của người
-- nhận, mà khối lượng là một thành phần chấm điểm của mô hình gợi ý; nhiệm vụ đã
-- hoàn thành đạt chất lượng >= 4/5 thì lọt vào tập đánh giá. Để lại là số liệu lệch.
--
-- Cách chạy:
--   set NLS_LANG=.AL32UTF8
--   sqlplus -S TASK_APP/<mat_khau>@localhost:1521/XEPDB1 @26_don_du_lieu_thu.sql
-- ---------------------------------------------------------------------------
SET SERVEROUTPUT ON
SET FEEDBACK OFF

DECLARE
  TYPE t_id IS TABLE OF NUMBER;
  ids t_id;

  so_truoc NUMBER;
  so_tep   NUMBER := 0;
  so_bc    NUMBER := 0;
  so_td    NUMBER := 0;
  so_kn    NUMBER := 0;
  so_nv    NUMBER := 0;
BEGIN
  SELECT COUNT(*) INTO so_truoc FROM TASKS;

  -- Danh sách tiêu đề chỉ viết MỘT lần ở đây; các lệnh xoá phía sau chạy theo ID gom được.
  -- Kiểu collection khai báo trong PL/SQL không dùng được trực tiếp trong câu SQL
  -- (PLS-00642), nên gom ID ra rồi xoá theo lô bằng FORALL.
  SELECT t.ID BULK COLLECT INTO ids
  FROM   TASKS t
  WHERE  t.TITLE IN (
           'Viết tài liệu API cho cổng thanh toán',
           'Thêm bộ nhớ đệm Redis cho truy vấn danh mục',
           'Chuyển tầng truy cập dữ liệu sang truy vấn bất đồng bộ',
           'Bổ sung kiểm thử tự động cho luồng đăng nhập',
           'Tối ưu kích thước gói tải về của trang quản trị',
           'Sửa lỗi sai múi giờ khi hiển thị hạn hoàn thành',
           'Xây dựng dịch vụ gửi thông báo đẩy',
           'Tổ chức chương trình đào tạo hội nhập cho nhân viên mới',
           'Tổ chức buổi tổng kết và khen thưởng cuối năm',
           'Rà soát và cải thiện quy trình nội bộ',
           'Chuẩn bị hồ sơ đánh giá năng lực cuối năm',
           'Viết điểm cuối tra cứu lịch sử giao dịch',
           'Thiết kế lại kiến trúc phân tầng cho hệ thống lõi',
           'Kiểm tra phạm vi giao việc của trưởng nhóm',
           'Khảo sát nhu cầu đào tạo năm 2027')
  ORDER BY t.ID;

  IF ids.COUNT = 0 THEN
    DBMS_OUTPUT.PUT_LINE('Khong co nhiem vu thu nao trong database. Khong xoa gi.');
    RETURN;
  END IF;

  -- Xem truoc tung dong. Khong dung TABLE(ids) vi kieu collection cuc bo khong
  -- goi duoc trong cau SQL (PLS-00642) — doc lai theo tung ID cho chac.
  DBMS_OUTPUT.PUT_LINE('Nhiem vu se xoa:');
  FOR i IN 1 .. ids.COUNT LOOP
    DECLARE
      v_ten VARCHAR2(200);
      v_tt  VARCHAR2(40);
    BEGIN
      SELECT t.TITLE, t.STATUS_CODE INTO v_ten, v_tt FROM TASKS t WHERE t.ID = ids(i);
      DBMS_OUTPUT.PUT_LINE('  #' || ids(i) || '  [' || v_tt || ']  ' || v_ten);
    END;
  END LOOP;

  -- Xoá từ lá vào gốc. Chỉ TASK_REQUIRED_SKILLS có ON DELETE CASCADE, ba bảng còn lại
  -- là NO ACTION nên phải tự dọn, không thì vướng ORA-02292.
  FORALL i IN 1 .. ids.COUNT DELETE FROM TASK_ATTACHMENTS WHERE TASK_ID = ids(i);
  FOR i IN 1 .. ids.COUNT LOOP so_tep := so_tep + SQL%BULK_ROWCOUNT(i); END LOOP;

  FORALL i IN 1 .. ids.COUNT DELETE FROM TASK_REPORTS WHERE TASK_ID = ids(i);
  FOR i IN 1 .. ids.COUNT LOOP so_bc := so_bc + SQL%BULK_ROWCOUNT(i); END LOOP;

  FORALL i IN 1 .. ids.COUNT DELETE FROM TASK_PROGRESS WHERE TASK_ID = ids(i);
  FOR i IN 1 .. ids.COUNT LOOP so_td := so_td + SQL%BULK_ROWCOUNT(i); END LOOP;

  FORALL i IN 1 .. ids.COUNT DELETE FROM TASK_REQUIRED_SKILLS WHERE TASK_ID = ids(i);
  FOR i IN 1 .. ids.COUNT LOOP so_kn := so_kn + SQL%BULK_ROWCOUNT(i); END LOOP;

  FORALL i IN 1 .. ids.COUNT DELETE FROM TASKS WHERE ID = ids(i);
  FOR i IN 1 .. ids.COUNT LOOP so_nv := so_nv + SQL%BULK_ROWCOUNT(i); END LOOP;

  COMMIT;
  DBMS_OUTPUT.PUT_LINE('Da xoa ' || so_nv || ' nhiem vu, ' || so_td || ' dong tien do, ' ||
                       so_bc || ' bao cao, ' || so_tep || ' tep dinh kem, ' ||
                       so_kn || ' ky nang yeu cau.');
  DBMS_OUTPUT.PUT_LINE('Con lai ' || (so_truoc - so_nv) || ' nhiem vu.');
END;
/
EXIT
