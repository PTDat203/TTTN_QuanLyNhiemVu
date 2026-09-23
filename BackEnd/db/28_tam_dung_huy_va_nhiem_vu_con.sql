-- ---------------------------------------------------------------------------
-- 28_tam_dung_huy_va_nhiem_vu_con.sql
--
-- Thêm ba năng lực cho phân hệ:
--   1. TẠM DỪNG nhiệm vụ kèm lý do, mở lại được về đúng trạng thái đang dở
--   2. HUỶ nhiệm vụ kèm lý do, dừng hẳn không mở lại
--   3. Quan hệ cha–con giữa các nhiệm vụ, để dừng một nhiệm vụ thì biết ngay
--      những nhiệm vụ nào ở dưới đang chịu ảnh hưởng
--
-- Vì sao cần quan hệ cha–con
-- --------------------------
-- Hiện chỉ NGƯỜI TẠO mới giao được việc (NhiemVuService.GiaoAsync). Nên khi
-- giám đốc giao cho trưởng phòng, trưởng phòng muốn giao tiếp xuống nhân viên
-- thì phải TẠO MỘT NHIỆM VỤ MỚI — và hai nhiệm vụ đó hoàn toàn rời rạc.
-- Giám đốc dừng nhiệm vụ của mình thì hệ thống không biết còn việc nào ở dưới.
-- Cột PARENT_TASK_ID nối chúng lại.
--
-- Cách chạy:
--   set NLS_LANG=.AL32UTF8
--   sqlplus -S TASK_APP/<mat_khau>@localhost:1521/XEPDB1 @28_tam_dung_huy_va_nhiem_vu_con.sql
--
-- CHỈ MỞ RỘNG: không xoá bảng, không xoá cột, không sửa dữ liệu đang có.
-- Chạy lại được: mỗi bước đều kiểm tra tồn tại trước khi thêm.
-- ---------------------------------------------------------------------------
SET SERVEROUTPUT ON
SET FEEDBACK OFF
SET DEFINE OFF

DECLARE
  -- Chạy một câu DDL, bỏ qua nếu thứ đó đã tồn tại. Nhờ vậy script chạy lại
  -- được mà không báo lỗi giữa chừng.
  PROCEDURE chay(cau VARCHAR2) IS
  BEGIN
    EXECUTE IMMEDIATE cau;
    DBMS_OUTPUT.PUT_LINE('  OK   ' || SUBSTR(cau, 1, 92));
  EXCEPTION WHEN OTHERS THEN
    -- ORA-01430 cột đã có · ORA-02275/02264 ràng buộc đã có · ORA-00955 tên đã dùng
    IF SQLCODE IN (-1430, -2275, -2264, -955) THEN
      DBMS_OUTPUT.PUT_LINE('  bỏ   ' || SUBSTR(cau, 1, 92) || '  (đã có)');
    ELSE
      DBMS_OUTPUT.PUT_LINE('  LỖI  ' || SQLERRM);
      DBMS_OUTPUT.PUT_LINE('       ' || SUBSTR(cau, 1, 200));
      RAISE;
    END IF;
  END;
