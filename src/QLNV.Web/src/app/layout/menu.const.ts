import { NbMenuItem } from '@nebular/theme';

import { VaiTro } from '../core/models';

/**
 * Menu ben trai - §3 danh sach man hinh.
 *
 * Cac man KHONG co muc menu vi duoc mo tu man khac:
 *  - M04 Tao/sua van ban       -> dialog tu M03
 *  - M05 Phan cong nhiem vu    -> `/van-ban/:id/phan-cong`, vao tu dong luoi M03
 *  - M06 Popup AI goi y        -> dialog tu M05
 *  - M07 Kiem tra ket qua      -> dialog tu M08 (bo loc "Tôi giao")
 *  - M09 Xu ly nhiem vu        -> dialog tu M08
 *  - M10 Gia han nhiem vu      -> dialog tu M08
 *
 * §6.4: menu AN theo vai tro. Day chi la lop tien nghi - route van phai co
 * `authGuard` / `vaiTroGuard`, va backend van kiem quyen doc lap (§6.4 bang).
 */
export interface MucMenuQlnv extends NbMenuItem {
  /** Vai tro duoc thay muc nay. Bo trong = moi vai tro. */
  vaiTroChoPhep?: string[];
  children?: MucMenuQlnv[];
}

const MENU_GOC: MucMenuQlnv[] = [
  {
    // M02 - Bang dieu khien
    title: 'Tổng quan',
    icon: 'pie-chart-outline',
    link: '/',
    home: true,
    pathMatch: 'full'
  },
  {
    // M03 - Danh sach van ban chi dao (loi vao cua M04, M05)
    title: 'Văn bản chỉ đạo',
    icon: 'file-text-outline',
    link: '/van-ban'
  },
  {
    // M08 - Nhiem vu cua toi (loi vao cua M07, M09, M10)
    title: 'Nhiệm vụ',
    icon: 'checkmark-square-outline',
    link: '/nhiem-vu'
  },
  {
    // §3.4 - nhom quan tri, CHI QUAN_TRI (§6.2 dong 21, 23, 24)
    title: 'Quản trị',
    icon: 'settings-2-outline',
    vaiTroChoPhep: [VaiTro.QuanTri],
    expanded: false,
    children: [
      {
        // M11 - Nguoi dung & ho so nang luc
        title: 'Người dùng',
        icon: 'people-outline',
        link: '/quan-tri/nguoi-dung',
        vaiTroChoPhep: [VaiTro.QuanTri]
      },
      {
        // M12 - Danh muc (TRANGTHAINV / TRANGTHAIPH / LOAIVB / DOKHAN / linh vuc)
        title: 'Danh mục',
        icon: 'book-outline',
        link: '/quan-tri/danh-muc',
        vaiTroChoPhep: [VaiTro.QuanTri]
      },
      {
        // M13 - Nhat ky goi y AI (§9.8)
        title: 'Nhật ký gợi ý AI',
        icon: 'activity-outline',
        link: '/quan-tri/ai-log',
        vaiTroChoPhep: [VaiTro.QuanTri]
      }
    ]
  }
];

/**
 * Tra ve menu da loc theo vai tro cua nguoi dang dang nhap.
 * Nhanh cha bi bo neu khong con muc con nao duoc phep.
 */
export function menuTheoVaiTro(vaiTro: string | null | undefined): NbMenuItem[] {
  const loc = (ds: MucMenuQlnv[]): MucMenuQlnv[] => {
    const kq: MucMenuQlnv[] = [];
    for (const muc of ds) {
      if (muc.vaiTroChoPhep && (!vaiTro || muc.vaiTroChoPhep.indexOf(vaiTro) < 0)) continue;

      if (muc.children && muc.children.length > 0) {
        const con = loc(muc.children);
        if (con.length === 0) continue;
        kq.push({ ...muc, children: con });
        continue;
      }
      kq.push({ ...muc });
    }
    return kq;
  };

  return loc(MENU_GOC) as NbMenuItem[];
}
