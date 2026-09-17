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
          {{
            auth.coTheGiaoViec()
              ? 'Việc bạn giao, việc giao cho bạn và việc trong phạm vi bạn phụ trách'
              : 'Nhiệm vụ được giao cho bạn'
          }}
        </p>
      </div>
      @if (auth.coTheGiaoViec()) {
        <a routerLink="/nhiem-vu/tao" class="nut-chinh">+ Tạo nhiệm vụ</a>
      }
    </div>

    <div class="bo-loc the">
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
      <div class="khung-bang the">
      <table>
        <thead>
          <tr>
            <th>Tiêu đề</th>
            <th>Trạng thái</th>
            <th>Ưu tiên</th>
            <th>{{ auth.coTheGiaoViec() ? 'Người thực hiện' : 'Người giao' }}</th>
            @if (auth.coTheGiaoViec()) {
              <th>Phòng · Nhóm</th>
            }
            <th>Hạn</th>
            <th>Tiến độ</th>
          </tr>
        </thead>
        <tbody>
          @for (nv of ketQua()!.danhSach; track nv.id) {
            <tr (click)="moChiTiet(nv.id)">
              <td class="tieu-de">{{ nv.title }}</td>
              <td>
                <span class="nhan" [style.color]="mauTrangThai(nv)"
                      [style.background]="mauTrangThai(nv) + '18'">
                  <i class="dot" [style.background]="mauTrangThai(nv)"></i>
                  {{ nv.tenTrangThai }}
                </span>
              </td>
              <td>
                <span class="cham" [style.background]="mauUuTien(nv)"></span>
                {{ nv.tenUuTien }}
              </td>
              <td>{{ auth.coTheGiaoViec() ? (nv.tenNguoiThucHien ?? '— chưa giao') : nv.tenNguoiTao }}</td>
              @if (auth.coTheGiaoViec()) {
                <td class="phong">{{ nv.tenNhom ?? nv.tenPhongBan ?? '—' }}</td>
              }
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
      </div>

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
        gap: 16px;
        margin-bottom: 20px;
      }
      h1 {
        margin: 0;
        font-size: 24px;
      }
      .phu {
        margin: 3px 0 0;
        font-size: 13.5px;
      }
      a.nut-chinh {
        display: inline-flex;
        align-items: center;
        padding: 10px 17px;
        border-radius: var(--bo-nho);
        text-decoration: none;
        font-size: 14px;
        font-weight: 600;
        white-space: nowrap;
      }
      a.nut-chinh:hover {
        background: var(--chinh-dam);
      }

      .bo-loc {
        display: flex;
        gap: 9px;
        flex-wrap: wrap;
        align-items: center;
        margin-bottom: 16px;
        padding: 13px 15px;
      }
      .bo-loc input:not([type]) {
        min-width: 230px;
        flex: 1 1 230px;
      }
      .bo-loc select {
        width: auto;
        min-width: 158px;
      }
      .o-tick {
        display: flex;
        align-items: center;
        gap: 7px;
        font-size: 13.5px;
        color: var(--chu-vua);
        white-space: nowrap;
        cursor: pointer;
      }
      .o-tick input {
        width: 15px;
        height: 15px;
        accent-color: var(--chinh);
        cursor: pointer;
      }

      /* overflow: hidden để bốn góc bo của thẻ cắt gọn được bảng bên trong;
         overflow-x: auto để bảng nhiều cột cuộn ngang thay vì phá vỡ bố cục. */
      .khung-bang {
        overflow: hidden;
      }
      table {
        width: 100%;
        border-collapse: collapse;
      }
      th {
        text-align: left;
        padding: 11px 16px;
        background: var(--nen-diu);
        font-size: 11.5px;
        font-weight: 600;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        color: var(--chu-nhat);
        border-bottom: 1px solid var(--vien);
        white-space: nowrap;
      }
      td {
        padding: 13px 16px;
        border-bottom: 1px solid var(--nen);
        font-size: 13.5px;
        color: var(--chu-vua);
      }
      tbody tr:last-child td {
        border-bottom: 0;
      }
      tbody tr {
        cursor: pointer;
        transition: background var(--chuyen);
      }
      tbody tr:hover {
        background: var(--chinh-nhat);
      }
      .tieu-de {
        font-weight: 550;
        color: var(--chu);
        max-width: 340px;
      }
      .nhan .dot {
        width: 6px;
        height: 6px;
        border-radius: 50%;
        flex: none;
      }
      .cham {
        display: inline-block;
        width: 8px;
        height: 8px;
        border-radius: 50%;
        margin-right: 7px;
        vertical-align: 1px;
      }
      .qua-han {
        color: var(--do);
        font-weight: 600;
      }
      .phong {
        color: var(--chu-nhat);
        font-size: 12.5px;
      }
      small {
        color: var(--chu-mo);
        margin-left: 4px;
        font-size: 11.5px;
      }
      .thanh {
        width: 74px;
        height: 6px;
        background: var(--vien);
        border-radius: 99px;
        overflow: hidden;
        display: inline-block;
        vertical-align: middle;
      }
      .thanh i {
        display: block;
        height: 100%;
        border-radius: 99px;
        background: linear-gradient(90deg, var(--lam), #22d3ee);
      }

      .trong {
        text-align: center;
        padding: 56px 20px;
        color: var(--chu-nhat);
        background: var(--mat);
        border: 1px dashed var(--vien-dam);
        border-radius: var(--bo);
      }

      .phan-trang {
        display: flex;
        justify-content: center;
        align-items: center;
        gap: 16px;
        margin-top: 18px;
        font-size: 13.5px;
        color: var(--chu-nhat);
      }

      @media (max-width: 760px) {
        .dau-trang {
          flex-direction: column;
        }
        .khung-bang {
          overflow-x: auto;
        }
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
