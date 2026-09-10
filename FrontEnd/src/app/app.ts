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
        <a routerLink="/nhiem-vu" class="ten-app">Quản lý nhiệm vụ</a>
        <div class="ben-phai">
          <span class="nguoi">
            {{ auth.nguoiDung()!.fullName }}
            <small>{{ auth.nguoiDung()!.tenVaiTro }}</small>
          </span>
          <button (click)="auth.dangXuat()">Đăng xuất</button>
        </div>
      </header>
      <main><router-outlet /></main>
    } @else {
      <router-outlet />
    }
  `,
  styles: [`
    :host { display: block; min-height: 100vh; background: #f1f5f9; }
    header {
      background: #0f172a; color: #fff; padding: 0 24px; height: 56px;
      display: flex; align-items: center; justify-content: space-between;
    }
    .ten-app { color: #fff; text-decoration: none; font-size: 16px; font-weight: 500; }
    .ben-phai { display: flex; align-items: center; gap: 16px; }
    .nguoi { font-size: 14px; display: flex; flex-direction: column; line-height: 1.3; }
    .nguoi small { color: #94a3b8; font-size: 11.5px; }
    header button {
      background: transparent; border: 1px solid #475569; color: #e2e8f0;
      padding: 6px 14px; border-radius: 7px; cursor: pointer; font-size: 13px;
    }
    main { padding: 24px; max-width: 1280px; margin: 0 auto; }
  `],
})
export class App {
  readonly auth = inject(AuthService);
}
