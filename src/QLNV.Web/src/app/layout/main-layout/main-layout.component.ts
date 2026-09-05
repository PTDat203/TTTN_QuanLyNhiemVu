import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { NbMenuItem, NbSidebarService } from '@nebular/theme';
import { Subject, takeUntil } from 'rxjs';

import { NHAN_VAI_TRO, NguoiDung } from '../../core/models';
import { AuthService } from '../../core/services/auth.service';
import { DanhMucService } from '../../core/services/danh-muc.service';
import { menuTheoVaiTro } from '../menu.const';

/**
 * Khung ung dung: `nb-layout` + `nb-sidebar` + `nb-menu` (§7.2 - Nebular 11).
 * Moi man hinh sau khi dang nhap deu ve trong `router-outlet` cua component nay.
 */
@Component({
  selector: 'qlnv-main-layout',
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss']
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  menu: NbMenuItem[] = [];
  nguoiDung: NguoiDung | null = null;

  private readonly huy$ = new Subject<void>();

  constructor(
    private readonly auth: AuthService,
    private readonly sidebar: NbSidebarService,
    private readonly danhMuc: DanhMucService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.auth.nguoiDung$.pipe(takeUntil(this.huy$)).subscribe((u) => {
      this.nguoiDung = u;
      this.menu = menuTheoVaiTro(u?.vaitro);
    });

    // Nap san danh muc (A4) de cac man khong phai cho: nhan trang thai,
    // do khan, loai van ban deu lay tu DM_TUDIEN (§7.4 muc 3).
    this.danhMuc.tatCaTuDien().subscribe({
      next: () => undefined,
      error: () => undefined // loi danh muc khong duoc chan man hinh; da co ban seed du phong
    });
  }

  ngOnDestroy(): void {
    this.huy$.next();
    this.huy$.complete();
  }

  get tenVaiTro(): string {
    const v = this.nguoiDung?.vaitro;
    return v ? NHAN_VAI_TRO[v] ?? v : '';
  }

  batTatSidebar(): void {
    this.sidebar.toggle(true, 'menu-chinh');
  }

  dangXuat(): void {
    this.auth.dangXuat().subscribe({
      next: () => void this.router.navigate(['/login']),
      // Dang xuat that bai o may chu van phai xoa phien o may tram.
      error: () => {
        this.auth.xoaPhien();
        void this.router.navigate(['/login']);
      }
    });
  }
}
