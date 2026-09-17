import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/api.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    @if (auth.daDangNhap()) {
      <header>
        <a routerLink="/nhiem-vu" class="ten-app">
          <span class="dau">NV</span>
          Quản lý nhiệm vụ
        </a>
        <div class="ben-phai">
          <span class="nguoi">
            <strong>{{ auth.nguoiDung()!.fullName }}</strong>
            <small>{{ auth.nguoiDung()!.tenVaiTro }}</small>
          </span>
          <span class="chu-cai">{{ chuCai() }}</span>
          <button (click)="auth.dangXuat()">Đăng xuất</button>
        </div>
      </header>
      <main><router-outlet /></main>
    } @else {
      <router-outlet />
    }
  `,
  styles: [`
    :host { display: block; min-height: 100vh; background: var(--nen); }

    /* Dải chuyển màu rất nhẹ thay vì một khối đặc — thanh điều hướng có chiều sâu
       hơn mà không lấn át nội dung bên dưới. */
    header {
      background: linear-gradient(180deg, #1e293b 0%, #0f172a 100%);
      color: #fff; padding: 0 28px; height: 60px;
      display: flex; align-items: center; justify-content: space-between;
      box-shadow: 0 1px 0 rgb(255 255 255 / 6%) inset, 0 2px 12px rgb(15 23 42 / 18%);
      position: sticky; top: 0; z-index: 20;
    }
    .ten-app {
      color: #fff; text-decoration: none; font-size: 15px; font-weight: 600;
      letter-spacing: -0.01em; display: flex; align-items: center; gap: 11px;
    }
    .dau {
      display: grid; place-items: center; width: 30px; height: 30px;
      border-radius: 9px; background: var(--chinh); color: #fff;
      font-size: 12px; font-weight: 700; letter-spacing: 0.02em;
      box-shadow: 0 2px 8px rgb(37 99 235 / 45%);
    }
    .ben-phai { display: flex; align-items: center; gap: 14px; }
    .nguoi {
      font-size: 13px; display: flex; flex-direction: column;
      line-height: 1.35; text-align: right;
    }
    .nguoi strong { font-weight: 550; }
    .nguoi small { color: #94a3b8; font-size: 11.5px; }
    .chu-cai {
      display: grid; place-items: center; width: 34px; height: 34px;
      border-radius: 50%; background: #334155; color: #e2e8f0;
      font-size: 13px; font-weight: 600; border: 1px solid #475569;
    }
    header button {
      background: rgb(255 255 255 / 6%); border: 1px solid #475569; color: #e2e8f0;
      padding: 7px 14px; border-radius: 8px; font-size: 13px; box-shadow: none;
    }
    header button:hover { background: rgb(255 255 255 / 12%); border-color: #64748b; }

    main { padding: 28px; max-width: 1280px; margin: 0 auto; }

    @media (max-width: 640px) {
      header { padding: 0 16px; }
      .nguoi { display: none; }
      main { padding: 18px 16px; }
    }
  `],
})
export class App {
  readonly auth = inject(AuthService);

  /** Chữ cái đầu của tên và họ, dùng làm ảnh đại diện chữ. */
  chuCai(): string {
    const ten = this.auth.nguoiDung()?.fullName ?? '';
    const phan = ten.trim().split(/\s+/).filter(Boolean);
    if (phan.length === 0) return '?';
    if (phan.length === 1) return phan[0].slice(0, 1).toUpperCase();
    return (phan[0].slice(0, 1) + phan[phan.length - 1].slice(0, 1)).toUpperCase();
  }
}
