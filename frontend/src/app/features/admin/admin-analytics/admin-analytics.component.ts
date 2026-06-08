import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { AnalyticsResponse } from '../../../core/models/order.models';

@Component({
  selector: 'app-admin-analytics',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-analytics.component.html',
  styleUrl: './admin-analytics.component.css'
})
export class AdminAnalyticsComponent implements OnInit {
  private readonly adminService = inject(AdminService);

  analytics = signal<AnalyticsResponse | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  readonly statusColors: Record<string, string> = {
    Pending: '#f59e0b',
    Paid: '#3b82f6',
    InTransit: '#8b5cf6',
    Delivered: '#16a34a',
    Cancelled: '#6b7280'
  };

  ngOnInit(): void {
    this.loading.set(true);
    this.adminService.getAnalytics().subscribe({
      next: (data) => { this.analytics.set(data); this.loading.set(false); },
      error: (err) => { this.error.set(err.error?.error ?? 'Failed to load analytics'); this.loading.set(false); }
    });
  }

  maxCount(): number {
    const a = this.analytics();
    if (!a) return 1;
    return Math.max(...a.ordersByStatus.map(s => s.count), 1);
  }

  maxQty(): number {
    const a = this.analytics();
    if (!a) return 1;
    return Math.max(...a.topProducts.map(p => p.totalQuantity), 1);
  }

  barWidth(count: number): string {
    return `${Math.round((count / this.maxCount()) * 100)}%`;
  }

  productBarWidth(qty: number): string {
    return `${Math.round((qty / this.maxQty()) * 100)}%`;
  }

  colorFor(status: string): string {
    return this.statusColors[status] ?? '#6b7280';
  }
}
