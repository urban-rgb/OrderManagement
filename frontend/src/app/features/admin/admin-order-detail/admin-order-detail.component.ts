import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { OrderResponse } from '../../../core/models/order.models';

@Component({
  selector: 'app-admin-order-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-order-detail.component.html',
  styleUrl: './admin-order-detail.component.css'
})
export class AdminOrderDetailComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly route = inject(ActivatedRoute);

  order = signal<OrderResponse | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  newStatus = 100;
  statusLoading = signal(false);
  statusError = signal<string | null>(null);
  statusSuccess = signal<string | null>(null);

  readonly statuses = [
    { label: 'Pending', value: 100 },
    { label: 'Paid', value: 200 },
    { label: 'InTransit', value: 300 },
    { label: 'Delivered', value: 400 },
    { label: 'Cancelled', value: 500 }
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loading.set(true);
    this.adminService.getOrder(id).subscribe({
      next: (data) => {
        this.order.set(data);
        this.newStatus = this.statuses.find(s => s.label === data.status)?.value ?? 100;
        this.loading.set(false);
      },
      error: (err) => { this.error.set(err.error?.error ?? 'Order not found'); this.loading.set(false); }
    });
  }

  applyStatus(): void {
    const o = this.order();
    if (!o) return;
    this.statusLoading.set(true);
    this.statusError.set(null);
    this.adminService.forceUpdateStatus(o.id, this.newStatus).subscribe({
      next: () => {
        const label = this.statuses.find(s => s.value === this.newStatus)?.label ?? '';
        this.order.update(prev => prev ? { ...prev, status: label as OrderResponse['status'] } : prev);
        this.statusLoading.set(false);
        this.statusSuccess.set('Status updated');
        setTimeout(() => this.statusSuccess.set(null), 3000);
      },
      error: (err) => {
        this.statusError.set(err.error?.error ?? 'Failed to update status');
        this.statusLoading.set(false);
      }
    });
  }
}
