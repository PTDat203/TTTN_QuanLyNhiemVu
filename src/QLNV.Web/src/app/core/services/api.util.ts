import { HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

/**
 * Ghep duong dan day du cua mot endpoint §5.
 *
 * @example duongDan('/nhiem-vu/123/tiep-nhan') -> '/api/v1/nhiem-vu/123/tiep-nhan'
 */
export function duongDan(duoi: string): string {
  const d = duoi.startsWith('/') ? duoi : `/${duoi}`;
  return `${environment.apiUrl}${environment.apiPrefix}${d}`;
}

/**
 * Dung `HttpParams` tu mot doi tuong bat ky.
 *
 * Quy tac:
 *  - Bo qua `null` / `undefined` / chuoi rong (de BE dung gia tri mac dinh).
 *  - Mang duoc gui bang cach LAP khoa: `?trangThai=2&trangThai=7`
 *    (dung cach ASP.NET Core rang buoc `List<int>`).
 *  - `boolean` gui thanh 'true' / 'false'.
 *
 * CANH BAO: ten khoa phai la TEN THUOC TINH C# (khong phai [JsonPropertyName]),
 * vi rang buoc query cua ASP.NET khong doc [JsonPropertyName].
 */
export function thamSo(nguon: Record<string, unknown> | null | undefined): HttpParams {
  let p = new HttpParams();
  if (!nguon) return p;

  for (const khoa of Object.keys(nguon)) {
    const gt = nguon[khoa];
    if (gt === null || gt === undefined) continue;

    if (Array.isArray(gt)) {
      for (const phanTu of gt) {
        if (phanTu === null || phanTu === undefined || phanTu === '') continue;
        p = p.append(khoa, String(phanTu));
      }
      continue;
    }

    if (typeof gt === 'string' && gt.trim() === '') continue;
    p = p.set(khoa, String(gt));
  }

  return p;
}

/** Khoa luu tru phien o localStorage. */
export const KHOA_LUU_TRU = {
  accessToken: 'qlnv.accessToken',
  refreshToken: 'qlnv.refreshToken',
  nguoiDung: 'qlnv.nguoiDung'
} as const;
