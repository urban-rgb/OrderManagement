import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../core/services/order.service';
import { OrderResponse, OrderStatus } from '../../core/models/order.models';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.css'
})
export class OrderDetailComponent implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  order = signal<OrderResponse | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  editingAddress = false;
  newAddress = '';
  actionLoading = signal(false);
  actionError = signal<string | null>(null);
  actionSuccess = signal<string | null>(null);

  readonly OrderStatus = OrderStatus;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loading.set(true);
    this.orderService.getOrder(id).subscribe({
      next: (data) => { this.order.set(data); this.loading.set(false); },
      error: (err) => { this.error.set(err.error?.error ?? 'Order not found'); this.loading.set(false); }
    });
  }

  canChangeAddress(): boolean {
    const o = this.order();
    return !!o && o.status !== OrderStatus.InTransit
      && o.status !== OrderStatus.Delivered
      && o.status !== OrderStatus.Cancelled;
  }

  canCancel(): boolean {
    const o = this.order();
    return !!o && o.status !== OrderStatus.Delivered
      && o.status !== OrderStatus.Cancelled;
  }

  submitAddress(): void {
    const o = this.order();
    if (!o || !this.newAddress.trim()) return;
    this.actionLoading.set(true);
    this.actionError.set(null);
    this.orderService.updateAddress(o.id, { newAddress: this.newAddress.trim() }).subscribe({
      next: () => {
        this.order.update(prev => prev ? { ...prev, shippingAddress: this.newAddress.trim() } : prev);
        this.editingAddress = false;
        this.newAddress = '';
        this.actionLoading.set(false);
        this.showSuccess('Address updated');
      },
      error: (err) => {
        this.actionError.set(err.error?.error ?? 'Failed to update address');
        this.actionLoading.set(false);
      }
    });
  }

  cancel(): void {
    const o = this.order();
    if (!o || !confirm('Cancel this order?')) return;
    this.actionLoading.set(true);
    this.actionError.set(null);
    this.orderService.cancelOrder(o.id).subscribe({
      next: () => {
        this.order.update(prev => prev ? { ...prev, status: OrderStatus.Cancelled } : prev);
        this.actionLoading.set(false);
        this.showSuccess('Order cancelled');
      },
      error: (err) => {
        this.actionError.set(err.error?.error ?? 'Failed to cancel order');
        this.actionLoading.set(false);
      }
    });
  }

  private showSuccess(msg: string): void {
    this.actionSuccess.set(msg);
    setTimeout(() => this.actionSuccess.set(null), 3000);
  }
}
