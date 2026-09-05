/**
 * Cau hinh moi truong PHAT TRIEN.
 *
 * §10.11 - TUYET DOI KHONG dat secret / client-secret / khoa ky JWT o day.
 * He goc de secret SSO trong environment.ts; day la lo hong khong duoc lap lai.
 * Tep nay chi chua duong dan API va cac co bat/tat tinh nang.
 */
export const environment = {
  production: false,

  /** Duong dan goc cua API. De rong => di qua proxy.conf.json (/api -> localhost:5080). */
  apiUrl: '',

  /** Prefix phien ban API theo §5. */
  apiPrefix: '/api/v1',

  /** So ngay coi la "sap het han" (§2.5 / M08). */
  nguongSapHetHan: 3,

  /** Kich thuoc trang mac dinh cua luoi du lieu. */
  kichThuocTrangMacDinh: 20
};
