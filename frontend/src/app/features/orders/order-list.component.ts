import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../core/services/order.service';
import { OrderResponse, OrderListParams } from '../../core/models/order.models';
import { OrderStatsComponent } from '../order-stats/order-stats.component';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, OrderStatsComponent],
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.css'
})
export class OrderListComponent implements OnInit {
  private readonly orderService = inject(OrderService);

  orders = signal<OrderResponse[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  userId = '';
  sortBy: OrderListParams['sortBy'] = 'createdAt';
  isDescending = true;
  page = 1;
  limit = 10;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const params: OrderListParams = {
      sortBy: this.sortBy,
      isDescending: this.isDescending,
      page: this.page,
      limit: this.limit
    };
    if (this.userId.trim()) params.userId = this.userId.trim();

    this.orderService.getOrders(params).subscribe({
      next: (data) => { this.orders.set(data); this.loading.set(false); },
      error: (err) => { this.error.set(err.error?.error ?? 'Failed to load orders'); this.loading.set(false); }
    });
  }

  prevPage(): void {
    if (this.page > 1) { this.page--; this.load(); }
  }

  nextPage(): void {
    this.page++;
    this.load();
  }

  onSortChange(field: OrderListParams['sortBy']): void {
    if (this.sortBy === field) {
      this.isDescending = !this.isDescending;
    } else {
      this.sortBy = field;
      this.isDescending = true;
    }
    this.page = 1;
    this.load();
  }
}