BEGIN
  ----------------------------------------------------------------------------
  -- 1. Hai trạng thái mới trong danh mục
  --
  -- TASKS.STATUS_CODE trỏ vào bảng này bằng khoá ngoại, nên phải thêm vào đây
  -- TRƯỚC khi ứng dụng ghi được hai trạng thái mới.
  ----------------------------------------------------------------------------
  DBMS_OUTPUT.PUT_LINE('1. Danh mục trạng thái');

  MERGE INTO TASK_STATUS_LOOKUP t
  USING (SELECT 'TAM_DUNG' code,
                'Tạm dừng' name,
                'Người tạo tạm dừng nhiệm vụ, có thể mở lại để làm tiếp' mota,
                7 thu_tu FROM DUAL) s
  ON (t.CODE = s.code)
  WHEN NOT MATCHED THEN
    INSERT (CODE, NAME, DESCRIPTION, SORT_ORDER)
    VALUES (s.code, s.name, s.mota, s.thu_tu);

  MERGE INTO TASK_STATUS_LOOKUP t
  USING (SELECT 'DA_HUY' code,
                'Đã huỷ' name,
                'Nhiệm vụ bị huỷ hẳn, không thực hiện và không mở lại được' mota,
                8 thu_tu FROM DUAL) s
  ON (t.CODE = s.code)
  WHEN NOT MATCHED THEN
    INSERT (CODE, NAME, DESCRIPTION, SORT_ORDER)
    VALUES (s.code, s.name, s.mota, s.thu_tu);

  DBMS_OUTPUT.PUT_LINE('  OK   thêm TAM_DUNG và DA_HUY vào TASK_STATUS_LOOKUP');

  ----------------------------------------------------------------------------
  -- 2. Cột lưu thông tin dừng / huỷ
  --
  -- STOP_REASON bắt buộc có khi dừng, nhưng KHÔNG đặt NOT NULL ở mức cột: 112
  -- nhiệm vụ đang có đều chưa dừng nên phải để trống được. Ràng buộc "dừng thì
  -- phải có lý do" đặt ở mức bảng, xem bước 4.
  --
  -- PREV_STATUS_CODE giữ trạng thái ngay trước khi dừng, để mở lại thì quay về
  -- đúng chỗ đang dở thay vì phải làm lại từ đầu.
  ----------------------------------------------------------------------------
  DBMS_OUTPUT.PUT_LINE('2. Cột thông tin dừng/huỷ');
  chay('ALTER TABLE TASKS ADD (STOP_REASON      VARCHAR2(500 CHAR))');
  chay('ALTER TABLE TASKS ADD (STOPPED_BY       NUMBER)');
  chay('ALTER TABLE TASKS ADD (STOPPED_AT       TIMESTAMP)');
  chay('ALTER TABLE TASKS ADD (PREV_STATUS_CODE VARCHAR2(30))');

  chay('ALTER TABLE TASKS ADD CONSTRAINT FK_TASKS_STOPPED_BY
        FOREIGN KEY (STOPPED_BY) REFERENCES USERS(ID)');
  chay('ALTER TABLE TASKS ADD CONSTRAINT FK_TASKS_PREV_STATUS
        FOREIGN KEY (PREV_STATUS_CODE) REFERENCES TASK_STATUS_LOOKUP(CODE)');

  ----------------------------------------------------------------------------
  -- 3. Quan hệ cha – con
  --
  -- ON DELETE SET NULL chứ không CASCADE: xoá nhiệm vụ cha thì nhiệm vụ con
  -- vẫn còn, chỉ mất liên kết. Xoá theo dây chuyền sẽ cuốn mất cả lịch sử làm
  -- việc của người ở dưới — mà lịch sử đó là dữ liệu nuôi phần AI gợi ý.
  ----------------------------------------------------------------------------
  DBMS_OUTPUT.PUT_LINE('3. Quan hệ cha–con');
  chay('ALTER TABLE TASKS ADD (PARENT_TASK_ID NUMBER)');
  chay('ALTER TABLE TASKS ADD CONSTRAINT FK_TASKS_PARENT
        FOREIGN KEY (PARENT_TASK_ID) REFERENCES TASKS(ID) ON DELETE SET NULL');

  -- Không tự trỏ vào chính mình. Không chặn được vòng lặp dài (A→B→A) bằng
  -- ràng buộc, phần đó tầng ứng dụng kiểm.
  chay('ALTER TABLE TASKS ADD CONSTRAINT CK_TASKS_PARENT_KHAC_MINH
        CHECK (PARENT_TASK_ID IS NULL OR PARENT_TASK_ID <> ID)');

  -- Truy vấn "các nhiệm vụ con của nhiệm vụ này" chạy mỗi lần mở màn chi tiết
  -- và mỗi lần dừng nhiệm vụ, nên cần chỉ mục.
  chay('CREATE INDEX IDX_TASKS_PARENT ON TASKS (PARENT_TASK_ID)');
  chay('CREATE INDEX IDX_TASKS_STOPPED_BY ON TASKS (STOPPED_BY)');

  ----------------------------------------------------------------------------
  -- 4. Ràng buộc toàn vẹn cho việc dừng/huỷ
  --
  -- Hai trạng thái dừng thì bắt buộc có đủ lý do, người dừng và thời điểm.
  -- Đặt ở mức bảng để dù ai ghi thẳng bằng SQL cũng không tạo ra được một
  -- nhiệm vụ "đã huỷ mà không ai biết vì sao".
  ----------------------------------------------------------------------------
  DBMS_OUTPUT.PUT_LINE('4. Ràng buộc toàn vẹn');
  chay('ALTER TABLE TASKS ADD CONSTRAINT CK_TASKS_LY_DO_DUNG
        CHECK (
          (STATUS_CODE NOT IN (''TAM_DUNG'', ''DA_HUY''))
          OR (STOP_REASON IS NOT NULL AND STOPPED_BY IS NOT NULL AND STOPPED_AT IS NOT NULL)
        )');

  -- Chỉ TAM_DUNG mới cần nhớ trạng thái cũ. DA_HUY là dừng hẳn nên không mở lại.
  chay('ALTER TABLE TASKS ADD CONSTRAINT CK_TASKS_PREV_STATUS
        CHECK (PREV_STATUS_CODE IS NULL OR STATUS_CODE = ''TAM_DUNG'')');

  COMMIT;
  DBMS_OUTPUT.PUT_LINE('Xong bước thay đổi lược đồ.');
END;
/

-- ---------------------------------------------------------------------------
-- Kiểm chứng
-- ---------------------------------------------------------------------------
SET SERVEROUTPUT ON
DECLARE
  so_cot    NUMBER;
  so_tt     NUMBER;
  so_rb     NUMBER;
  so_index  NUMBER;
BEGIN
  SELECT COUNT(*) INTO so_cot FROM USER_TAB_COLUMNS
  WHERE TABLE_NAME = 'TASKS'
    AND COLUMN_NAME IN ('STOP_REASON','STOPPED_BY','STOPPED_AT','PREV_STATUS_CODE','PARENT_TASK_ID');

  SELECT COUNT(*) INTO so_tt FROM TASK_STATUS_LOOKUP
  WHERE CODE IN ('TAM_DUNG','DA_HUY');

  SELECT COUNT(*) INTO so_rb FROM USER_CONSTRAINTS
  WHERE TABLE_NAME = 'TASKS'
    AND CONSTRAINT_NAME IN ('FK_TASKS_PARENT','FK_TASKS_STOPPED_BY','FK_TASKS_PREV_STATUS',
                            'CK_TASKS_LY_DO_DUNG','CK_TASKS_PREV_STATUS','CK_TASKS_PARENT_KHAC_MINH');

  SELECT COUNT(*) INTO so_index FROM USER_INDEXES
  WHERE INDEX_NAME IN ('IDX_TASKS_PARENT','IDX_TASKS_STOPPED_BY');

  DBMS_OUTPUT.PUT_LINE('---------------------------------------------');
  DBMS_OUTPUT.PUT_LINE('Cột mới        : ' || so_cot  || '/5');
  DBMS_OUTPUT.PUT_LINE('Trạng thái mới : ' || so_tt   || '/2');
  DBMS_OUTPUT.PUT_LINE('Ràng buộc      : ' || so_rb   || '/6');
  DBMS_OUTPUT.PUT_LINE('Chỉ mục        : ' || so_index|| '/2');

  IF so_cot = 5 AND so_tt = 2 AND so_rb = 6 AND so_index = 2 THEN
    DBMS_OUTPUT.PUT_LINE('=> ĐẦY ĐỦ. Khởi động lại backend để nạp ánh xạ mới.');
  ELSE
    DBMS_OUTPUT.PUT_LINE('=> CÒN THIẾU. Xem lại phần thông báo phía trên.');
  END IF;
END;
/
EXIT
