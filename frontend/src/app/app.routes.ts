import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

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
  { path: '**', redirectTo: 'orders' }
];
