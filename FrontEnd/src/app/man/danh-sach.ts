import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, NhiemVuService } from '../core/api.service';
import {
  DANH_SACH_TRANG_THAI, KetQuaPhanTrang, MAU_TRANG_THAI, MAU_UU_TIEN,
  NhiemVuLoc, NhiemVuTomTat,
} from '../core/models';

@Component({
  selector: 'app-danh-sach',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe],
  template: `
    <div class="dau-trang">
      <div>
        <h1>Nhiệm vụ</h1>
        <p class="phu">
          {{ auth.laManager() ? 'Nhiệm vụ bạn đã tạo' : 'Nhiệm vụ được giao cho bạn' }}
        </p>
      </div>
      @if (auth.laManager()) {
        <a routerLink="/nhiem-vu/tao" class="nut-chinh">+ Tạo nhiệm vụ</a>
      }
    </div>

    <div class="bo-loc">
      <input
        placeholder="Tìm theo tiêu đề…"
        [(ngModel)]="loc.tuKhoa"
        (keyup.enter)="tai(1)"
      />

      <select [(ngModel)]="loc.statusCode" (change)="tai(1)">
        <option value="">Mọi trạng thái</option>
        @for (t of danhSachTrangThai; track t.ma) {
          <option [value]="t.ma">{{ t.ten }}</option>
        }
      </select>

      <select [(ngModel)]="loc.priority" (change)="tai(1)">
        <option value="">Mọi mức ưu tiên</option>
        <option value="HIGH">Cao</option>
        <option value="MEDIUM">Trung bình</option>
        <option value="LOW">Thấp</option>
      </select>

      <label class="o-tick">
        <input type="checkbox" [(ngModel)]="loc.quaHan" (change)="tai(1)" />
        Chỉ việc quá hạn
      </label>

      <button (click)="tai(1)">Lọc</button>
      <button class="phu-nhat" (click)="xoaLoc()">Xoá lọc</button>
    </div>

    @if (dangTai()) {
      <p class="trong">Đang tải…</p>
    } @else if (!ketQua() || ketQua()!.tongSoDong === 0) {
      <p class="trong">Không có nhiệm vụ nào khớp điều kiện lọc.</p>
    } @else {
      <table>
        <thead>
          <tr>
            <th>Tiêu đề</th>
            <th>Trạng thái</th>
            <th>Ưu tiên</th>
            <th>{{ auth.laManager() ? 'Người thực hiện' : 'Người giao' }}</th>
            <th>Hạn</th>
            <th>Tiến độ</th>
          </tr>
        </thead>
        <tbody>
          @for (nv of ketQua()!.danhSach; track nv.id) {
            <tr (click)="moChiTiet(nv.id)">
              <td class="tieu-de">{{ nv.title }}</td>
              <td>
                <span class="the-trang-thai" [style.background]="mauTrangThai(nv)">
                  {{ nv.tenTrangThai }}
                </span>
              </td>
              <td>
                <span class="cham" [style.background]="mauUuTien(nv)"></span>
                {{ nv.tenUuTien }}
              </td>
              <td>{{ auth.laManager() ? (nv.tenNguoiThucHien ?? '— chưa giao') : nv.tenNguoiTao }}</td>
              <td>
                @if (nv.dueDate) {
                  <span [class.qua-han]="nv.quaHan">
                    {{ nv.dueDate | date: 'dd/MM/yyyy' }}
                    @if (nv.quaHan) {
                      <small>(quá {{ -nv.soNgayConLai! }} ngày)</small>
                    } @else if (nv.soNgayConLai !== null && nv.soNgayConLai! <= 3) {
                      <small>(còn {{ nv.soNgayConLai }} ngày)</small>
                    }
                  </span>
                } @else {
                  <span class="mo">—</span>
                }
              </td>
              <td>
                @if (nv.tienDoPhanTram !== null && nv.tienDoPhanTram !== undefined) {
                  <div class="thanh"><i [style.width.%]="nv.tienDoPhanTram"></i></div>
                  <small>{{ nv.tienDoPhanTram }}%</small>
                } @else {
                  <span class="mo">—</span>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>

      <div class="phan-trang">
        <button [disabled]="!ketQua()!.coTrangTruoc" (click)="tai(ketQua()!.trangHienTai - 1)">
          ← Trước
        </button>
        <span>
          Trang {{ ketQua()!.trangHienTai }} / {{ ketQua()!.tongSoTrang }}
          · {{ ketQua()!.tongSoDong }} nhiệm vụ
        </span>
        <button [disabled]="!ketQua()!.coTrangSau" (click)="tai(ketQua()!.trangHienTai + 1)">
          Sau →
        </button>
      </div>
    }
  `,
  styles: [
    `
      .dau-trang {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        margin-bottom: 18px;
      }
      h1 {
        margin: 0;
        font-size: 22px;
      }
      .phu {
        margin: 4px 0 0;
        color: #64748b;
        font-size: 14px;
      }
      .nut-chinh {
        background: #2563eb;
        color: #fff;
        padding: 9px 16px;
        border-radius: 8px;
        text-decoration: none;
        font-size: 14px;
      }
      .bo-loc {
        display: flex;
        gap: 8px;
        flex-wrap: wrap;
        margin-bottom: 16px;
        align-items: center;
      }
      .bo-loc input[type='text'],
      .bo-loc input:not([type]),
      .bo-loc select {
        padding: 8px 10px;
        border: 1px solid #cbd5e1;
        border-radius: 7px;
        font-size: 14px;
      }
      .bo-loc input:not([type]) {
        min-width: 220px;
      }
      .bo-loc button {
        padding: 8px 14px;
        border: 1px solid #cbd5e1;
        background: #fff;
        border-radius: 7px;
        cursor: pointer;
        font-size: 14px;
      }
      .phu-nhat {
        color: #64748b;
      }
      .o-tick {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 14px;
        color: #334155;
      }
      table {
        width: 100%;
        border-collapse: collapse;
        background: #fff;
        border-radius: 10px;
        overflow: hidden;
        box-shadow: 0 1px 3px rgb(0 0 0 / 8%);
      }
      th {
        text-align: left;
        padding: 11px 14px;
        background: #f8fafc;
        font-size: 13px;
        color: #475569;
        border-bottom: 1px solid #e2e8f0;
      }
      td {
        padding: 12px 14px;
        border-bottom: 1px solid #f1f5f9;
        font-size: 14px;
      }
      tbody tr {
        cursor: pointer;
      }
      tbody tr:hover {
        background: #f8fafc;
      }
      .tieu-de {
        font-weight: 500;
        max-width: 340px;
      }
      .the-trang-thai {
        color: #fff;
        padding: 3px 9px;
        border-radius: 20px;
        font-size: 12px;
        white-space: nowrap;
      }
      .cham {
        display: inline-block;
        width: 8px;
        height: 8px;
        border-radius: 50%;
        margin-right: 6px;
      }
      .qua-han {
        color: #dc2626;
        font-weight: 500;
      }
      .mo {
        color: #94a3b8;
      }
      small {
        color: #64748b;
        margin-left: 4px;
      }
      .thanh {
        width: 70px;
        height: 6px;
        background: #e2e8f0;
        border-radius: 4px;
        overflow: hidden;
        display: inline-block;
        vertical-align: middle;
      }
      .thanh i {
        display: block;
        height: 100%;
        background: #0891b2;
      }
      .phan-trang {
        display: flex;
        justify-content: center;
        align-items: center;
        gap: 16px;
        margin-top: 18px;
        font-size: 14px;
        color: #475569;
      }
      .phan-trang button {
        padding: 7px 14px;
        border: 1px solid #cbd5e1;
        background: #fff;
        border-radius: 7px;
        cursor: pointer;
      }
      .phan-trang button:disabled {
        opacity: 0.4;
        cursor: default;
      }
      .trong {
        text-align: center;
        color: #64748b;
        padding: 40px;
        background: #fff;
        border-radius: 10px;
      }
    `,
  ],
})
export class DanhSachComponent implements OnInit {
  private api = inject(NhiemVuService);
  private router = inject(Router);
  readonly auth = inject(AuthService);

