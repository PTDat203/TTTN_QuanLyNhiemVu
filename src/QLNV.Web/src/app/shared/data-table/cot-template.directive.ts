import { Directive, Input, TemplateRef } from '@angular/core';

/**
 * Danh dau mot `ng-template` la o tuy bien cho MOT cot cua `qlnv-data-table`.
 *
 * Ngu canh truyen vao template:
 *  - `$implicit` : ban ghi cua dong (`let-dong`)
 *  - `giaTri`    : gia tri tho cua o        (`let-gt="giaTri"`)
 *  - `chiSo`     : chi so dong trong trang  (`let-i="chiSo"`)
 *  - `cot`       : dinh nghia cot           (`let-c="cot"`)
 *
 * @example
 * ```html
 * <qlnv-data-table [cot]="cot" [duLieu]="ds">
 *   <ng-template qlnvCot="trangthai" let-dong>
 *     <qlnv-trang-thai-chip [trangthai]="dong.trangthai"
 *                           [trangthaiDvXuly]="dong.trangthaiDvXuly">
 *     </qlnv-trang-thai-chip>
 *   </ng-template>
 * </qlnv-data-table>
 * ```
 */
@Directive({ selector: '[qlnvCot]' })
export class CotTemplateDirective {
  /** Khoa cot ma template nay phu trach (trung `CotBang.khoa`). */
  @Input('qlnvCot') khoa = '';

  constructor(public readonly template: TemplateRef<unknown>) {}
}
