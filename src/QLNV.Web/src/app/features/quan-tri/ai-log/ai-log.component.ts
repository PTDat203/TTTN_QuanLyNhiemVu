import { Component } from '@angular/core';

/**
 * M13 - Nhat ky goi y AI
 *
 * Nhat ky goi y, ty le chap nhan top-1 / top-3, Precision@1/@3, MRR, Gini, so sanh baseline, tinh chinh trong so w1..w5. Dung H3, H4 va GET /ai/nhat-ky, /ai/so-sanh-baseline.
 *
 * TRANG THAI: KHUNG RONG - tac tu phu trach man hinh nay se thay toan bo
 * noi dung lop va template. GIU NGUYEN ten lop, selector va duong dan tep
 * (router va cac man khac da tro toi chung).
 */
@Component({
  selector: 'qlnv-ai-log',
  templateUrl: './ai-log.component.html',
  styleUrls: ['./ai-log.component.scss']
})
export class AiLogComponent {}
