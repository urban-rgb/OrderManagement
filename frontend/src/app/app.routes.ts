import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'orders', pathMatch: 'full' },
  {
    path: 'orders',
    loadComponent: () => import('./features/orders/order-list.component').then(m => m.OrderListComponent)
  },
  {
    path: 'orders/new',
    loadComponent: () => import('./features/order-form/order-form.component').then(m => m.OrderFormComponent)
  },
  {
    path: 'orders/:id',
    loadComponent: () => import('./features/order-detail/order-detail.component').then(m => m.OrderDetailComponent)
  },
  { path: '**', redirectTo: 'orders' }
];
