import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { OrderResponse, AdminOrderListParams } from '../../../core/models/order.models';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-orders.component.html',
  styleUrl: './admin-orders.component.css'
})
export class AdminOrdersComponent implements OnInit {
  private readonly adminService = inject(AdminService);

  orders = signal<OrderResponse[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  sortBy: AdminOrderListParams['sortBy'] = 'createdAt';
  isDescending = true;
  page = 1;
  limit = 20;

  filterUserId = '';
  filterStatus = '';
  filterDateFrom = '';
  filterDateTo = '';

  readonly statuses = ['Pending', 'Paid', 'InTransit', 'Delivered', 'Cancelled'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const params: AdminOrderListParams = {
      sortBy: this.sortBy,
      isDescending: this.isDescending,
      page: this.page,
      limit: this.limit,
      ...(this.filterUserId.trim() && { userId: this.filterUserId.trim() }),
      ...(this.filterStatus && { status: this.filterStatus }),
      ...(this.filterDateFrom && { dateFrom: this.filterDateFrom }),
      ...(this.filterDateTo && { dateTo: this.filterDateTo })
    };
    this.adminService.getOrders(params).subscribe({
      next: (data) => { this.orders.set(data); this.loading.set(false); },
      error: (err) => { this.error.set(err.error?.error ?? 'Failed to load orders'); this.loading.set(false); }
    });
  }

  applyFilters(): void {
    this.page = 1;
    this.load();
  }

  reset(): void {
    this.filterUserId = '';
    this.filterStatus = '';
    this.filterDateFrom = '';
    this.filterDateTo = '';
    this.page = 1;
    this.load();
  }

  onSortChange(field: AdminOrderListParams['sortBy']): void {
    if (this.sortBy === field) {
      this.isDescending = !this.isDescending;
    } else {
      this.sortBy = field;
      this.isDescending = true;
    }
    this.page = 1;
    this.load();
  }

  prevPage(): void {
    if (this.page > 1) { this.page--; this.load(); }
  }

  nextPage(): void {
    this.page++;
    this.load();
  }
}
