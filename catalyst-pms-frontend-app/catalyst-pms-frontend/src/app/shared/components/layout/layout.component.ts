import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { BreakpointObserver } from '@angular/cdk/layout';

import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  managerOnly?: boolean;
}

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatBadgeModule,
    MatMenuModule,
    MatDividerModule
  ],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss'
})
export class LayoutComponent implements OnInit {
  readonly isHandset = signal(false);
  readonly unreadCount = signal(0);

  readonly navItems: NavItem[] = [
    { label: 'Dashboard', icon: 'space_dashboard', route: '/dashboard' },
    { label: 'Products', icon: 'inventory_2', route: '/products' },
    { label: 'Workflow', icon: 'rule', route: '/workflow', managerOnly: true },
    { label: 'Categories', icon: 'category', route: '/categories', managerOnly: true },
    { label: 'Notifications', icon: 'notifications', route: '/notifications' },
    { label: 'Audit log', icon: 'history', route: '/audit-log' }
  ];

  constructor(
    public authService: AuthService,
    private notificationService: NotificationService,
    private router: Router,
    private breakpointObserver: BreakpointObserver
  ) {}

  ngOnInit(): void {
    this.breakpointObserver.observe(['(max-width: 900px)']).subscribe((result) => {
      this.isHandset.set(result.matches);
    });

    this.refreshUnreadCount();
  }

  visibleNavItems(): NavItem[] {
    const isManager = this.authService.isManager();
    return this.navItems.filter((item) => !item.managerOnly || isManager);
  }

  refreshUnreadCount(): void {
    this.notificationService.getUnreadCount().subscribe({
      next: (res) => this.unreadCount.set(res.data.count),
      error: () => {}
    });
  }

  logout(): void {
    // logoutRedirect() navigates the browser itself (to postLogoutRedirectUri) —
    // no need to also call router.navigate() here.
    this.authService.logout();
  }
}
