import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./shared/components/layout/layout.component').then((m) => m.LayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent)
      },
      {
        path: 'products',
        loadComponent: () =>
          import('./features/products/product-list/product-list.component').then((m) => m.ProductListComponent)
      },
      {
        path: 'products/new',
        canActivate: [roleGuard],
        data: { roles: ['ProductCapturer'] },
        loadComponent: () =>
          import('./features/products/product-form/product-form.component').then((m) => m.ProductFormComponent)
      },
      {
        path: 'products/:id/edit',
        canActivate: [roleGuard],
        data: { roles: ['ProductCapturer'] },
        loadComponent: () =>
          import('./features/products/product-form/product-form.component').then((m) => m.ProductFormComponent)
      },
      {
        path: 'products/:id',
        loadComponent: () =>
          import('./features/products/product-details/product-details.component').then(
            (m) => m.ProductDetailsComponent
          )
      },
      {
        path: 'categories',
        canActivate: [roleGuard],
        data: { roles: ['ProductManager'] },
        loadComponent: () => import('./features/categories/categories.component').then((m) => m.CategoriesComponent)
      },
      {
        path: 'workflow',
        canActivate: [roleGuard],
        data: { roles: ['ProductManager'] },
        loadComponent: () => import('./features/workflow/workflow.component').then((m) => m.WorkflowComponent)
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./features/notifications/notifications.component').then((m) => m.NotificationsComponent)
      },
      {
        path: 'audit-log',
        loadComponent: () => import('./features/audit-log/audit-log.component').then((m) => m.AuditLogComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
