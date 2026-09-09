--------------------------------------------------------------------------------
-- 02_seed_danh_muc.sql
-- Muc dich : Nap 6 dong danh muc trang thai nhiem vu vao TASK_STATUS_LOOKUP.
-- Chay bang: tai khoan TASK_APP, sau khi da chay 01_tao_bang.sql.
--
-- Dung MERGE thay vi INSERT de chay lai nhieu lan khong bao ORA-00001 (trung khoa chinh).
-- MERGE con giup sua lai NAME/DESCRIPTION/SORT_ORDER neu doi cach hien thi.
--
-- ENCODING: tep luu o UTF-8. SQL*Plus tren Windows can  set NLS_LANG=.AL32UTF8
--           truoc khi chay, neu khong tieng Viet se thanh dau hoi.
--
-- Vong doi trang thai (muc 7 cua DATABASE_SOURCE_OF_TRUTH):
--   MOI_TAO -> DA_GIAO -> DANG_THUC_HIEN -> CHO_XAC_NHAN -> HOAN_THANH
--   nhanh chua dat: CHO_XAC_NHAN -> YEU_CAU_BO_SUNG -> DANG_THUC_HIEN
--------------------------------------------------------------------------------

MERGE INTO TASK_STATUS_LOOKUP t
USING (
        SELECT 'MOI_TAO'         AS CODE,
               'Mới tạo'         AS NAME,
               'Nhiệm vụ vừa được khởi tạo, chưa phân công người thực hiện' AS DESCRIPTION,
               1                  AS SORT_ORDER FROM DUAL
  UNION ALL SELECT 'DA_GIAO',
               'Đã giao',
               'Đã phân công người thực hiện, chờ người thực hiện tiếp nhận',
               2 FROM DUAL
  UNION ALL SELECT 'DANG_THUC_HIEN',
               'Đang thực hiện',
               'Người thực hiện đã tiếp nhận và đang xử lý nhiệm vụ',
               3 FROM DUAL
  UNION ALL SELECT 'CHO_XAC_NHAN',
               'Chờ xác nhận',
               'Đã gửi báo cáo kết quả và chờ người giao kiểm tra',
               4 FROM DUAL
  UNION ALL SELECT 'YEU_CAU_BO_SUNG',
               'Yêu cầu bổ sung',
               'Kết quả chưa đạt, người thực hiện cần chỉnh sửa và báo cáo lại',
               5 FROM DUAL
  UNION ALL SELECT 'HOAN_THANH',
               'Hoàn thành',
               'Nhiệm vụ đã được người giao xác nhận hoàn thành',
               6 FROM DUAL
) s
ON (t.CODE = s.CODE)
WHEN MATCHED THEN
    UPDATE SET t.NAME        = s.NAME,
               t.DESCRIPTION = s.DESCRIPTION,
               t.SORT_ORDER  = s.SORT_ORDER
WHEN NOT MATCHED THEN
    INSERT (CODE, NAME, DESCRIPTION, SORT_ORDER)
    VALUES (s.CODE, s.NAME, s.DESCRIPTION, s.SORT_ORDER);

COMMIT;

-- Kiem chung: phai ra dung 6 dong, SORT_ORDER 1..6
SELECT CODE, NAME, SORT_ORDER
FROM   TASK_STATUS_LOOKUP
ORDER  BY SORT_ORDER;
