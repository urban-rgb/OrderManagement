import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'orders', pathMatch: 'full' },
  {
    path: 'auth',
    children: [
      { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
      { path: 'register', loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent) },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ]
  },
  {
    path: 'orders',
    canActivate: [authGuard],
    loadComponent: () => import('./features/orders/order-list.component').then(m => m.OrderListComponent)
  },
  {
    path: 'orders/new',
    canActivate: [authGuard],
    loadComponent: () => import('./features/order-form/order-form.component').then(m => m.OrderFormComponent)
  },
  {
    path: 'orders/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/order-detail/order-detail.component').then(m => m.OrderDetailComponent)
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    children: [
      { path: '', redirectTo: 'orders', pathMatch: 'full' },
      { path: 'orders', loadComponent: () => import('./features/admin/admin-orders/admin-orders.component').then(m => m.AdminOrdersComponent) },
      { path: 'orders/:id', loadComponent: () => import('./features/admin/admin-order-detail/admin-order-detail.component').then(m => m.AdminOrderDetailComponent) },
      { path: 'analytics', loadComponent: () => import('./features/admin/admin-analytics/admin-analytics.component').then(m => m.AdminAnalyticsComponent) },
      { path: 'users', loadComponent: () => import('./features/admin/admin-users/admin-users.component').then(m => m.AdminUsersComponent) }
    ]
  },
  { path: '**', redirectTo: 'orders' }
];