  readonly danhSachTrangThai = DANH_SACH_TRANG_THAI;
  readonly ketQua = signal<KetQuaPhanTrang<NhiemVuTomTat> | null>(null);
  readonly dangTai = signal(false);

  loc: NhiemVuLoc = { trang: 1, kichThuocTrang: 15, statusCode: '', priority: '' };

  ngOnInit(): void {
    this.tai(1);
  }

  tai(trang: number): void {
    this.dangTai.set(true);
    this.api.danhSach({ ...this.loc, trang }).subscribe({
      next: (kq) => {
        this.ketQua.set(kq);
        this.loc.trang = trang;
        this.dangTai.set(false);
      },
      error: () => this.dangTai.set(false),
    });
  }

  xoaLoc(): void {
    this.loc = { trang: 1, kichThuocTrang: 15, statusCode: '', priority: '' };
    this.tai(1);
  }

  moChiTiet(id: number): void {
    this.router.navigate(['/nhiem-vu', id]);
  }

  mauTrangThai(nv: NhiemVuTomTat): string {
    return MAU_TRANG_THAI[nv.statusCode] ?? '#64748b';
  }

  mauUuTien(nv: NhiemVuTomTat): string {
    return MAU_UU_TIEN[nv.priority] ?? '#64748b';
  }
}
