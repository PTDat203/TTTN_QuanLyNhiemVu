--------------------------------------------------------------------------------
--                              !!!  CANH BAO  !!!
--------------------------------------------------------------------------------
-- 99_xoa_het.sql
--
-- TEP NAY XOA VINH VIEN CA 7 BANG VA TOAN BO DU LIEU TRONG SCHEMA TASK_APP.
-- Khong the hoan tac (da dung PURGE nen khong nam trong recyclebin).
--
-- CHI chay tren may phat trien khi muon dung lai tu dau. TUYET DOI khong chay
-- tren moi truong that. Neu chi muon lam moi du lieu demo thi chay lai
-- 03_seed_du_lieu_mau.sql, tep do da tu xoa du lieu cu roi nap lai.
--
-- Chay bang: tai khoan TASK_APP.
--   sqlplus TASK_APP/<mat_khau>@127.0.0.1:1521/XEPDB1 @99_xoa_het.sql
--
-- Thu tu DROP nguoc voi thu tu phu thuoc khoa ngoai:
--   TASK_ATTACHMENTS -> TASK_REPORTS -> TASK_PROGRESS -> TASKS
--   -> USER_SKILLS -> TASK_STATUS_LOOKUP -> USERS
--
-- Bo qua ORA-00942 (table or view does not exist) de chay lai nhieu lan khong loi.
--------------------------------------------------------------------------------

DECLARE
    TYPE t_danh_sach IS TABLE OF VARCHAR2(30);

    -- DUNG THU TU: bang con truoc, bang cha sau
    v_bang t_danh_sach := t_danh_sach(
        'TASK_ATTACHMENTS',    -- FK -> TASKS, TASK_REPORTS, USERS
        'TASK_REPORTS',        -- FK -> TASKS, USERS
        'TASK_PROGRESS',       -- FK -> TASKS, USERS
        'TASKS',               -- FK -> USERS, TASK_STATUS_LOOKUP
        'USER_SKILLS',         -- FK -> USERS
        'TASK_STATUS_LOOKUP',  -- khong con bang nao tham chieu
        'USERS'                -- xoa sau cung
    );
BEGIN
    FOR i IN 1 .. v_bang.COUNT LOOP
        BEGIN
            EXECUTE IMMEDIATE 'DROP TABLE ' || v_bang(i) || ' CASCADE CONSTRAINTS PURGE';
            DBMS_OUTPUT.PUT_LINE('Da xoa bang ' || v_bang(i));
        EXCEPTION
            WHEN OTHERS THEN
                -- -942 : bang khong ton tai, coi nhu da xoa roi
                IF SQLCODE = -942 THEN
                    DBMS_OUTPUT.PUT_LINE('Bang ' || v_bang(i) || ' khong ton tai - bo qua.');
                ELSE
                    RAISE;
                END IF;
        END;
    END LOOP;
END;
/

-- Don sach recyclebin phong truong hop con sot doi tuong cu
PURGE RECYCLEBIN;

-- Kiem chung: truy van duoi day phai tra ve 0 dong
SELECT TABLE_NAME
FROM   USER_TABLES
WHERE  TABLE_NAME IN ('USERS', 'USER_SKILLS', 'TASK_STATUS_LOOKUP',
                      'TASKS', 'TASK_PROGRESS', 'TASK_REPORTS', 'TASK_ATTACHMENTS')
ORDER  BY TABLE_NAME;

--------------------------------------------------------------------------------
-- Muon dung lai tu dau, chay lai theo thu tu:
--   01_tao_bang.sql
--   02_seed_danh_muc.sql
--   03_seed_du_lieu_mau.sql
--------------------------------------------------------------------------------
