import { Component, EventEmitter, Input, Output } from '@angular/core';

/** Trang thai rong dung chung cho moi man danh sach. */
@Component({
  selector: 'qlnv-empty-state',
  templateUrl: './empty-state.component.html',
  styleUrls: ['./empty-state.component.scss']
})
export class EmptyStateComponent {
  /** Lop bieu tuong FontAwesome 6, vi du 'fa-inbox'. */
  @Input() icon = 'fa-inbox';

  /** Dong tieu de (tieng Viet co dau). */
  @Input() tieuDe = 'Chưa có dữ liệu';

  /** Dong mo ta phu. */
  @Input() moTa: string | null = null;

  /** Nhan nut hanh dong. Bo trong = khong hien nut. */
  @Input() nhanNut: string | null = null;

  /** Nguoi dung bam nut hanh dong. */
  @Output() bamNut = new EventEmitter<void>();
}
